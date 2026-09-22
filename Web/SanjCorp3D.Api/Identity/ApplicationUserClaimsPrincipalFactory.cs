using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.TenantId.HasValue)
            identity.AddClaim(new Claim(TenantContext.ClaimType, user.TenantId.Value.ToString()));
        if (user.IsSupremeAdmin)
            identity.AddClaim(new Claim(ClaimTypes.Role, AppRoles.SuperAdmin));
        return identity;
    }
}
