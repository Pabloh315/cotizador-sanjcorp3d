using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.SuperAdmin}"), Route("api/users")]
public sealed class UsersController(UserManager<ApplicationUser> users, TenantContext tenantContext) : ControllerBase
{
    public sealed record ResetPasswordRequest(string Password);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var items = await users.Users.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.UserName).ToListAsync(cancellationToken);
        var result = new List<object>(items.Count);
        foreach (var item in items)
        {
            result.Add(ToDto(item, await users.GetRolesAsync(item)));
        }
        return Ok(result);
    }

    [HttpGet("roles")]
    public IActionResult Roles() => Ok(new[] { AppRoles.Administrator, AppRoles.Sales, AppRoles.Production, AppRoles.Viewer });

    [Authorize(Roles = AppRoles.SuperAdmin), HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        Validate(request.Username, request.DisplayName, request.Role);
        if (tenantContext.CurrentTenantId is null) throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            ProfilePhotoUrl = NormalizePhoto(request.ProfilePhotoUrl),
            Email = NullIfWhiteSpace(request.Email),
            EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email), TenantId = tenantContext.CurrentTenantId,
            Active = true
        };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return IdentityError(created);
        var roleResult = await users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(user);
            return IdentityError(roleResult);
        }
        return Created($"/api/users/{user.Id}", ToDto(user, [request.Role]));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request)
    {
        Validate("existing", request.DisplayName, request.Role);
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var user = await users.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (user is null) return NotFound();
        bool isSelf = User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString();
        var currentRoles = await users.GetRolesAsync(user);
        bool losesAdmin = currentRoles.Contains(AppRoles.Administrator) && (!request.Active || request.Role != AppRoles.Administrator);
        if (isSelf && !request.Active) return BadRequest(new { message = "No puedes desactivar tu propia cuenta." });
        if (isSelf && request.Role != AppRoles.Administrator) return BadRequest(new { message = "No puedes quitarte tu propio permiso de administrador." });
        if (losesAdmin && await IsLastActiveAdministrator(user)) return BadRequest(new { message = "Debe quedar al menos un administrador activo." });

        user.DisplayName = request.DisplayName.Trim();
        user.ProfilePhotoUrl = NormalizePhoto(request.ProfilePhotoUrl);
        user.Email = NullIfWhiteSpace(request.Email);
        user.EmailConfirmed = !string.IsNullOrWhiteSpace(request.Email);
        user.Active = request.Active;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded) return IdentityError(updated);
        if (!currentRoles.SequenceEqual([request.Role]))
        {
            var removed = await users.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded) return IdentityError(removed);
            var added = await users.AddToRoleAsync(user, request.Role);
            if (!added.Succeeded) return IdentityError(added);
        }
        return Ok(ToDto(user, [request.Role]));
    }

    [HttpPost("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var user = await users.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (user is null) return NotFound();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, request.Password);
        return result.Succeeded ? NoContent() : IdentityError(result);
    }

    [Authorize(Roles = AppRoles.SuperAdmin), HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var user = await users.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (user is null) return NotFound();
        var result = await users.DeleteAsync(user);
        return result.Succeeded ? NoContent() : IdentityError(result);
    }

    private async Task<bool> IsLastActiveAdministrator(ApplicationUser target)
    {
        var tenantId = tenantContext.CurrentTenantId;
        var admins = await users.GetUsersInRoleAsync(AppRoles.Administrator);
        return admins.Count(x => x.Active && x.Id != target.Id && x.TenantId == tenantId) == 0;
    }

    private static object ToDto(ApplicationUser user, IEnumerable<string> roles) => new
    {
        user.Id, user.UserName, user.DisplayName, user.Email, user.ProfilePhotoUrl, user.TenantId, user.IsMakerOwner, user.Active, user.CreatedAtUtc,
        user.LastLoginAtUtc, user.TwoFactorEnabled, Role = roles.FirstOrDefault() ?? AppRoles.Viewer
    };

    private static void Validate(string username, string displayName, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Usuario y nombre son obligatorios.");
        if (!AppRoles.All.Contains(role)) throw new ArgumentException("El rol seleccionado no es válido.");
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizePhoto(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length > 2_000_000 ? throw new ArgumentException("La foto de perfil es demasiado grande.") : value;
    private ObjectResult IdentityError(IdentityResult result) => BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
}


