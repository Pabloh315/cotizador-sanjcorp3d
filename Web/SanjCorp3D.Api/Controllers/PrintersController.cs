using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/printers")]
public sealed class PrintersController(AppDbContext db, TenantContext tenantContext) : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["Disponible", "En uso", "En mantenimiento", "Fuera de uso"];

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        List<Printer> items;
        if (User.IsInRole(AppRoles.SuperAdmin))
        {
            var all = await db.Printers.IgnoreQueryFilters().AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
            items = all.GroupBy(x => x.CatalogId).Select(x => x.First()).ToList();
        }
        else items = await db.Printers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

        if (!includeArchived) items = items.Where(x => x.Active).ToList();
        return Ok(items.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPost]
    public async Task<IActionResult> Create(Printer input, CancellationToken ct)
    {
        input.Id = 0;
        input.Active = true;
        input.IsDefault = false;
        input.Status = NormalizeStatus(input.Status);
        Validate(input);

        if (!User.IsInRole(AppRoles.SuperAdmin))
        {
            input.CatalogId = Guid.NewGuid();
            db.Printers.Add(input);
            await db.SaveChangesAsync(ct);
            return Ok(input);
        }

        var existing = await db.Printers.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Name.ToLower() == input.Name.Trim().ToLower())
            .OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.Active) return Conflict(new { message = "Ya existe una impresora con ese nombre en el catalogo global." });
            await UpdateCatalog(existing.CatalogId, input, true, ct);
            input.Id = existing.Id;
            input.TenantId = existing.TenantId;
            input.CatalogId = existing.CatalogId;
            return Ok(input);
        }

        var catalogId = Guid.NewGuid();
        var tenantIds = await db.Tenants.IgnoreQueryFilters().Select(x => x.Id).ToListAsync(ct);
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            Printer? first = null;
            foreach (var tenantId in tenantIds)
            {
                tenantContext.Use(tenantId);
                var item = Copy(input, catalogId);
                db.Printers.Add(item);
                await db.SaveChangesAsync(ct);
                first ??= item;
            }
            await transaction.CommitAsync(ct);
            return Ok(first ?? input);
        });
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, Printer input, CancellationToken ct)
    {
        input.Status = NormalizeStatus(input.Status);
        Validate(input);

        if (!User.IsInRole(AppRoles.SuperAdmin))
        {
            var item = await db.Printers.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return NotFound();
            if (await db.Printers.AnyAsync(x => x.Id != id && x.Name.ToLower() == input.Name.Trim().ToLower(), ct))
                return Conflict(new { message = "Ya existe una impresora con ese nombre." });
            Apply(item, input, input.Active);
            await db.SaveChangesAsync(ct);
            return Ok(item);
        }

        var source = await db.Printers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (source is null) return NotFound();
        if (await NameExists(input.Name, source.CatalogId, ct)) return Conflict(new { message = "Ya existe una impresora con ese nombre en el catalogo global." });

        await UpdateCatalog(source.CatalogId, input, input.Active, ct);
        return Ok(input);
    }

    private async Task UpdateCatalog(Guid catalogId, Printer input, bool active, CancellationToken ct)
    {
        await db.Printers.IgnoreQueryFilters().Where(x => x.CatalogId == catalogId).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Name, input.Name.Trim())
            .SetProperty(x => x.BuildX, input.BuildX)
            .SetProperty(x => x.BuildY, input.BuildY)
            .SetProperty(x => x.BuildZ, input.BuildZ)
            .SetProperty(x => x.Nozzle, input.Nozzle)
            .SetProperty(x => x.Speed, input.Speed)
            .SetProperty(x => x.PowerWatts, input.PowerWatts)
            .SetProperty(x => x.HourlyCost, input.HourlyCost)
            .SetProperty(x => x.Status, input.Status)
            .SetProperty(x => x.Active, active), ct);
        if (!active)
            await db.Printers.IgnoreQueryFilters().Where(x => x.CatalogId == catalogId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, false), ct);
    }

    private async Task<bool> NameExists(string name, Guid exceptCatalogId, CancellationToken ct)
    {
        var normalized = name.Trim().ToLower();
        return await db.Printers.IgnoreQueryFilters().AnyAsync(x => x.Name.ToLower() == normalized && x.CatalogId != exceptCatalogId, ct);
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpDelete("{id:long}")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        if (!User.IsInRole(AppRoles.SuperAdmin))
        {
            var item = await db.Printers.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return NotFound();
            item.Active = false;
            item.IsDefault = false;
            item.Status = "Fuera de uso";
            await db.SaveChangesAsync(ct);
            return NoContent();
        }

        var source = await db.Printers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (source is null) return NotFound();
        await db.Printers.IgnoreQueryFilters().Where(x => x.CatalogId == source.CatalogId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.Active, false).SetProperty(p => p.IsDefault, false).SetProperty(p => p.Status, "Fuera de uso"), ct);
        return NoContent();
    }

    [HttpPut("{id:long}/favorite")]
    public async Task<IActionResult> Favorite(long id, FavoritePrinterRequest request, CancellationToken ct)
    {
        if (User.IsInRole(AppRoles.SuperAdmin)) return BadRequest(new { message = "Selecciona las favoritas desde una cuenta de trabajo." });
        var item = await db.Printers.FirstOrDefaultAsync(x => x.Id == id && x.Active, ct);
        if (item is null) return NotFound();
        if (request.Favorite && item.Status != "Disponible") return BadRequest(new { message = "Solo una máquina disponible puede marcarse como favorita." });
        item.IsDefault = request.Favorite;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    private static Printer Copy(Printer source, Guid catalogId) => new()
    {
        CatalogId = catalogId,
        Name = source.Name.Trim(),
        BuildX = source.BuildX,
        BuildY = source.BuildY,
        BuildZ = source.BuildZ,
        Nozzle = source.Nozzle,
        Speed = source.Speed,
        PowerWatts = source.PowerWatts,
        HourlyCost = source.HourlyCost,
        Status = source.Status,
        IsDefault = false,
        Active = source.Active
    };

    private static void Apply(Printer target, Printer input, bool active)
    {
        target.Name = input.Name.Trim();
        target.BuildX = input.BuildX;
        target.BuildY = input.BuildY;
        target.BuildZ = input.BuildZ;
        target.Nozzle = input.Nozzle;
        target.Speed = input.Speed;
        target.PowerWatts = input.PowerWatts;
        target.HourlyCost = input.HourlyCost;
        target.Status = input.Status;
        target.Active = active;
        if (!active || target.Status != "Disponible") target.IsDefault = false;
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Disponible";
        var normalized = AllowedStatuses.FirstOrDefault(x => string.Equals(x, status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (normalized is null) throw new ArgumentException("El estado de la máquina no es válido.");
        return normalized;
    }

    private static void Validate(Printer item)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 100)
            throw new ArgumentException("El nombre es obligatorio y admite hasta 100 caracteres.");
        if (!AllowedStatuses.Contains(item.Status)) throw new ArgumentException("El estado de la máquina no es válido.");
        if (item.BuildX <= 0 || item.BuildY <= 0 || item.BuildZ <= 0 || item.Nozzle <= 0 || item.Speed <= 0 || item.PowerWatts < 0 || item.HourlyCost < 0 || item.ColorCount < 1)
            throw new ArgumentException("Revisa dimensiones, boquilla, velocidad, potencia, costo y cantidad de colores.");
    }
}

public sealed record FavoritePrinterRequest(bool Favorite);
