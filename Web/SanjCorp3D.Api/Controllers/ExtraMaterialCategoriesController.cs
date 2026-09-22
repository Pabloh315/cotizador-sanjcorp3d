using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/extra-material-categories")]
public sealed class ExtraMaterialCategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        var query = db.ExtraMaterialCategories.AsNoTracking();
        if (!includeArchived) query = query.Where(x => x.Active);
        return Ok(await query.OrderBy(x => x.Name).Select(x => new ExtraMaterialCategoryDto(x.Id, x.Name, x.Active)).ToListAsync(ct));
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPost]
    public async Task<IActionResult> Create(CreateExtraMaterialCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "La categoria es obligatoria." });
        var item = new ExtraMaterialCategory { Name = name, Active = true };
        db.ExtraMaterialCategories.Add(item);
        try { await db.SaveChangesAsync(ct); return Ok(new ExtraMaterialCategoryDto(item.Id, item.Name, item.Active)); }
        catch (DbUpdateException) { return Conflict(new { message = "Esa categoria ya esta registrada." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, CreateExtraMaterialCategoryRequest request, CancellationToken ct)
    {
        var item = await db.ExtraMaterialCategories.FindAsync([id], ct);
        if (item is null) return NotFound();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "La categoria es obligatoria." });
        item.Name = name;
        try { await db.SaveChangesAsync(ct); return Ok(new ExtraMaterialCategoryDto(item.Id, item.Name, item.Active)); }
        catch (DbUpdateException) { return Conflict(new { message = "Esa categoria ya esta registrada." }); }
    }

    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), HttpDelete("{id:long}")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        var item = await db.ExtraMaterialCategories.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Active = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}