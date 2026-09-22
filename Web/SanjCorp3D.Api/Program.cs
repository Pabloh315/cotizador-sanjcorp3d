using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Infrastructure;
using SanjCorp3D.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var connectionString = ResolveConnectionString(builder.Configuration);
var configuredKeyDirectory = builder.Configuration["DataProtection:KeysPath"];
var keyDirectory = string.IsNullOrWhiteSpace(configuredKeyDirectory)
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanjCorp3D", "DataProtectionKeys")
    : Path.GetFullPath(configuredKeyDirectory);
Directory.CreateDirectory(keyDirectory);
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("SanjCorp3D.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
if (OperatingSystem.IsWindows()) dataProtection.ProtectKeysWithDpapi();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 9;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = builder.Environment.IsDevelopment() ? "SanjCorp3D.Session" : "__Host-SanjCorp3D.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "https://cotizadoronline.vercel.app")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
builder.Services.AddAuthorization();
builder.Services.AddScoped<BusinessSettingsService>();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetSlidingWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0
        }));
});

var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();
    var requestedTenant = context.Request.Headers[TenantContext.HeaderName].FirstOrDefault();
    var initialized = tenantContext.Initialize(context.User, requestedTenant);
    // A cookie issued before the tenant claim was added can still be valid.
    // Recover only the authenticated user's own tenant from the database; a
    // requested header that differs from it remains forbidden.
    if (!initialized && context.User.Identity?.IsAuthenticated == true &&
        !context.User.IsInRole(AppRoles.SuperAdmin) &&
        string.IsNullOrWhiteSpace(context.User.FindFirst(TenantContext.ClaimType)?.Value))
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        var user = await userManager.GetUserAsync(context.User);
        if (user?.Active == true && user.TenantId is Guid tenantId &&
            (!Guid.TryParse(requestedTenant, out var requestedId) || requestedId == tenantId) &&
            await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == tenantId && x.Active))
        {
            tenantContext.Use(tenantId);
            initialized = true;
        }
    }
    if (!initialized)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "No tienes acceso a este espacio de trabajo." });
        return;
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

await IdentitySeeder.EnsureSeededAsync(app.Services, app.Configuration);
app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    // In hosted environments the provider injects DATABASE_URL. Prefer it over
    // the development connection string that remains in appsettings.json.
    var value = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("PostgreSql");
    if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Falta ConnectionStrings:PostgreSql o DATABASE_URL.");
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "postgres" && uri.Scheme != "postgresql")) return value;
    var credentials = uri.UserInfo.Split(':', 2);
    if (credentials.Length != 2) throw new InvalidOperationException("DATABASE_URL no contiene credenciales PostgreSQL válidas.");
    return new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = Npgsql.SslMode.Require
    }.ConnectionString;
}

public partial class Program;

