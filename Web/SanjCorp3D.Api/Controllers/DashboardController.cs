using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/dashboard")]
public sealed class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Ok(new
        {
            Quotes = await db.Quotes.CountAsync(cancellationToken),
            SalesThisMonth = await db.Sales.CountAsync(x => x.SoldAtUtc >= month, cancellationToken),
            RevenueThisMonth = await db.Sales.Where(x => x.SoldAtUtc >= month).SumAsync(x => (decimal?)x.SaleAmount, cancellationToken) ?? 0,
            LowStock = await db.Consumables.CountAsync(x => x.Active && x.StockGrams <= x.LowStockGrams, cancellationToken)
        });
    }
}
