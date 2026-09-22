using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize(Roles = AppRoles.SuperAdmin), Route("api/tenants")]
public sealed class TenantsController(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    TenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenants = await db.Tenants.AsNoTracking().OrderBy(x => x.Kind).ThenBy(x => x.Name).ToListAsync(ct);
        var userCounts = await db.Users.AsNoTracking()
            .Where(x => x.TenantId.HasValue)
            .GroupBy(x => x.TenantId!.Value)
            .Select(x => new { TenantId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);
        return Ok(tenants.Select(x => ToDto(x, userCounts.GetValueOrDefault(x.Id))).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMakerTenantRequest request, CancellationToken ct)
    {
        ValidateTenant(request.Name, request.Slug, request.LogoUrl);
        ValidateUser(request.Username, request.DisplayName);
        var slug = Slug(request.Slug);
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug, ct))
            return Conflict(new { message = "Ya existe una cuenta con ese identificador." });

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = slug, Kind = "maker",
                LogoUrl = CleanLogo(request.LogoUrl), Active = true
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);

            // New Maker spaces receive every global printer as a local working
            // copy. Their favorite flags remain independent from other spaces.
            var catalog = await db.Printers.IgnoreQueryFilters().AsNoTracking()
                .GroupBy(x => x.CatalogId).Select(x => x.OrderBy(item => item.Id).First()).ToListAsync(ct);
            tenantContext.Use(tenant.Id);
            db.Printers.AddRange(catalog.Select(x => new Printer
            {
                CatalogId = x.CatalogId, Name = x.Name, BuildX = x.BuildX, BuildY = x.BuildY,
                BuildZ = x.BuildZ, Nozzle = x.Nozzle, Speed = x.Speed, PowerWatts = x.PowerWatts,
                HourlyCost = x.HourlyCost, ColorCount = x.ColorCount, Status = x.Status, IsDefault = false, Active = x.Active
            }));
            await db.SaveChangesAsync(ct);

            var user = new ApplicationUser
            {
                UserName = request.Username.Trim(), DisplayName = request.DisplayName.Trim(),
                Email = NullIfWhiteSpace(request.Email), EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
                TenantId = tenant.Id, Active = true, IsMakerOwner = true
            };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return IdentityError(created);
            var roleResult = await users.AddToRoleAsync(user, AppRoles.Maker);
            if (!roleResult.Succeeded) return IdentityError(roleResult);
            await transaction.CommitAsync(ct);
            return Created($"/api/tenants/{tenant.Id}", ToDto(tenant, 1));
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTenantRequest request, CancellationToken ct)
    {
        ValidateTenant(request.Name, "valid-slug", request.LogoUrl);
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.Kind == "maker", ct);
        if (tenant is null) return NotFound();
        tenant.Name = request.Name.Trim(); tenant.LogoUrl = CleanLogo(request.LogoUrl); tenant.Active = request.Active;
        await db.SaveChangesAsync(ct);
        var count = await db.Users.CountAsync(x => x.TenantId == tenant.Id, ct);
        return Ok(ToDto(tenant, count));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.Kind == "maker", ct);
        if (tenant is null) return NotFound();

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var tenantUsers = await users.Users.Where(x => x.TenantId == id).ToListAsync(ct);
            foreach (var user in tenantUsers)
            {
                var deleted = await users.DeleteAsync(user);
                if (!deleted.Succeeded) return IdentityError(deleted);
            }

            // The context normally points at the selected workspace. Switch it to
            // the target before deleting tenant-scoped entities so the safety stamp
            // does not reject a deliberate supreme-admin deletion.
            tenantContext.Use(id);
            db.SaleConsumables.RemoveRange(await db.SaleConsumables.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Sales.RemoveRange(await db.Sales.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.QuoteConsumables.RemoveRange(await db.QuoteConsumables.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.QuoteMaterials.RemoveRange(await db.QuoteMaterials.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.ChatMessages.RemoveRange(await db.ChatMessages.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.ConsumableStockLots.RemoveRange(await db.ConsumableStockLots.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Quotes.RemoveRange(await db.Quotes.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Printers.RemoveRange(await db.Printers.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Consumables.RemoveRange(await db.Consumables.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Materials.RemoveRange(await db.Materials.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.BusinessSettings.RemoveRange(await db.BusinessSettings.IgnoreQueryFilters().Where(x => x.TenantId == id).ToListAsync(ct));
            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return NoContent();
        });
    }

    [HttpPost("{id:guid}/users")]
    public async Task<IActionResult> CreateUser(Guid id, CreateMakerUserRequest request, CancellationToken ct)
    {
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.Kind == "maker", ct);
        if (tenant is null) return NotFound();
        if (!tenant.Active) return BadRequest(new { message = "La cuenta Maker está desactivada." });
        var currentUsers = await users.Users.CountAsync(x => x.TenantId == tenant.Id, ct);
        if (currentUsers >= 3)
            return BadRequest(new { message = "Cada cuenta Maker puede tener un máximo de tres usuarios." });
        ValidateUser(request.Username, request.DisplayName);
        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(), DisplayName = request.DisplayName.Trim(),
            Email = NullIfWhiteSpace(request.Email), EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
            TenantId = tenant.Id, Active = true, IsMakerOwner = false
        };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return IdentityError(created);
        var roleResult = await users.AddToRoleAsync(user, AppRoles.Maker);
        if (!roleResult.Succeeded) return IdentityError(roleResult);
        return Created($"/api/tenants/{tenant.Id}/users/{user.Id}", new { user.Id, user.UserName, user.DisplayName, user.Email, user.TenantId, Role = AppRoles.Maker });
    }

    private static TenantDto ToDto(Tenant tenant, int users) => new(tenant.Id, tenant.Name, tenant.Slug, tenant.Kind, tenant.LogoUrl, tenant.Active, tenant.CreatedAtUtc, users);

    private static void ValidateTenant(string name, string slug, string? logo)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("El nombre de la cuenta es obligatorio y admite hasta 120 caracteres.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > 80) throw new ArgumentException("El identificador de la cuenta es obligatorio y admite hasta 80 caracteres.");
        if (!Slug(slug).All(x => char.IsLetterOrDigit(x) || x == '-')) throw new ArgumentException("El identificador solo puede usar letras, números y guiones.");
        if (!string.IsNullOrWhiteSpace(logo) && logo.Length > 2_000_000) throw new ArgumentException("El logo es demasiado grande.");
    }

    private static void ValidateUser(string username, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Usuario y nombre son obligatorios.");
    }

    private static string Slug(string value) => value.Trim().ToLowerInvariant();
    private static string? CleanLogo(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private ObjectResult IdentityError(IdentityResult result) => BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
}


