using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.Administrator), Route("api/workspace")]
public sealed class WorkspaceController(AppDbContext db, TenantContext tenantContext) : ControllerBase
{
    public sealed record BrandingRequest(string Name, string? LogoUrl);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenant = await FindTenant(ct);
        return tenant is null ? NotFound() : Ok(ToDto(tenant));
    }

    [HttpPut("branding")]
    public async Task<IActionResult> UpdateBranding(BrandingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
            return BadRequest(new { message = "El nombre de la empresa es obligatorio y admite hasta 120 caracteres." });
        if (!string.IsNullOrWhiteSpace(request.LogoUrl) && request.LogoUrl.Length > 2_000_000)
            return BadRequest(new { message = "El logo es demasiado grande." });

        var tenant = await FindTenant(ct);
        if (tenant is null) return NotFound();
        tenant.Name = request.Name.Trim();
        tenant.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(tenant));
    }

    private async Task<Models.Tenant?> FindTenant(CancellationToken ct)
    {
        var id = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó una empresa.");
        return await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    private static object ToDto(Models.Tenant tenant) => new { tenant.Id, tenant.Name, tenant.Slug, tenant.Kind, tenant.LogoUrl, tenant.Active };
}
