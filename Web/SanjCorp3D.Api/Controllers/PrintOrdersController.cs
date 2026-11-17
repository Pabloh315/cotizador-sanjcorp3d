using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/print-orders")]
public sealed class PrintOrdersController(AppDbContext db) : ControllerBase
{
    private const string Pending = "Pendiente";
    private const string Printing = "Imprimiendo";
    private const string Cooling = "Enfriamiento";
    private const string Done = "Terminado";
    private const string Cancelled = "Cancelado";

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orders = await db.PrintOrders.AsNoTracking()
            .Include(x => x.Quote)
            .Include(x => x.Printer)
            .OrderBy(x => x.Printer.Name)
            .ThenBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new PrintOrderDto(
                x.Id, x.QuoteId, x.Quote.OrderCode, x.Quote.Customer, x.Quote.ProjectName, x.Quote.ProductName,
                x.PrinterId, x.Printer.Name, x.Printer.Status, x.Status,
                x.CreatedAtUtc, x.StartedAtUtc, x.EstimatedFinishedAtUtc, x.FinishedAtUtc, x.CoolingUntilUtc, x.CompletedAtUtc,
                x.Quote.PrintHours, x.Quote.Quantity, x.Quote.RecommendedPrice,
                x.Quote.Consumables.Select(line => new PrintOrderConsumableDto(line.LegacyConsumableId, line.Name, line.Material, line.Color, line.Grams * x.Quote.Quantity)).ToList()))
            .ToListAsync(ct);
        return Ok(orders);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Inventory},{AppRoles.Production},{AppRoles.Maker}"), HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).ThenInclude(x => x.Consumables).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != Pending) return Conflict(new { message = "Solo puedes empezar un pedido pendiente." });
        if (await db.PrintOrders.AnyAsync(x => x.PrinterId == order.PrinterId && (x.Status == Printing || x.Status == Cooling), ct))
            return Conflict(new { message = "Esta impresora todavía tiene una impresión activa o en enfriamiento." });
        var olderPending = await db.PrintOrders.Where(x => x.PrinterId == order.PrinterId && x.Status == Pending && (x.CreatedAtUtc < order.CreatedAtUtc || (x.CreatedAtUtc == order.CreatedAtUtc && x.Id < order.Id)))
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync(ct);
        if (olderPending != 0)
            return Conflict(new { message = $"Primero debe iniciar el pedido #{olderPending}, que está antes en la fila." });

        var now = DateTime.UtcNow;
        order.Status = Printing;
        order.StartedAtUtc = now;
        order.EstimatedFinishedAtUtc = now.AddMinutes((double)Math.Max(order.Quote.PrintHours, 0) * 60d);
        order.Printer.Status = "En uso";
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(order));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Inventory},{AppRoles.Production},{AppRoles.Maker}"), HttpPost("{id:long}/finish")]
    public async Task<IActionResult> Finish(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).ThenInclude(x => x.Consumables).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != Printing) return Conflict(new { message = "Solo puedes terminar un pedido que está imprimiendo." });
        var now = DateTime.UtcNow;
        order.Status = Cooling;
        order.FinishedAtUtc = now;
        order.CoolingUntilUtc = now.AddMinutes(15);
        order.Printer.Status = "En mantenimiento";
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(order));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Inventory},{AppRoles.Production},{AppRoles.Maker}"), HttpPost("{id:long}/cooling-complete")]
    public async Task<IActionResult> CoolingComplete(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).ThenInclude(x => x.Consumables).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != Cooling) return Conflict(new { message = "Solo puedes liberar un pedido en enfriamiento." });
        order.Status = Done;
        order.CompletedAtUtc = DateTime.UtcNow;
        if (!await db.PrintOrders.AnyAsync(x => x.PrinterId == order.PrinterId && x.Id != order.Id && (x.Status == Printing || x.Status == Cooling), ct))
            order.Printer.Status = "Disponible";
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(order));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Inventory},{AppRoles.Production},{AppRoles.Maker}"), HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancelPrintOrderRequest request, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).ThenInclude(x => x.Consumables).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return NotFound();
            if (order.Status != Printing && order.Status != Cooling) return Conflict(new { message = "Solo puedes cancelar una impresión iniciada o en enfriamiento." });
            if (request.Consumables is null || request.Consumables.Count == 0) return BadRequest(new { message = "Indica cuánto material se perdió." });
            var usage = request.Consumables.Where(x => x.Grams > 0).GroupBy(x => x.ConsumableId)
                .Select(group => new { ConsumableId = group.Key, Grams = decimal.Round(group.Sum(x => x.Grams), 4, MidpointRounding.AwayFromZero) }).ToList();
            if (usage.Count == 0) return BadRequest(new { message = "La pérdida debe ser mayor que cero gramos." });
            var allowedIds = order.Quote.Consumables.Where(x => x.LegacyConsumableId > 0).Select(x => x.LegacyConsumableId).Distinct().ToHashSet();
            if (usage.Any(x => !allowedIds.Contains(x.ConsumableId))) return BadRequest(new { message = "Uno de los materiales no pertenece a esta impresión." });
            var ids = usage.Select(x => x.ConsumableId).ToArray();
            var inventory = await db.Consumables.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var lots = await db.ConsumableStockLots.Where(x => ids.Contains(x.ConsumableId)).OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
            foreach (var loss in usage)
            {
                if (!inventory.TryGetValue(loss.ConsumableId, out var item)) return Conflict(new { message = "Uno de los materiales ya no existe en el inventario." });
                if (item.StockGrams + 0.0001m < loss.Grams) return Conflict(new { message = $"Stock insuficiente para registrar la pérdida de {item.Name} - {item.Color}. Disponible: {item.StockGrams:0.##} g." });
            }
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Falla durante la impresión" : request.Reason.Trim();
            if (reason.Length > 240) return BadRequest(new { message = "El motivo admite hasta 240 caracteres." });
            foreach (var loss in usage)
            {
                var item = inventory[loss.ConsumableId];
                var left = loss.Grams;
                foreach (var lot in lots.Where(x => x.ConsumableId == item.Id))
                {
                    if (left <= 0) break;
                    var take = Math.Min(left, lot.RemainingGrams);
                    lot.RemainingGrams -= take;
                    left -= take;
                }
                item.StockGrams = decimal.Round(item.StockGrams - loss.Grams, 4, MidpointRounding.AwayFromZero);
                SyncLegacyQuantity(item);
                db.InventoryLosses.Add(new InventoryLoss { ConsumableId = item.Id, Grams = loss.Grams, Reason = $"Impresión {order.Quote.OrderCode}: {reason}" });
            }
            var now = DateTime.UtcNow;
            order.Status = Cancelled;
            order.FinishedAtUtc ??= now;
            order.CompletedAtUtc = now;
            order.CoolingUntilUtc = null;
            order.Printer.Status = "Disponible";
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(ToDto(order));
        });
    }

    private static PrintOrderDto ToDto(PrintOrder x) => new(
        x.Id, x.QuoteId, x.Quote.OrderCode, x.Quote.Customer, x.Quote.ProjectName, x.Quote.ProductName,
        x.PrinterId, x.Printer.Name, x.Printer.Status, x.Status,
        x.CreatedAtUtc, x.StartedAtUtc, x.EstimatedFinishedAtUtc, x.FinishedAtUtc, x.CoolingUntilUtc, x.CompletedAtUtc,
        x.Quote.PrintHours, x.Quote.Quantity, x.Quote.RecommendedPrice,
        x.Quote.Consumables.Select(line => new PrintOrderConsumableDto(line.LegacyConsumableId, line.Name, line.Material, line.Color, line.Grams * x.Quote.Quantity)).ToList());

    private static void SyncLegacyQuantity(Consumable item)
    {
        item.StockQuantity = item.StockGrams >= int.MaxValue * 1000m
            ? int.MaxValue
            : (int)decimal.Floor(item.StockGrams / 1000m);
    }
}
