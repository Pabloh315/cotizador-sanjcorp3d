using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Identity;

public static class IdentitySeeder
{
    private const string OldSupremeUsername = "richi_sanj_flo";
    private const string SeedPasswordPrefix = "SeedUsers";

    private static readonly SeedUserTemplate[] SeedUserTemplates =
    [
        new("super_admin", "Super Admin", AppRoles.SuperAdmin, true, "superadmin123"),
        new("admin1", "Administrador 1", AppRoles.Administrator, false, "admin1234"),
        new("ventas1", "Ventas 1", AppRoles.Sales, false, "ventas1234"),
        new("produccion1", "Produccion 1", AppRoles.Production, false, "produccion1234"),
        new("consulta1", "Consulta 1", AppRoles.Viewer, false, "consulta1234"),
        new("maker1", "Maker 1", AppRoles.Maker, false, "maker1234")
    ];

    public static async Task EnsureSeededAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.Database.CanConnectAsync()) return;
        await db.Database.MigrateAsync();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        var technology = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == TenantContext.TechnologyTenantId);
        if (technology is null)
        {
            technology = new Tenant
            {
                Id = TenantContext.TechnologyTenantId,
                Name = "SanjCorp Technology",
                Slug = "sanjcorp-technology",
                Kind = "technology",
                Active = true
            };
            db.Tenants.Add(technology);
            await db.SaveChangesAsync();
        }
        tenantContext.Use(technology.Id);
        await DatabaseSequenceService.AlignBusinessSequencesAsync(db);

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await RenameLegacySupremeUser(users);
        foreach (var seed in BuildSeeds(configuration, environment.IsDevelopment()))
            await UpsertUser(users, technology.Id, seed);
        await AssignExistingUsersToTechnology(users, technology.Id);
    }

    private static IEnumerable<SeedUser> BuildSeeds(IConfiguration configuration, bool isDevelopment)
    {
        foreach (var template in SeedUserTemplates)
        {
            var password = configuration[$"{SeedPasswordPrefix}:{template.Username}:Password"];
            if (isDevelopment) password ??= template.DevelopmentPassword;
            if (!isDevelopment && string.IsNullOrWhiteSpace(password))
            {
                if (template.IsSupreme)
                    throw new InvalidOperationException($"Falta {SeedPasswordPrefix}__{template.Username}__Password para crear el administrador inicial en producción.");
                continue;
            }
            yield return new SeedUser(template.Username, template.DisplayName, password, template.Role, template.IsSupreme);
        }
    }

    private static async Task RenameLegacySupremeUser(UserManager<ApplicationUser> users)
    {
        var oldUser = await users.FindByNameAsync(OldSupremeUsername);
        if (oldUser is null || await users.FindByNameAsync("super_admin") is not null) return;
        oldUser.UserName = "super_admin";
        oldUser.NormalizedUserName = users.NormalizeName("super_admin");
        oldUser.Email = "super_admin@local.sanjcorp3d";
        oldUser.NormalizedEmail = users.NormalizeEmail(oldUser.Email);
        oldUser.DisplayName = "Super Admin";
        oldUser.EmailConfirmed = true;
        oldUser.Active = true;
        oldUser.TenantId = null;
        oldUser.IsSupremeAdmin = true;
        await users.UpdateAsync(oldUser);
    }

    private static async Task UpsertUser(UserManager<ApplicationUser> users, Guid technologyId, SeedUser seed)
    {
        var user = await users.FindByNameAsync(seed.Username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = seed.Username,
                Email = $"{seed.Username}@local.sanjcorp3d",
                DisplayName = seed.DisplayName,
                EmailConfirmed = true,
                Active = true,
                TenantId = seed.IsSupreme ? null : technologyId,
                IsSupremeAdmin = seed.IsSupreme
            };
            if (string.IsNullOrWhiteSpace(seed.Password))
                throw new InvalidOperationException($"Falta una contraseña inicial para {seed.Username}.");
            var created = await users.CreateAsync(user, seed.Password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(error => error.Description)));
        }
        else
        {
            user.Email = $"{seed.Username}@local.sanjcorp3d";
            user.EmailConfirmed = true;
            user.DisplayName = seed.DisplayName;
            user.Active = true;
            user.TenantId = seed.IsSupreme ? null : technologyId;
            user.IsSupremeAdmin = seed.IsSupreme;
            await users.UpdateAsync(user);
            if (!string.IsNullOrWhiteSpace(seed.Password))
            {
                if (await users.HasPasswordAsync(user)) await users.RemovePasswordAsync(user);
                var passwordResult = await users.AddPasswordAsync(user, seed.Password);
                if (!passwordResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", passwordResult.Errors.Select(error => error.Description)));
            }
        }

        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Any()) await users.RemoveFromRolesAsync(user, currentRoles);
        await users.AddToRoleAsync(user, seed.Role);
    }

    private static async Task AssignExistingUsersToTechnology(UserManager<ApplicationUser> users, Guid technologyId)
    {
        foreach (var user in users.Users.Where(x => x.TenantId == null).ToList())
        {
            var roles = await users.GetRolesAsync(user);
            if (roles.Contains(AppRoles.SuperAdmin)) continue;
            user.TenantId = technologyId;
            await users.UpdateAsync(user);
        }
    }

    private sealed record SeedUserTemplate(string Username, string DisplayName, string Role, bool IsSupreme, string DevelopmentPassword);
    private sealed record SeedUser(string Username, string DisplayName, string? Password, string Role, bool IsSupreme = false);
}
