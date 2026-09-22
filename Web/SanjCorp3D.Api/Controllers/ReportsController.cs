using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;

namespace SanjCorp3D.Api.Controllers;

[ApiController,Authorize,Route("api/reports")]
public sealed class ReportsController(AppDbContext db):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult>Get([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,[FromQuery]Guid? userId=null,CancellationToken ct=default)
    {
        var start=DateTime.SpecifyKind((from??DateTime.UtcNow.AddMonths(-1)).Date,DateTimeKind.Utc);var end=DateTime.SpecifyKind((to??DateTime.UtcNow).Date.AddDays(1),DateTimeKind.Utc);
        var quotes=await db.Quotes.AsNoTracking().Where(x=>x.CreatedAtUtc>=start&&x.CreatedAtUtc<end).ToListAsync(ct);
        var sales=await db.Sales.AsNoTracking().Where(x=>x.SoldAtUtc>=start&&x.SoldAtUtc<end&&(!userId.HasValue||x.CreatedByUserId==userId)).Include(x=>x.Quote).OrderByDescending(x=>x.SoldAtUtc).Select(x=>new{x.Id,x.QuoteId,x.Quote.OrderCode,x.SoldAtUtc,x.Quote.Customer,x.Quote.ProjectName,x.Quote.ProductName,Quantity=x.Quote.Quantity,CostTotal=x.Quote.Subtotal,x.SaleAmount,Profit=x.SaleAmount-x.Quote.Subtotal}).ToListAsync(ct);
        var soldQuoteIds = sales.Select(x => x.QuoteId).ToArray();
        var soldQuotes = quotes.Where(x => soldQuoteIds.Contains(x.Id)).ToList();
        var distribution=await db.QuoteConsumables.AsNoTracking().Where(x=>soldQuoteIds.Contains(x.QuoteId)).GroupBy(x=>x.Name).Select(x=>new{Name=x.Key,Grams=x.Sum(v=>v.Grams*v.Quote.Quantity)}).OrderByDescending(x=>x.Grams).ToListAsync(ct);
        var products = sales.GroupBy(x => string.IsNullOrWhiteSpace(x.ProductName) ? x.ProjectName : x.ProductName).Select(x => new { Name = x.Key, Quantity = x.Sum(v => v.Quantity), Revenue = x.Sum(v => v.SaleAmount) }).OrderByDescending(x => x.Quantity).ToList();
        var losses = await db.InventoryLosses.AsNoTracking().Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end).GroupBy(x => x.Consumable.Name).Select(x => new { Name = x.Key, Grams = x.Sum(v => v.Grams) }).OrderByDescending(x => x.Grams).ToListAsync(ct);
        return Ok(new{QuoteCount=quotes.Count,TotalCost=quotes.Sum(x=>x.Subtotal),ProjectedRevenue=quotes.Sum(x=>x.RecommendedPrice),ProjectedProfit=quotes.Sum(x=>x.ProfitAmount),SaleCount=sales.Count,SalesRevenue=sales.Sum(x=>x.SaleAmount),SalesProfit=sales.Sum(x=>x.Profit),Sales=sales,ProductDistribution=products,LossDistribution=losses,ConsumableDistribution=distribution,CostDistribution=new[]{new{Name="Consumibles",Value=soldQuotes.Sum(x=>x.MaterialCost)},new{Name="Electricidad",Value=soldQuotes.Sum(x=>x.ElectricityCost)},new{Name="Mantenimiento",Value=soldQuotes.Sum(x=>x.MaintenanceCost)},new{Name="Adicionales",Value=soldQuotes.Sum(x=>x.AdditionalCost)}}});
    }

    [HttpGet("sales/export")]
    public async Task<IActionResult>ExportSales([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {
        var start=DateTime.SpecifyKind((from??DateTime.UtcNow.AddMonths(-1)).Date,DateTimeKind.Utc);var end=DateTime.SpecifyKind((to??DateTime.UtcNow).Date.AddDays(1),DateTimeKind.Utc);var rows=await db.Sales.AsNoTracking().Include(x=>x.Quote).Where(x=>x.SoldAtUtc>=start&&x.SoldAtUtc<end).OrderByDescending(x=>x.SoldAtUtc).ToListAsync(ct);var csv=new StringBuilder("Código,Fecha,Cliente,Proyecto,Venta,Costo,Ganancia\r\n");foreach(var x in rows)csv.AppendLine(string.Join(',',Csv(x.Quote.OrderCode),Csv(x.SoldAtUtc.ToString("O")),Csv(x.Quote.Customer),Csv(x.Quote.ProjectName),x.SaleAmount.ToString(CultureInfo.InvariantCulture),x.Quote.Subtotal.ToString(CultureInfo.InvariantCulture),(x.SaleAmount-x.Quote.Subtotal).ToString(CultureInfo.InvariantCulture)));return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),"text/csv",$"ventas-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
    private static string Csv(string value)=>$"\"{value.Replace("\"","\"\"")}\"";
}

