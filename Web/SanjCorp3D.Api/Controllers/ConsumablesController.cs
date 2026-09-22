using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/consumables")]
public sealed class ConsumablesController(AppDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.Consumables.AsNoTracking();
        if (!includeArchived) query = query.Where(x => x.Active);
        return Ok(await query
            .OrderBy(x => x.Category)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Material)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Color)
            .ToListAsync(ct));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost]
    public async Task<IActionResult> Create(Consumable input, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        input.Id = 0;
        input.Active = true;
        NormalizeStock(input);
        Validate(input);
        await ClearDefault(input, ct);
        db.Consumables.Add(input);
        var result = await Save(input, ct);
        if (result is OkObjectResult && input.StockGrams > 0)
        {
            db.ConsumableStockLots.Add(new ConsumableStockLot { Consumable = input, OriginalGrams = input.StockGrams, RemainingGrams = input.StockGrams, PricePerKilogram = input.PricePerUnit });
            await db.SaveChangesAsync(ct);
        }
        return result;
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, Consumable input, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        var item = await db.Consumables.FindAsync([id], ct);
        if (item is null) return NotFound();
        NormalizeStock(input);
        Validate(input);
        await ClearDefault(input, ct);
        item.Name = input.Name.Trim();
        item.Category = input.Category.Trim();
        item.Material = input.Material.Trim();
        item.Color = input.Color.Trim();
        item.PricePerUnit = input.PricePerUnit;
        item.Density = input.Density;
        item.IsDefault = input.IsDefault;
        item.Active = input.Active;
        item.StockGrams = input.StockGrams;
        item.LowStockGrams = input.LowStockGrams;
        SyncLegacyQuantity(item);
        await ReplaceLots(item, ct);
        return await Save(item, ct);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPatch("{id:long}/stock")]
    public async Task<IActionResult> Stock(long id, [FromBody] StockRequest request, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        var item = await db.Consumables.FindAsync([id], ct);
        if (item is null) return NotFound();

        decimal total = request.StockGrams
            ?? (request.Quantity.HasValue ? request.Quantity.Value * 1000m : item.StockGrams);
        decimal threshold = request.LowStockGrams ?? item.LowStockGrams;
        if (total < 0 || threshold < 0)
            return BadRequest(new { message = "La existencia y el umbral no pueden ser negativos." });

        item.StockGrams = decimal.Round(total, 4, MidpointRounding.AwayFromZero);
        item.LowStockGrams = decimal.Round(threshold, 4, MidpointRounding.AwayFromZero);
        SyncLegacyQuantity(item);
        await ReplaceLots(item, ct);
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/stock/add")]
    public async Task<IActionResult> AddStock(long id, [FromBody] StockAdditionRequest request, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        if (request.Kilograms < 0 || request.Grams < 0 || request.PricePerKilogram < 0)
            return BadRequest(new { message = "Los kilos y gramos a agregar no pueden ser negativos." });

        var item = await db.Consumables.FindAsync([id], ct);
        if (item is null) return NotFound();
        var incomingGrams = decimal.Round(request.Kilograms * 1000m + request.Grams, 4, MidpointRounding.AwayFromZero);
        if (incomingGrams <= 0) return BadRequest(new { message = "Indica una cantidad mayor que cero." });
        var lots = await db.ConsumableStockLots.Where(x => x.ConsumableId == item.Id).OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
        if (lots.Count == 0 && item.StockGrams > 0)
        {
            lots.Add(new ConsumableStockLot { TenantId = item.TenantId, ConsumableId = item.Id, OriginalGrams = item.StockGrams, RemainingGrams = item.StockGrams, PricePerKilogram = item.PricePerUnit });
            db.ConsumableStockLots.Add(lots[0]);
        }
        var incomingPrice = request.PricePerKilogram > 0 ? request.PricePerKilogram : item.PricePerUnit;
        if (item.StockGrams > 0 && incomingPrice > item.PricePerUnit)
        {
            var blendGrams = Math.Min(500m, item.StockGrams);
            var left = blendGrams;
            for (var index = lots.Count - 1; index >= 0 && left > 0; index--)
            {
                var take = Math.Min(left, lots[index].RemainingGrams);
                lots[index].RemainingGrams -= take;
                left -= take;
            }
            db.ConsumableStockLots.Add(new ConsumableStockLot { TenantId = item.TenantId, ConsumableId = item.Id, OriginalGrams = blendGrams + incomingGrams, RemainingGrams = blendGrams + incomingGrams, PricePerKilogram = decimal.Round((item.PricePerUnit + incomingPrice) / 2m, 4, MidpointRounding.AwayFromZero) });
            item.PricePerUnit = decimal.Round((item.PricePerUnit + incomingPrice) / 2m, 4, MidpointRounding.AwayFromZero);
        }
        else
        {
            db.ConsumableStockLots.Add(new ConsumableStockLot { TenantId = item.TenantId, ConsumableId = item.Id, OriginalGrams = incomingGrams, RemainingGrams = incomingGrams, PricePerKilogram = incomingPrice });
            item.PricePerUnit = incomingPrice;
        }
        item.StockGrams = decimal.Round(item.StockGrams + incomingGrams, 4, MidpointRounding.AwayFromZero);
        SyncLegacyQuantity(item);
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpPost("{id:long}/loss")]
    public async Task<IActionResult> RegisterLoss(long id, [FromBody] InventoryLossRequest request, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        if (request.Grams <= 0) return BadRequest(new { message = "Indica una pérdida mayor que cero gramos." });
        var item = await db.Consumables.FindAsync([id], ct);
        if (item is null) return NotFound();
        if (item.StockGrams + 0.0001m < request.Grams)
            return BadRequest(new { message = "La pérdida supera la existencia disponible." });
        var lots = await db.ConsumableStockLots.Where(x => x.ConsumableId == id).OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
        var left = request.Grams;
        foreach (var lot in lots)
        {
            if (left <= 0) break;
            var take = Math.Min(left, lot.RemainingGrams);
            lot.RemainingGrams -= take;
            left -= take;
        }
        item.StockGrams = decimal.Round(item.StockGrams - request.Grams, 4, MidpointRounding.AwayFromZero);
        SyncLegacyQuantity(item);
        db.InventoryLosses.Add(new InventoryLoss { TenantId = item.TenantId, ConsumableId = item.Id, Grams = request.Grams, Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Producto fallido" : request.Reason.Trim() });
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"), HttpDelete("{id:long}")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        if (!await CanManageAsync()) return Forbid();
        var item = await db.Consumables.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Active = false;
        item.IsDefault = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record StockRequest(decimal? StockGrams, decimal? LowStockGrams, int? Quantity);
    public sealed record StockAdditionRequest(decimal Kilograms, decimal Grams, decimal PricePerKilogram = 0);

    private async Task<bool> CanManageAsync()
    {
        if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Administrator)) return true;
        var current = await users.GetUserAsync(User);
        return current?.IsMakerOwner == true && User.IsInRole(AppRoles.Maker);
    }

    private async Task ClearDefault(Consumable input, CancellationToken ct)
    {
        if (input.IsDefault)
            await db.Consumables.Where(x => x.IsDefault).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, false), ct);
    }

    private async Task<IActionResult> Save(Consumable item, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Ok(ToResponse(item));
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Ya existe ese consumible, material y color." });
        }
    }

    private async Task ReplaceLots(Consumable item, CancellationToken ct)
    {
        var current = await db.ConsumableStockLots.Where(x => x.ConsumableId == item.Id).ToListAsync(ct);
        db.ConsumableStockLots.RemoveRange(current);
        if (item.StockGrams > 0)
            db.ConsumableStockLots.Add(new ConsumableStockLot { ConsumableId = item.Id, OriginalGrams = item.StockGrams, RemainingGrams = item.StockGrams, PricePerKilogram = item.PricePerUnit });
    }

    private static object ToResponse(Consumable item) => new
    {
        item.Id, item.TenantId, item.Name, item.Category, item.Material, item.Color,
        item.PricePerUnit, item.Density, item.IsDefault, item.Active, item.StockQuantity,
        item.StockGrams, item.LowStockGrams
    };

    private static void NormalizeStock(Consumable item)
    {
        // Older clients sent StockQuantity as a count of 1 kg spools.
        // Translate it only when the new total-grams field was not supplied.
        if (item.StockGrams <= 0 && item.StockQuantity > 0)
            item.StockGrams = item.StockQuantity * 1000m;
        item.StockGrams = decimal.Round(item.StockGrams, 4, MidpointRounding.AwayFromZero);
        item.LowStockGrams = decimal.Round(item.LowStockGrams, 4, MidpointRounding.AwayFromZero);
        SyncLegacyQuantity(item);
    }

    private static void SyncLegacyQuantity(Consumable item)
    {
        item.StockQuantity = item.StockGrams >= int.MaxValue * 1000m
            ? int.MaxValue
            : (int)decimal.Floor(item.StockGrams / 1000m);
    }

    private static void Validate(Consumable item)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Category) ||
            string.IsNullOrWhiteSpace(item.Material) || string.IsNullOrWhiteSpace(item.Color))
            throw new ArgumentException("Nombre, categoría, material y color son obligatorios.");
        if (item.PricePerUnit < 0 || item.Density <= 0 || item.StockGrams < 0 || item.LowStockGrams < 0)
            throw new ArgumentException("Precio, densidad, existencia o umbral no son válidos.");
    }
}

