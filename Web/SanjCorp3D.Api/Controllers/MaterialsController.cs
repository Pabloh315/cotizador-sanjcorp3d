using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;

namespace SanjCorp3D.Api.Controllers;

[ApiController,Authorize,Route("api/materials")]
public sealed class MaterialsController(AppDbContext db, UserManager<ApplicationUser> users):ControllerBase
{
    [HttpGet]public async Task<IActionResult>List([FromQuery]bool includeArchived=false,CancellationToken ct=default){var q=db.Materials.AsNoTracking();if(!includeArchived)q=q.Where(x=>x.Active);return Ok(await q.OrderBy(x=>x.Category).ThenBy(x=>x.Name).ToListAsync(ct));}
    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPost]public async Task<IActionResult>Create(ExtraMaterial input,CancellationToken ct){if(!await CanManageAsync())return Forbid();input.Id=0;input.Active=true;Validate(input);db.Materials.Add(input);return await Save(input,ct);}
    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPut("{id:long}")]public async Task<IActionResult>Update(long id,ExtraMaterial input,CancellationToken ct){if(!await CanManageAsync())return Forbid();var item=await db.Materials.FindAsync([id],ct);if(item is null)return NotFound();Validate(input);item.Name=input.Name.Trim();item.Category=input.Category.Trim();item.Unit=input.Unit.Trim();item.UnitPrice=input.UnitPrice;item.Active=input.Active;return await Save(item,ct);}
    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Production},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpDelete("{id:long}")]public async Task<IActionResult>Archive(long id,CancellationToken ct){if(!await CanManageAsync())return Forbid();var item=await db.Materials.FindAsync([id],ct);if(item is null)return NotFound();item.Active=false;await db.SaveChangesAsync(ct);return NoContent();}
    private async Task<IActionResult>Save(ExtraMaterial item,CancellationToken ct){try{await db.SaveChangesAsync(ct);return Ok(item);}catch(DbUpdateException){return Conflict(new{message="Ya existe un material con ese nombre."});}}
    private static void Validate(ExtraMaterial x){if(string.IsNullOrWhiteSpace(x.Name)||string.IsNullOrWhiteSpace(x.Category)||string.IsNullOrWhiteSpace(x.Unit))throw new ArgumentException("Nombre, categoría y unidad son obligatorios.");if(x.UnitPrice<0)throw new ArgumentException("El precio no puede ser negativo.");}
    private async Task<bool> CanManageAsync(){if(User.IsInRole(AppRoles.SuperAdmin)||User.IsInRole(AppRoles.Administrator))return true;var current=await users.GetUserAsync(User);return current?.IsMakerOwner==true&&User.IsInRole(AppRoles.Maker);}
}

