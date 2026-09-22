using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/alerts")]
public sealed class AlertsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryAlertDto>>> List(CancellationToken ct)
    {
        var rows = await db.Consumables
            .AsNoTracking()
            .Where(x => x.Active && x.StockGrams <= x.LowStockGrams)
            .OrderBy(x => x.StockGrams)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Category,
                x.Material,
                x.Color,
                x.StockGrams,
                x.LowStockGrams
            })
            .ToListAsync(ct);

        return Ok(rows.Select(x => new InventoryAlertDto(
            x.Id,
            x.Name,
            x.Category,
            x.Material,
            x.Color,
            x.StockGrams,
            x.LowStockGrams,
            x.StockGrams <= 0 ? "out" : "low",
            x.StockGrams <= 0
                ? $"{x.Name} · {x.Material} · {x.Color} está agotado."
                : $"{x.Name} · {x.Material} · {x.Color} tiene solo {FormatWeight(x.StockGrams)} disponibles."
        )).ToList());
    }

    private static string FormatWeight(decimal grams)
    {
        var kilos = decimal.Floor(grams / 1000m);
        var remainder = grams - kilos * 1000m;
        return kilos > 0 && remainder > 0
            ? $"{kilos:0.##} kg {remainder:0.##} g"
            : kilos > 0 ? $"{kilos:0.##} kg" : $"{remainder:0.##} g";
    }
}

