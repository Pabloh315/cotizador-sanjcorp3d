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

        if (await users.FindByNameAsync(request.Username.Trim()) is not null)
            return Conflict(new { message = "Ese nombre de usuario ya estÃƒÆ’Ã‚Â¡ en uso. Elige uno diferente." });

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

            var user = new ApplicationUser
            {
                UserName = request.Username.Trim(), DisplayName = request.DisplayName.Trim(),
                Email = NullIfWhiteSpace(request.Email), EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email),
                TenantId = tenant.Id, Active = true, IsMakerOwner = true
            };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return IdentityError(created);
            var roleResult = await users.AddToRoleAsync(user, AppRoles.Administrator);
            if (!roleResult.Succeeded) return IdentityError(roleResult);
            await transaction.CommitAsync(ct);
            return Created($"/api/tenants/{tenant.Id}", ToDto(tenant, 1));
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTenantRequest request, CancellationToken ct)
    {
        ValidateTenant(request.Name, "valid-slug", request.LogoUrl);
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
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

    public sealed record AdministratorRequest(string Username, string DisplayName, string? Email, string? Password, bool Active = true);

    [HttpGet("{id:guid}/administrators")]
    public async Task<IActionResult> ListAdministrators(Guid id, CancellationToken ct)
    {
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == id, ct)) return NotFound();
        var items = await users.Users.Where(x => x.TenantId == id).OrderBy(x => x.UserName).ToListAsync(ct);
        var result = new List<object>();
        foreach (var item in items)
            if (await users.IsInRoleAsync(item, AppRoles.Administrator)) result.Add(AdministratorDto(item));
        return Ok(result);
    }

    [HttpPost("{id:guid}/administrators")]
    public async Task<IActionResult> CreateAdministrator(Guid id, AdministratorRequest request, CancellationToken ct)
    {
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == id && x.Active, ct)) return NotFound();
        ValidateUser(request.Username, request.DisplayName);
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "La contrasena inicial es obligatoria." });
        if (await users.FindByNameAsync(request.Username.Trim()) is not null) return Conflict(new { message = "Ese usuario ya esta en uso." });
        var user = new ApplicationUser { UserName = request.Username.Trim(), DisplayName = request.DisplayName.Trim(), Email = NullIfWhiteSpace(request.Email), EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email), TenantId = id, Active = request.Active, IsMakerOwner = false };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return IdentityError(created);
        var assigned = await users.AddToRoleAsync(user, AppRoles.Administrator);
        if (!assigned.Succeeded) { await users.DeleteAsync(user); return IdentityError(assigned); }
        return Created($"/api/tenants/{id}/administrators/{user.Id}", AdministratorDto(user));
    }

    [HttpPut("{id:guid}/administrators/{userId:guid}")]
    public async Task<IActionResult> UpdateAdministrator(Guid id, Guid userId, AdministratorRequest request, CancellationToken ct)
    {
        ValidateUser(request.Username, request.DisplayName);
        var user = await users.Users.FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == id, ct);
        if (user is null || !await users.IsInRoleAsync(user, AppRoles.Administrator)) return NotFound();
        if (!request.Active && await ActiveAdministratorCount(id) <= 1) return BadRequest(new { message = "La empresa debe mantener al menos un administrador activo." });
        var currentName = user.UserName ?? string.Empty;
        if (!string.Equals(currentName, request.Username.Trim(), StringComparison.OrdinalIgnoreCase) && await users.FindByNameAsync(request.Username.Trim()) is not null) return Conflict(new { message = "Ese usuario ya esta en uso." });
        user.UserName = request.Username.Trim(); user.DisplayName = request.DisplayName.Trim(); user.Email = NullIfWhiteSpace(request.Email); user.EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email); user.Active = request.Active;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded) return IdentityError(updated);
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var reset = await users.ResetPasswordAsync(user, token, request.Password);
            if (!reset.Succeeded) return IdentityError(reset);
        }
        return Ok(AdministratorDto(user));
    }

    [HttpDelete("{id:guid}/administrators/{userId:guid}")]
    public async Task<IActionResult> DeleteAdministrator(Guid id, Guid userId, CancellationToken ct)
    {
        var user = await users.Users.FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == id, ct);
        if (user is null || !await users.IsInRoleAsync(user, AppRoles.Administrator)) return NotFound();
        if (await ActiveAdministratorCount(id) <= 1 && user.Active) return BadRequest(new { message = "La empresa debe mantener al menos un administrador activo." });
        var deleted = await users.DeleteAsync(user);
        return deleted.Succeeded ? NoContent() : IdentityError(deleted);
    }

    private async Task<int> ActiveAdministratorCount(Guid tenantId)
    {
        var admins = await users.GetUsersInRoleAsync(AppRoles.Administrator);
        return admins.Count(x => x.TenantId == tenantId && x.Active);
    }

    private static object AdministratorDto(ApplicationUser user) => new { user.Id, user.UserName, user.DisplayName, user.Email, user.Active, user.CreatedAtUtc, Role = AppRoles.Administrator };
    private static TenantDto ToDto(Tenant tenant, int users) => new(tenant.Id, tenant.Name, tenant.Slug, tenant.Kind, tenant.LogoUrl, tenant.Active, tenant.CreatedAtUtc, users);

    private static void ValidateTenant(string name, string slug, string? logo)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("El nombre de la cuenta es obligatorio y admite hasta 120 caracteres.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > 80) throw new ArgumentException("El identificador de la cuenta es obligatorio y admite hasta 80 caracteres.");
        if (!Slug(slug).All(x => char.IsLetterOrDigit(x) || x == '-')) throw new ArgumentException("El identificador solo puede usar letras, nÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Âºmeros y guiones.");
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
