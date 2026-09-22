using System.Security.Claims;
using SanjCorp3D.Api.Identity;

namespace SanjCorp3D.Api.Services;

public sealed class TenantContext
{
    public static readonly Guid TechnologyTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string HeaderName = "X-Tenant-Id";
    public const string ClaimType = "sanjcorp:tenant";

    public Guid? CurrentTenantId { get; private set; }
    public Guid? UserTenantId { get; private set; }
    public bool IsSuperAdmin { get; private set; }

    public bool Initialize(ClaimsPrincipal user, string? requestedTenant)
    {
        CurrentTenantId = null;
        UserTenantId = null;
        IsSuperAdmin = user.IsInRole(AppRoles.SuperAdmin);
        if (user.Identity?.IsAuthenticated != true) return true;

        var claim = user.FindFirstValue(ClaimType);
        if (string.IsNullOrWhiteSpace(claim) && !IsSuperAdmin) return false;
        if (!string.IsNullOrWhiteSpace(claim) && !Guid.TryParse(claim, out var userTenant)) return false;
        if (Guid.TryParse(claim, out var parsedTenant)) UserTenantId = parsedTenant;

        if (Guid.TryParse(requestedTenant, out var selected))
        {
            if (!IsSuperAdmin && selected != UserTenantId) return false;
            CurrentTenantId = selected;
        }
        else if (IsSuperAdmin)
        {
            CurrentTenantId = UserTenantId ?? TechnologyTenantId;
        }
        else CurrentTenantId = UserTenantId;
        return CurrentTenantId.HasValue;
    }

    public void Use(Guid tenantId) => CurrentTenantId = tenantId;

}
