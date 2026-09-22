using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController,Authorize,Route("api/settings")]
public sealed class SettingsController(BusinessSettingsService settings, UserManager<ApplicationUser> users, AppDbContext db, TenantContext tenantContext):ControllerBase
{
    [HttpGet]public async Task<IActionResult>Get(CancellationToken ct)=>Ok(await settings.GetAsync(ct));
    [Authorize(Roles=$"{AppRoles.Administrator},{AppRoles.Maker},{AppRoles.SuperAdmin}"),HttpPut]
    public async Task<IActionResult>Update(BusinessSettingsDto input,CancellationToken ct)
    {
        var current = await users.GetUserAsync(User);
        var tenant = tenantContext.CurrentTenantId.HasValue
            ? await db.Tenants.IgnoreQueryFilters().Where(x => x.Id == tenantContext.CurrentTenantId.Value).Select(x => x.Kind).FirstOrDefaultAsync(ct)
            : null;
        var mayEdit = current is not null &&
            (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Administrator) ||
             (tenant == "maker" && current.IsMakerOwner));
        if (!mayEdit) return Forbid();
        await settings.SaveAsync(input,ct);
        return Ok(await settings.GetAsync(ct));
    }
}
