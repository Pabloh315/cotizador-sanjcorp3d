namespace SanjCorp3D.Api.Identity;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string SuperAdmin = "SuperAdmin";
    public const string Maker = "Maker";
    public const string Sales = "Sales";
    public const string Production = "Production";
    public const string Viewer = "Viewer";
    public static readonly string[] All = [SuperAdmin, Maker, Administrator, Sales, Production, Viewer];
}
