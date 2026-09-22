using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/consumable-material-types")]
public sealed class ConsumableMaterialTypesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.ConsumableMaterialTypes.AsNoTracking();
        if (!includeArchived) query = query.Where(x => x.Active);
        return Ok(await query.OrderBy(x => x.Name).Select(x => new ConsumableMaterialTypeDto(x.Id, x.Name, x.Active)).ToListAsync(ct));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPost]
    public async Task<IActionResult> Create(CreateConsumableMaterialTypeRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "El tipo de material es obligatorio." });
        var item = new ConsumableMaterialType { Name = name, Active = true };
        db.ConsumableMaterialTypes.Add(item);
        try { await db.SaveChangesAsync(ct); return Ok(new ConsumableMaterialTypeDto(item.Id, item.Name, item.Active)); }
        catch (DbUpdateException) { return Conflict(new { message = "Ese tipo de material ya esta registrado." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, CreateConsumableMaterialTypeRequest request, CancellationToken ct)
    {
        var item = await db.ConsumableMaterialTypes.FindAsync([id], ct);
        if (item is null) return NotFound();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "El tipo de material es obligatorio." });
        item.Name = name;
        try { await db.SaveChangesAsync(ct); return Ok(new ConsumableMaterialTypeDto(item.Id, item.Name, item.Active)); }
        catch (DbUpdateException) { return Conflict(new { message = "Ese tipo de material ya esta registrado." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpDelete("{id:long}")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var item = await db.ConsumableMaterialTypes.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Active = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}