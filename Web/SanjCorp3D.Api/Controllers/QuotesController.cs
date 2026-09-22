using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController,Authorize,Route("api/quotes")]
public sealed class QuotesController(AppDbContext db,BusinessSettingsService settings):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult>List([FromQuery]string search="",[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {
        var q=db.Quotes.AsNoTracking().Include(x=>x.Sale).AsQueryable();
        search=search.Trim();if(search.Length>0)q=q.Where(x=>EF.Functions.ILike(x.OrderCode,$"%{search}%")||EF.Functions.ILike(x.Customer,$"%{search}%")||EF.Functions.ILike(x.ProjectName,$"%{search}%"));
        if(from.HasValue)q=q.Where(x=>x.CreatedAtUtc>=DateTime.SpecifyKind(from.Value.Date,DateTimeKind.Utc));
        if(to.HasValue)q=q.Where(x=>x.CreatedAtUtc<DateTime.SpecifyKind(to.Value.Date.AddDays(1),DateTimeKind.Utc));
        var rows=await q.OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new{x.Id,x.OrderCode,x.CreatedAtUtc,x.Customer,x.ProjectName,x.PrinterName,x.TotalWeight,CostTotal=x.Subtotal,x.RecommendedPrice,SoldAtUtc=x.Sale==null?(DateTime?)null:x.Sale.SoldAtUtc}).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult>Get(long id,CancellationToken ct)
    {
        var q=await db.Quotes.AsNoTracking().AsSplitQuery().Where(x=>x.Id==id).Select(x=>new
        {
            x.Id,x.OrderCode,x.CreatedAtUtc,x.Customer,x.CustomerPhone,x.ProjectName,x.ProductName,x.PrinterName,x.PrintHours,x.Quantity,
            x.AdditionalManualCost,x.ProfitMultiplier,x.Notes,x.TotalWeight,x.MaterialCost,x.ElectricityCost,
            x.MachineCost,x.MaintenanceCost,x.LaborCost,x.AdditionalCost,x.FunctionalSurcharge,x.Subtotal,
            x.ProfitAmount,x.TaxAmount,x.RecommendedPrice,
            Consumables=x.Consumables.OrderBy(v=>v.Id).Select(v=>new{v.Id,v.LegacyConsumableId,v.Name,v.Category,v.Material,v.Color,v.Grams,v.PricePerUnit,v.Density,v.LineCost}).ToList(),
            Materials=x.Materials.OrderBy(v=>v.Id).Select(v=>new{v.Id,v.LegacyMaterialId,v.Name,v.Quantity,v.UnitPrice,v.LineCost}).ToList(),
            Sale=x.Sale==null?null:new{x.Sale.Id,x.Sale.SoldAtUtc,x.Sale.SaleAmount}
        }).FirstOrDefaultAsync(ct);
        return q is null?NotFound():Ok(q);
    }

    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPost("calculate")]
    public async Task<IActionResult>Calculate(QuoteRequest request,CancellationToken ct)
    {
        var resolved=await Resolve(request,ct);return Ok(QuoteCalculator.Calculate(request,resolved.Printer,resolved.Consumables,resolved.Materials,await settings.GetAsync(ct)));
    }

    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPost]
    public async Task<IActionResult>Create(QuoteRequest request,CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhone)) return BadRequest(new { message = "El celular del cliente es obligatorio para guardar la cotización." });
        var resolved=await Resolve(request,ct);var calculation=QuoteCalculator.Calculate(request,resolved.Printer,resolved.Consumables,resolved.Materials,await settings.GetAsync(ct));
        var strategy=db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            long next=(await db.Quotes.MaxAsync(x=>(long?)x.Id,ct)??0)+1;var created=DateTime.UtcNow;
            var quote=new Quote{OrderCode=$"{Initial(request.Customer)}{Initial(resolved.Printer.Name)}{created:yyyyMMdd}{next:0000}",CreatedAtUtc=created,Customer=request.Customer.Trim(),CustomerPhone=request.CustomerPhone.Trim(),ProjectName=request.ProjectName.Trim(),ProductName=request.ProductName?.Trim()??string.Empty,PrinterId=resolved.Printer.Id,PrinterName=resolved.Printer.Name,PrintHours=request.PrintHours,Quantity=request.Quantity,AdditionalManualCost=request.AdditionalManualCost,ProfitMultiplier=request.ProfitMultiplier,Notes=request.Notes?.Trim()??string.Empty,TotalWeight=calculation.TotalWeight,MaterialCost=calculation.MaterialCost,ElectricityCost=calculation.ElectricityCost,MachineCost=0,MaintenanceCost=calculation.MaintenanceCost,LaborCost=0,AdditionalCost=calculation.AdditionalCost,FunctionalSurcharge=0,Subtotal=calculation.Subtotal,ProfitAmount=calculation.ProfitAmount,TaxAmount=calculation.TaxAmount,RecommendedPrice=calculation.RecommendedPrice};
            foreach(var line in resolved.Consumables)quote.Consumables.Add(new QuoteConsumable{LegacyConsumableId=line.Item.Id,Name=line.Item.Name,Category=line.Item.Category,Material=line.Item.Material,Color=line.Item.Color,Grams=line.Grams,PricePerUnit=line.Item.PricePerUnit,Density=line.Item.Density,LineCost=decimal.Round(line.UnitCost*request.Quantity,4,MidpointRounding.AwayFromZero)});
            foreach(var line in resolved.Materials)quote.Materials.Add(new QuoteMaterial{LegacyMaterialId=line.Item.Id,Name=line.Item.Name,Quantity=line.Quantity,UnitPrice=line.Item.UnitPrice,LineCost=decimal.Round(line.Cost,4,MidpointRounding.AwayFromZero)});
            db.Quotes.Add(quote);await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
            return CreatedAtAction(nameof(Get),new{id=quote.Id},new{quote.Id,quote.OrderCode,quote.CreatedAtUtc,quote.Customer,quote.ProjectName,quote.ProductName,quote.PrinterName,quote.TotalWeight,CostTotal=quote.Subtotal,quote.RecommendedPrice,SoldAtUtc=(DateTime?)null});
        });
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPatch("{id:long}/price")]
    public async Task<IActionResult> UpdatePrice(long id, [FromBody] PriceUpdateRequest request, CancellationToken ct)
    {
        if (request.Price <= 0) return BadRequest(new { message = "El precio final debe ser mayor que cero." });
        var quote = await db.Quotes.Include(x => x.Sale).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (quote is null) return NotFound();
        if (quote.Sale is not null) return Conflict(new { message = "No puedes cambiar el precio de una venta confirmada." });
        quote.RecommendedPrice = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        quote.ProfitAmount = quote.RecommendedPrice - quote.Subtotal - quote.TaxAmount;
        await db.SaveChangesAsync(ct);
        return Ok(new { quote.Id, quote.RecommendedPrice, quote.ProfitAmount });
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/copy")]
    public async Task<IActionResult> Copy(long id, CancellationToken ct)
    {
        var source = await db.Quotes.AsNoTracking().Include(x => x.Consumables).Include(x => x.Materials).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (source is null) return NotFound();
        var now = DateTime.UtcNow;
        var copy = new Quote { OrderCode = $"COPIA{now:yyyyMMddHHmmss}", CreatedAtUtc = now, Customer = source.Customer, CustomerPhone = source.CustomerPhone, ProjectName = source.ProjectName, ProductName = source.ProductName, PrinterId = source.PrinterId, PrinterName = source.PrinterName, PrintHours = source.PrintHours, Quantity = source.Quantity, AdditionalManualCost = source.AdditionalManualCost, ProfitMultiplier = source.ProfitMultiplier, Notes = source.Notes, TotalWeight = source.TotalWeight, MaterialCost = source.MaterialCost, ElectricityCost = source.ElectricityCost, MachineCost = source.MachineCost, MaintenanceCost = source.MaintenanceCost, LaborCost = source.LaborCost, AdditionalCost = source.AdditionalCost, FunctionalSurcharge = source.FunctionalSurcharge, Subtotal = source.Subtotal, ProfitAmount = source.ProfitAmount, TaxAmount = source.TaxAmount, RecommendedPrice = source.RecommendedPrice };
        foreach (var line in source.Consumables) copy.Consumables.Add(new QuoteConsumable { LegacyConsumableId = line.LegacyConsumableId, Name = line.Name, Category = line.Category, Material = line.Material, Color = line.Color, Grams = line.Grams, PricePerUnit = line.PricePerUnit, Density = line.Density, LineCost = line.LineCost });
        foreach (var line in source.Materials) copy.Materials.Add(new QuoteMaterial { LegacyMaterialId = line.LegacyMaterialId, Name = line.Name, Quantity = line.Quantity, UnitPrice = line.UnitPrice, LineCost = line.LineCost });
        db.Quotes.Add(copy); await db.SaveChangesAsync(ct);
        return Ok(new { copy.Id, copy.OrderCode, copy.CreatedAtUtc, copy.Customer, copy.ProjectName, copy.ProductName, copy.PrinterName, copy.TotalWeight, CostTotal = copy.Subtotal, copy.RecommendedPrice, SoldAtUtc = (DateTime?)null });
    }

    public sealed record PriceUpdateRequest(decimal Price);

    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Sales},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPost("{id:long}/sale")]
    public async Task<IActionResult>ConfirmSale(long id,CancellationToken ct)
    {
        var strategy=db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var quote = await db.Quotes
                .Include(x => x.Sale)
                .Include(x => x.Consumables)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return NotFound();
            if (quote.Sale is not null) return Conflict(new { message = "Esta cotización ya fue confirmada como venta." });

            var usage = new List<(long Id, decimal Grams)>();
            foreach (var line in quote.Consumables)
            {
                if (line.Grams <= 0) continue;
                if (line.LegacyConsumableId <= 0)
                    return Conflict(new { message = $"La cotización {quote.OrderCode} tiene un consumible sin referencia de inventario." });
                var grams = decimal.Round(line.Grams * quote.Quantity, 4, MidpointRounding.AwayFromZero);
                var index = usage.FindIndex(x => x.Id == line.LegacyConsumableId);
                if (index < 0) usage.Add((line.LegacyConsumableId, grams));
                else usage[index] = (usage[index].Id, usage[index].Grams + grams);
            }

            var ids = usage.Select(x => x.Id).ToArray();
            var inventory = await db.Consumables.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var lotsByItem = await db.ConsumableStockLots.Where(x => ids.Contains(x.ConsumableId)).OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
            foreach (var required in usage)
            {
                if (!inventory.TryGetValue(required.Id, out var item))
                    return Conflict(new { message = "Uno de los filamentos de esta cotización ya no existe en el inventario." });
                if (item.StockGrams + 0.0001m < required.Grams)
                    return Conflict(new
                    {
                        code = "INSUFFICIENT_STOCK",
                        message = $"Stock insuficiente para {item.Name} · {item.Material} · {item.Color}. Disponible: {FormatWeight(item.StockGrams)}; necesario: {FormatWeight(required.Grams)}."
                    });
            }

            var sale = new Sale { SoldAtUtc = DateTime.UtcNow, SaleAmount = quote.RecommendedPrice, CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) is string value && Guid.TryParse(value, out var userId) ? userId : null };
            foreach (var required in usage)
            {
                var item = inventory[required.Id];
                var lots = lotsByItem.Where(x => x.ConsumableId == item.Id).OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).ToList();
                if (lots.Count == 0 && item.StockGrams > 0) lots.Add(new ConsumableStockLot { ConsumableId = item.Id, RemainingGrams = item.StockGrams, OriginalGrams = item.StockGrams, PricePerKilogram = item.PricePerUnit });
                var left = required.Grams;
                foreach (var lot in lots)
                {
                    if (left <= 0) break;
                    var take = Math.Min(left, lot.RemainingGrams);
                    lot.RemainingGrams -= take;
                    left -= take;
                    sale.Consumables.Add(new SaleConsumable { ConsumableId = item.Id, Grams = take, PricePerKilogram = lot.PricePerKilogram });
                }
                item.StockGrams = decimal.Round(item.StockGrams - required.Grams, 4, MidpointRounding.AwayFromZero);
                SyncLegacyQuantity(item);
            }
            quote.Sale = sale;
            var assignedPrinterIds = AssignedPrinterIds(quote);
            if (assignedPrinterIds.Count == 0)
            {
                var printerId = quote.PrinterId ?? await db.Printers.Where(x => x.Active && x.Name == quote.PrinterName).Select(x => (long?)x.Id).FirstOrDefaultAsync(ct);
                if (printerId.HasValue) assignedPrinterIds.Add(printerId.Value);
            }
            foreach (var assignedPrinterId in assignedPrinterIds.Distinct())
            {
                if (await db.PrintOrders.AnyAsync(x => x.PrinterId == assignedPrinterId && x.Status != "Terminado", ct))
                    return Conflict(new { message = "Una de las impresoras seleccionadas ya tiene un pedido asignado. Debe terminar y enfriarse antes de recibir otro pedido." });
                if (!await db.PrintOrders.AnyAsync(x => x.QuoteId == quote.Id && x.PrinterId == assignedPrinterId, ct))
                    db.PrintOrders.Add(new PrintOrder { QuoteId = quote.Id, PrinterId = assignedPrinterId, Status = "Pendiente", CreatedAtUtc = DateTime.UtcNow });
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new
            {
                sale.Id,
                sale.QuoteId,
                sale.SoldAtUtc,
                sale.SaleAmount,
                consumed = usage.Select(x => new { consumableId = x.Id, grams = x.Grams }).ToList()
            });
        });
    }

    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpDelete("{id:long}")]
    public async Task<IActionResult>Delete(long id,CancellationToken ct)
    {
        var strategy=db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var quote = await db.Quotes.Include(x => x.Sale).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return NotFound();
            if (quote.Sale is not null)
            {
                var consumption = await db.SaleConsumables.Where(x => x.SaleId == quote.Sale.Id).ToListAsync(ct);
                var ids = consumption.Select(x => x.ConsumableId).Distinct().ToArray();
                var inventory = await db.Consumables.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
                foreach (var line in consumption)
                {
                    if (!inventory.TryGetValue(line.ConsumableId, out var item)) continue;
                    item.StockGrams = decimal.Round(item.StockGrams + line.Grams, 4, MidpointRounding.AwayFromZero);
                    db.ConsumableStockLots.Add(new ConsumableStockLot { ConsumableId = item.Id, OriginalGrams = line.Grams, RemainingGrams = line.Grams, PricePerKilogram = line.PricePerKilogram > 0 ? line.PricePerKilogram : item.PricePerUnit });
                    SyncLegacyQuantity(item);
                }
            }
            db.Quotes.Remove(quote);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return NoContent();
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult>Export([FromQuery]string search="",[FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {
        var q=db.Quotes.AsNoTracking().Include(x=>x.Sale).AsQueryable();
        search=search.Trim();
        if(search.Length>0)q=q.Where(x=>EF.Functions.ILike(x.OrderCode,$"%{search}%")||EF.Functions.ILike(x.Customer,$"%{search}%")||EF.Functions.ILike(x.ProjectName,$"%{search}%"));
        if(from.HasValue)q=q.Where(x=>x.CreatedAtUtc>=DateTime.SpecifyKind(from.Value.Date,DateTimeKind.Utc));
        if(to.HasValue)q=q.Where(x=>x.CreatedAtUtc<DateTime.SpecifyKind(to.Value.Date.AddDays(1),DateTimeKind.Utc));
        var rows=await q.OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct);
        var config = await settings.GetAsync(ct);
        var html = ExcelHistoryReport(rows, "Atlas Impresiones 3D", config.CurrencySymbol, search, from, to);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(html)).ToArray(),"application/vnd.ms-excel",$"historial-cotizaciones-{DateTime.UtcNow:yyyyMMdd}.xls");
    }

    private static string ExcelHistoryReport(IReadOnlyList<Quote> rows, string businessName, string currencySymbol, string search, DateTime? from, DateTime? to)
    {
        static string H(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string M(decimal value, string symbol) => $"{symbol} {value:0.00}";
        var sold = rows.Count(x => x.Sale is not null);
        var pending = rows.Count - sold;
        var totalCost = rows.Sum(x => x.Subtotal);
        var totalPrice = rows.Sum(x => x.RecommendedPrice);
        var totalProfit = rows.Sum(x => x.ProfitAmount);
        var logo = ExcelLogoDataUri();
        var period = from.HasValue || to.HasValue ? $"{from:yyyy-MM-dd} al {to:yyyy-MM-dd}" : "Todos los registros";
        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset='utf-8'><style>");
        sb.AppendLine("body{font-family:Calibri,Arial,sans-serif;color:#10201d;background:#fff} table{border-collapse:collapse;width:100%} td,th{border:1px solid #bfd9d4;padding:7px 9px;font-size:12px} .top td{border:0}.brand{background:#057369;color:white}.brand h1{margin:0;font-size:24px}.brand p{margin:3px 0 0 0;font-size:12px}.logo{width:82px;height:64px;text-align:center;vertical-align:middle;background:#057369}.title{font-size:18px;font-weight:700;color:#057369}.muted{color:#60736f}.metric{background:#eefaf7;font-weight:700}.metric b{font-size:15px}.head th{background:#057369;color:white;font-weight:700;text-align:left}.money{text-align:right;mso-number-format:'#,##0.00'}.num{text-align:right}.sold{background:#dff6ec;color:#155d42;font-weight:700}.pending{background:#f1f4f2;color:#4b5855;font-weight:700}.total td{background:#eaf8f5;font-weight:700}.note{font-size:11px;color:#60736f;border:0}.spacer td{border:0;height:12px}.code{mso-number-format:'@';font-weight:700}</style></head><body>");
        sb.AppendLine("<table class='top'>");
        sb.AppendLine("<tr class='brand'><td class='logo'>" + (logo is null ? "<b>ATLAS<br/>3D</b>" : $"<img src='{logo}' width='70' height='54' />") + $"</td><td colspan='9'><h1>{H(businessName)}</h1><p>Historial de cotizaciones para registro contable y control tributario</p></td></tr>");
        sb.AppendLine($"<tr><td colspan='10' class='title'>Informe de historial</td></tr>");
        sb.AppendLine($"<tr><td colspan='2'><b>Generado:</b></td><td colspan='3'>{DateTime.Now:yyyy-MM-dd HH:mm}</td><td colspan='2'><b>Periodo:</b></td><td colspan='3'>{H(period)}</td></tr>");
        sb.AppendLine($"<tr><td colspan='2'><b>Busqueda:</b></td><td colspan='8'>{H(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}</td></tr>");
        sb.AppendLine("<tr class='spacer'><td colspan='10'></td></tr>");
        sb.AppendLine("<tr><td colspan='2' class='metric'>Cotizaciones<br/><b>" + rows.Count + "</b></td><td colspan='2' class='metric'>Vendidas<br/><b>" + sold + "</b></td><td colspan='2' class='metric'>Pendientes<br/><b>" + pending + "</b></td><td colspan='2' class='metric'>Ingresos<br/><b>" + H(M(totalPrice, currencySymbol)) + "</b></td><td colspan='2' class='metric'>Ganancia<br/><b>" + H(M(totalProfit, currencySymbol)) + "</b></td></tr>");
        sb.AppendLine("<tr class='spacer'><td colspan='10'></td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr class='head'><th>Código</th><th>Estado</th><th>Fecha</th><th>Cliente</th><th>Proyecto</th><th>Producto</th><th>Impresora</th><th>Peso g</th><th>Costo</th><th>Precio final</th><th>Ganancia</th></tr>");
        foreach (var x in rows)
        {
            var status = x.Sale is null ? "COTIZACION" : "VENDIDA";
            sb.AppendLine("<tr>"+
                $"<td class='code'>{H(x.OrderCode)}</td>"+
                $"<td class='{(x.Sale is null ? "pending" : "sold")}'>{status}</td>"+
                $"<td>{x.CreatedAtUtc:yyyy-MM-dd HH:mm}</td>"+
                $"<td>{H(x.Customer)}</td>"+
                $"<td>{H(x.ProjectName)}</td>"+
                $"<td>{H(x.ProductName)}</td>"+
                $"<td>{H(x.PrinterName)}</td>"+
                $"<td class='num'>{x.TotalWeight:0.##}</td>"+
                $"<td class='money'>{x.Subtotal:0.00}</td>"+
                $"<td class='money'>{x.RecommendedPrice:0.00}</td>"+
                $"<td class='money'>{x.ProfitAmount:0.00}</td></tr>");
        }
        sb.AppendLine($"<tr class='total'><td colspan='8'>TOTALES</td><td class='money'>{totalCost:0.00}</td><td class='money'>{totalPrice:0.00}</td><td class='money'>{totalProfit:0.00}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<p class='note'>Documento generado por Atlas Impresiones 3D. Revise la informacion antes de presentarla como respaldo contable o tributario.</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string? ExcelLogoDataUri()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "logoblanco.png");
            if (System.IO.File.Exists(candidate)) return "data:image/png;base64," + Convert.ToBase64String(System.IO.File.ReadAllBytes(candidate));
            current = current.Parent;
        }
        return null;
    }
    [HttpGet("{id:long}/voucher")]
    public async Task<IActionResult>Voucher(long id,CancellationToken ct)
    {
        var quote = await db.Quotes.AsNoTracking()
            .Include(x => x.Sale)
            .Include(x => x.Consumables)
            .Include(x => x.Materials)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (quote is null) return NotFound();
        var business = "Atlas Impresiones 3D";
        var config = await settings.GetAsync(ct);
        return File(PdfVoucher(quote, business, config.CurrencySymbol), "application/pdf", $"cotización-{quote.OrderCode}.pdf");
    }
    private async Task<(Printer Printer,List<QuoteCalculator.ConsumableLine> Consumables,List<QuoteCalculator.MaterialLine> Materials)>Resolve(QuoteRequest request,CancellationToken ct)
    {
        var printer=await db.Printers.FirstOrDefaultAsync(x=>x.Id==request.PrinterId&&x.Active,ct)??throw new ArgumentException("Selecciona una impresora registrada antes de cotizar.");
        var consumableIds=request.Consumables.Select(x=>x.ConsumableId).Distinct().ToArray();var available=await db.Consumables.Where(x=>consumableIds.Contains(x.Id)&&x.Active).ToDictionaryAsync(x=>x.Id,ct);var lots=await db.ConsumableStockLots.Where(x=>consumableIds.Contains(x.ConsumableId)).OrderBy(x=>x.ReceivedAtUtc).ThenBy(x=>x.Id).ToListAsync(ct);var remainingLots=available.ToDictionary(x=>x.Key,lotsForItem=>lots.Where(x=>x.ConsumableId==lotsForItem.Key).Select(x=>new StockPriceSegment(x.RemainingGrams,x.PricePerKilogram)).ToList());foreach(var item in available.Values)if(remainingLots[item.Id].Count==0&&item.StockGrams>0)remainingLots[item.Id].Add(new StockPriceSegment(item.StockGrams,item.PricePerUnit));var consumables=new List<QuoteCalculator.ConsumableLine>();foreach(var input in request.Consumables){if(!available.TryGetValue(input.ConsumableId,out var item))throw new ArgumentException("Uno de los consumibles no existe o está archivado.");if(item.StockGrams<=0)throw new ArgumentException($"No hay existencia de {item.Name} · {item.Material} · {item.Color}.");var itemLots=remainingLots[item.Id];var required=input.Grams*request.Quantity;if(itemLots.Sum(x=>x.Grams)+0.0001m<required)throw new ArgumentException($"Stock insuficiente para {item.Name} · {item.Material} · {item.Color}.");var cost=CostFor(itemLots,required);RemoveFrom(itemLots,required);consumables.Add(new(item,input.Grams,cost/request.Quantity));}
        var materialIds=request.Materials.Select(x=>x.MaterialId).Distinct().ToArray();var materialCatalog=await db.Materials.Where(x=>materialIds.Contains(x.Id)&&x.Active).ToDictionaryAsync(x=>x.Id,ct);var materials=new List<QuoteCalculator.MaterialLine>();foreach(var input in request.Materials){if(!materialCatalog.TryGetValue(input.MaterialId,out var item))throw new ArgumentException("Uno de los materiales no existe o está archivado.");materials.Add(new(item,input.Quantity));}
        return(printer,consumables,materials);
    }
    private sealed record StockPriceSegment(decimal Grams, decimal Price);
    private static decimal CostFor(List<StockPriceSegment> lots, decimal grams){var left=grams;var cost=0m;foreach(var lot in lots){if(left<=0)break;var take=Math.Min(left,lot.Grams);cost+=take/1000m*lot.Price;left-=take;}return cost;}
    private static void RemoveFrom(List<StockPriceSegment> lots, decimal grams){var left=grams;for(var i=0;i<lots.Count&&left>0;){var take=Math.Min(left,lots[i].Grams);left-=take;if(take>=lots[i].Grams)lots.RemoveAt(i);else{lots[i]=lots[i] with { Grams=lots[i].Grams-take };i++;}}}
    private static byte[] PdfVoucher(Quote quote, string businessName, string currencySymbol)
    {
        static string Safe(string value)
        {
            value ??= string.Empty;
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var ascii = new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c <= 127).ToArray());
            return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
        static string N(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static string Money(decimal value, string symbol) => $"{symbol} {value:0.00}";
        static string TrimText(string value, int max) => string.IsNullOrWhiteSpace(value) ? "-" : value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
        static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

        var logo = TryLoadLogoPngRgb();
        var content = new StringBuilder();
        void Raw(string value) => content.Append(value).Append('\n');
        void Fill(decimal r, decimal g, decimal b) => Raw($"{N(r)} {N(g)} {N(b)} rg");
        void Stroke(decimal r, decimal g, decimal b) => Raw($"{N(r)} {N(g)} {N(b)} RG");
        void Rect(decimal x, decimal y, decimal w, decimal h, bool stroke = false) => Raw($"{N(x)} {N(y)} {N(w)} {N(h)} re {(stroke ? "S" : "f")}");
        void Line(decimal x1, decimal y1, decimal x2, decimal y2) => Raw($"{N(x1)} {N(y1)} m {N(x2)} {N(y2)} l S");
        void Text(string font, decimal size, decimal x, decimal y, string value, decimal r = .06m, decimal g = .13m, decimal b = .12m)
        {
            Fill(r, g, b);
            Raw($"BT /{font} {N(size)} Tf {N(x)} {N(y)} Td ({Safe(value)}) Tj ET");
        }
        void Image(decimal x, decimal y, decimal w, decimal h)
        {
            if (logo is null) return;
            Raw($"q {N(w)} 0 0 {N(h)} {N(x)} {N(y)} cm /Im1 Do Q");
        }
        void InfoCard(decimal x, decimal y, decimal w, string title, params (string Label, string Value)[] rows)
        {
            Fill(.97m, .995m, .99m); Rect(x, y, w, 112);
            Stroke(.72m, .88m, .84m); Rect(x, y, w, 112, true);
            Text("F2", 10, x + 14, y + 88, title, .02m, .41m, .37m);
            var rowY = y + 63;
            foreach (var row in rows.Take(3))
            {
                Text("F1", 8, x + 14, rowY + 13, row.Label.ToUpperInvariant(), .36m, .48m, .46m);
                Text("F2", 10, x + 14, rowY, TrimText(row.Value, 31), .05m, .10m, .09m);
                rowY -= 31;
            }
        }

        Fill(1, 1, 1); Rect(0, 0, 595, 842);
        Fill(.92m, .985m, .975m); Rect(0, 710, 595, 132);
        Fill(.13m, .78m, .72m); Rect(0, 706, 595, 5);
        Fill(.02m, .45m, .41m); Rect(42, 744, 82, 72);
        if (logo is null)
        {
            Stroke(.13m, .78m, .72m); Rect(48, 750, 70, 60, true);
            Text("F2", 16, 59, 785, "ATLAS", 1, 1, 1);
            Text("F1", 7, 55, 773, "IMPRESIONES 3D", .86m, 1, .98m);
        }
        else
        {
            Image(50, 752, 66, 56);
        }
        Text("F2", 26, 143, 790, businessName, .02m, .31m, .29m);
        Text("F1", 11, 145, 770, "Comprobante profesional de cotización", .20m, .46m, .43m);
        Fill(1, 1, 1); Rect(407, 756, 130, 48);
        Stroke(.72m, .88m, .84m); Rect(407, 756, 130, 48, true);
        Text("F2", 10, 421, 786, quote.Sale is null ? "COTIZACION" : "VENTA CONFIRMADA", .02m, .41m, .37m);
        Text("F1", 8.5m, 421, 771, $"Código: {quote.OrderCode}", .22m, .37m, .35m);
        Text("F1", 8.5m, 421, 759, $"Fecha: {quote.CreatedAtUtc:yyyy-MM-dd HH:mm}", .22m, .37m, .35m);

        InfoCard(42, 574, 248, "DATOS DEL CLIENTE", ("Cliente", quote.Customer), ("Celular", string.IsNullOrWhiteSpace(quote.CustomerPhone) ? "Sin registrar" : quote.CustomerPhone), ("Estado", quote.Sale is null ? "Cotización" : "Venta confirmada"));
        InfoCard(305, 574, 248, "DETALLE DEL TRABAJO", ("Proyecto", quote.ProjectName), ("Producto", string.IsNullOrWhiteSpace(quote.ProductName) ? "Pieza personalizada" : quote.ProductName), ("Impresora", quote.PrinterName));

        Fill(.98m, 1, .995m); Rect(42, 512, 511, 42);
        Stroke(.72m, .88m, .84m); Rect(42, 512, 511, 42, true);
        Text("F1", 8, 62, 537, "CANTIDAD", .36m, .48m, .46m); Text("F2", 11, 62, 522, $"{quote.Quantity} pieza(s)");
        Text("F1", 8, 205, 537, "TIEMPO ESTIMADO", .36m, .48m, .46m); Text("F2", 11, 205, 522, $"{quote.PrintHours:0.##} h");
        Text("F1", 8, 370, 537, "PESO TOTAL", .36m, .48m, .46m); Text("F2", 11, 370, 522, $"{quote.TotalWeight:0.##} g");

        Fill(.02m, .45m, .41m); Rect(42, 462, 511, 30);
        Text("F2", 10, 60, 473, "CONCEPTO", 1, 1, 1);
        Text("F2", 10, 405, 473, "IMPORTE", 1, 1, 1);
        decimal y = 435;
        void Row(string label, decimal amount, bool bold = false, bool shade = false)
        {
            Fill(shade ? .95m : 1m, shade ? .99m : 1m, shade ? .985m : 1m); Rect(42, y - 9, 511, 30);
            Stroke(.86m, .93m, .91m); Line(42, y - 10, 553, y - 10);
            Text(bold ? "F2" : "F1", 10.5m, 60, y, label);
            Text(bold ? "F2" : "F1", 10.5m, 405, y, Money(amount, currencySymbol));
            y -= 30;
        }
        Row("Consumibles", quote.MaterialCost);
        Row("Electricidad", quote.ElectricityCost, shade: true);
        Row("Mantenimiento", quote.MaintenanceCost);
        Row("Adicionales y extras", quote.AdditionalCost, shade: true);
        Row("Subtotal de costos", quote.Subtotal, true);
        Row("Ganancia", quote.ProfitAmount, shade: true);
        Row("Impuesto", quote.TaxAmount);

        Fill(.90m, .985m, .97m); Rect(42, 218, 511, 62);
        Stroke(.13m, .78m, .72m); Rect(42, 218, 511, 62, true);
        Text("F2", 12, 62, 254, "PRECIO FINAL", .02m, .41m, .37m);
        Text("F2", 26, 352, 242, Money(quote.RecommendedPrice, currencySymbol), .02m, .31m, .29m);
        Fill(.13m, .78m, .72m); Rect(42, 214, 511, 4);

        Text("F2", 10.5m, 42, 178, "RESUMEN DE MATERIALES", .02m, .41m, .37m);
        var materialSummary = quote.Consumables.OrderBy(x => x.Id).Take(3).Select(x => $"{x.Name} - {x.Material} - {x.Color}: {x.Grams:0.##} g/pieza").ToList();
        materialSummary.AddRange(quote.Materials.OrderBy(x => x.Id).Take(Math.Max(0, 4 - materialSummary.Count)).Select(x => $"{x.Name}: {x.Quantity:0.##} x {Money(x.UnitPrice, currencySymbol)}"));
        if (materialSummary.Count == 0) materialSummary.Add("Sin materiales adicionales registrados.");
        var lineY = 158m;
        foreach (var line in materialSummary.Take(4))
        {
            Text("F1", 8.5m, 52, lineY, TrimText(line, 84), .25m, .36m, .34m);
            lineY -= 14;
        }

        Stroke(.72m, .88m, .84m); Line(42, 70, 553, 70);
        Text("F1", 8, 42, 52, "Este documento corresponde a una cotización y no reemplaza una factura fiscal.", .34m, .43m, .41m);
        Text("F2", 9, 377, 52, "Gracias por confiar en Atlas Impresiones 3D", .02m, .41m, .37m);

        var stream = content.ToString();
        var pageResources = logo is null
            ? "<< /Font << /F1 4 0 R /F2 5 0 R >> >>"
            : "<< /Font << /F1 4 0 R /F2 5 0 R >> /XObject << /Im1 7 0 R >> >>";
        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources {pageResources} /Contents 6 0 R >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"),
            Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"),
            Ascii($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream")
        };
        if (logo is not null)
        {
            using var compressed = new MemoryStream();
            using (var zipper = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) zipper.Write(logo.Rgb);
            var bytes = compressed.ToArray();
            using var imageObject = new MemoryStream();
            imageObject.Write(Ascii($"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {bytes.Length} >>\nstream\n"));
            imageObject.Write(bytes);
            imageObject.Write(Ascii("\nendstream"));
            objects.Add(imageObject.ToArray());
        }

        using var pdf = new MemoryStream();
        void WriteAscii(string value) => pdf.Write(Ascii(value));
        WriteAscii("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(pdf.Position);
            WriteAscii($"{i + 1} 0 obj\n");
            pdf.Write(objects[i]);
            WriteAscii("\nendobj\n");
        }
        var xref = pdf.Position;
        WriteAscii($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii($"{offset:0000000000} 00000 n \n");
        WriteAscii($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return pdf.ToArray();
    }

    private sealed record PngLogo(int Width, int Height, byte[] Rgb);

    private static PngLogo? TryLoadLogoPngRgb()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "logoblanco.png");
            if (System.IO.File.Exists(candidate)) return TryDecodePng(candidate, 7, 115, 105);
            current = current.Parent;
        }
        return null;
    }

    private static PngLogo? TryDecodePng(string path, byte bgR, byte bgG, byte bgB)
    {
        try
        {
            var data = System.IO.File.ReadAllBytes(path);
            if (data.Length < 33 || data[0] != 137 || data[1] != 80 || data[2] != 78 || data[3] != 71) return null;
            var offset = 8;
            var width = 0; var height = 0; byte bitDepth = 0; byte colorType = 0;
            using var idat = new MemoryStream();
            while (offset + 8 <= data.Length)
            {
                var length = ReadBigEndianInt(data, offset); offset += 4;
                var type = Encoding.ASCII.GetString(data, offset, 4); offset += 4;
                if (offset + length > data.Length) return null;
                if (type == "IHDR") { width = ReadBigEndianInt(data, offset); height = ReadBigEndianInt(data, offset + 4); bitDepth = data[offset + 8]; colorType = data[offset + 9]; }
                else if (type == "IDAT") idat.Write(data, offset, length);
                else if (type == "IEND") break;
                offset += length + 4;
            }
            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType is not (2 or 6)) return null;
            idat.Position = 0;
            using var z = new ZLibStream(idat, CompressionMode.Decompress);
            using var rawStream = new MemoryStream();
            z.CopyTo(rawStream);
            var raw = rawStream.ToArray();
            var bpp = colorType == 6 ? 4 : 3;
            var stride = width * bpp;
            var rows = new byte[height * stride];
            var source = 0; var dest = 0;
            for (var row = 0; row < height; row++)
            {
                var filter = raw[source++];
                for (var i = 0; i < stride; i++)
                {
                    var value = raw[source++];
                    var left = i >= bpp ? rows[dest + i - bpp] : (byte)0;
                    var up = row > 0 ? rows[dest + i - stride] : (byte)0;
                    var upLeft = row > 0 && i >= bpp ? rows[dest + i - stride - bpp] : (byte)0;
                    rows[dest + i] = filter switch
                    {
                        0 => value,
                        1 => (byte)(value + left),
                        2 => (byte)(value + up),
                        3 => (byte)(value + ((left + up) >> 1)),
                        4 => (byte)(value + Paeth(left, up, upLeft)),
                        _ => value
                    };
                }
                dest += stride;
            }
            var rgb = new byte[width * height * 3];
            for (var p = 0; p < width * height; p++)
            {
                var src = p * bpp; var dst = p * 3;
                if (colorType == 6)
                {
                    var a = rows[src + 3] / 255m;
                    rgb[dst] = (byte)Math.Round(rows[src] * a + bgR * (1 - a));
                    rgb[dst + 1] = (byte)Math.Round(rows[src + 1] * a + bgG * (1 - a));
                    rgb[dst + 2] = (byte)Math.Round(rows[src + 2] * a + bgB * (1 - a));
                }
                else { rgb[dst] = rows[src]; rgb[dst + 1] = rows[src + 1]; rgb[dst + 2] = rows[src + 2]; }
            }
            return new PngLogo(width, height, rgb);
        }
        catch { return null; }
    }

    private static int ReadBigEndianInt(byte[] data, int offset) => (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    private static byte Paeth(byte a, byte b, byte c)
    {
        var p = a + b - c; var pa = Math.Abs(p - a); var pb = Math.Abs(p - b); var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
    private static void SyncLegacyQuantity(Consumable item)
    {
        item.StockQuantity = item.StockGrams >= int.MaxValue * 1000m
            ? int.MaxValue
            : (int)decimal.Floor(item.StockGrams / 1000m);
    }
    private static List<long> AssignedPrinterIds(Quote quote)
    {
        var ids = new List<long>();
        if (string.IsNullOrWhiteSpace(quote.Notes)) return ids;
        var line = quote.Notes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith("PrinterIds:", StringComparison.OrdinalIgnoreCase));
        if (line is null) return ids;
        foreach (var part in line["PrinterIds:".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (long.TryParse(part, out var id) && id > 0) ids.Add(id);
        return ids;
    }
    private static string FormatWeight(decimal grams)
    {
        var kilos = decimal.Floor(grams / 1000m);
        var remainder = grams - kilos * 1000m;
        return kilos > 0 && remainder > 0 ? $"{kilos:0.##} kg {remainder:0.##} g" : kilos > 0 ? $"{kilos:0.##} kg" : $"{remainder:0.##} g";
    }
    private static string Initial(string value){foreach(char c in value.Trim().Normalize(System.Text.NormalizationForm.FormD)){if(System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark||!char.IsLetterOrDigit(c))continue;return char.ToUpperInvariant(c).ToString();}return"X";}
    private static string Csv(string value)=>$"\"{value.Replace("\"","\"\"")}\"";
}











