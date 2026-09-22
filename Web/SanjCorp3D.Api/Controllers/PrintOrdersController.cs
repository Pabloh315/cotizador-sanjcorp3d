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
                x.Quote.PrintHours, x.Quote.Quantity, x.Quote.RecommendedPrice))
            .ToListAsync(ct);
        return Ok(orders);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).FirstOrDefaultAsync(x => x.Id == id, ct);
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

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/finish")]
    public async Task<IActionResult> Finish(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).FirstOrDefaultAsync(x => x.Id == id, ct);
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

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/cooling-complete")]
    public async Task<IActionResult> CoolingComplete(long id, CancellationToken ct)
    {
        var order = await db.PrintOrders.Include(x => x.Printer).Include(x => x.Quote).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        if (order.Status != Cooling) return Conflict(new { message = "Solo puedes liberar un pedido en enfriamiento." });
        order.Status = Done;
        order.CompletedAtUtc = DateTime.UtcNow;
        if (!await db.PrintOrders.AnyAsync(x => x.PrinterId == order.PrinterId && x.Id != order.Id && (x.Status == Printing || x.Status == Cooling), ct))
            order.Printer.Status = "Disponible";
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(order));
    }

    private static PrintOrderDto ToDto(PrintOrder x) => new(
        x.Id, x.QuoteId, x.Quote.OrderCode, x.Quote.Customer, x.Quote.ProjectName, x.Quote.ProductName,
        x.PrinterId, x.Printer.Name, x.Printer.Status, x.Status,
        x.CreatedAtUtc, x.StartedAtUtc, x.EstimatedFinishedAtUtc, x.FinishedAtUtc, x.CoolingUntilUtc, x.CompletedAtUtc,
        x.Quote.PrintHours, x.Quote.Quantity, x.Quote.RecommendedPrice);
}

