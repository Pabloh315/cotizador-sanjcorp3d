using Microsoft.AspNetCore.Identity;

namespace SanjCorp3D.Api.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    // Only the first Maker account in a tenant may edit that tenant's settings.
    public bool IsMakerOwner { get; set; }
    public bool IsSupremeAdmin { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
