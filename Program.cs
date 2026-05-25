using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Radzen;
using SAF.Components;
using SAF.Data;
using SAF.Repositories.Abstractions;
using SAF.Repositories.Implementations;
using SAF.Security;
using SAF.Services.Abstractions;
using SAF.Services.Implementations;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

builder.Services.AddCascadingAuthenticationState();

// ── Database ─────────────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found or empty.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()));

// IVC connection string (read-only access to IVC database)
var ivcConnectionString = builder.Configuration.GetConnectionString("IvcConnection");
if (string.IsNullOrWhiteSpace(ivcConnectionString))
    throw new InvalidOperationException("Connection string 'IvcConnection' not found or empty.");

builder.Services.AddDbContext<IvcDbContext>(options =>
    options.UseSqlServer(ivcConnectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()));

// Separate context that points to Auth.Web's database to share the DataProtection key ring.
var authWebConnectionString = builder.Configuration.GetConnectionString("AuthWebConnection");
if (string.IsNullOrWhiteSpace(authWebConnectionString))
    throw new InvalidOperationException("Connection string 'AuthWebConnection' not found or empty.");

builder.Services.AddDbContext<DataProtectionDbContext>(options =>
    options.UseSqlServer(authWebConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── Shared-cookie DataProtection ─────────────────────────────────────────────────────────────
var dataProtectionAppName = builder.Configuration["SharedCookie:ApplicationName"];
if (string.IsNullOrWhiteSpace(dataProtectionAppName))
    throw new InvalidOperationException("SharedCookie:ApplicationName not configured.");

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DataProtectionDbContext>()
    .SetApplicationName(dataProtectionAppName);

// ── Authentication ────────────────────────────────────────────────────────────────────────────
var sharedCookieName = builder.Configuration["SharedCookie:Name"];
if (string.IsNullOrWhiteSpace(sharedCookieName))
    throw new InvalidOperationException("SharedCookie:Name not configured.");
var sharedCookieDomain = builder.Configuration["SharedCookie:Domain"];

const string SharedScheme = "Identity.Application";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme          = SharedScheme;
        options.DefaultChallengeScheme = SharedScheme;
    })
    .AddCookie(SharedScheme, options =>
    {
        options.Cookie.Name        = sharedCookieName;
        options.Cookie.Domain      = string.IsNullOrWhiteSpace(sharedCookieDomain)
                                         ? null : sharedCookieDomain;
        options.Cookie.SameSite    = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly    = true;
        options.SlidingExpiration  = true;
        options.ExpireTimeSpan     = TimeSpan.FromHours(8);
        options.LoginPath          = "/Account/Login";
        options.AccessDeniedPath   = "/Account/AccessDenied";
    });

builder.Services.AddOptions<CookieAuthenticationOptions>(SharedScheme)
    .PostConfigure<IDataProtectionProvider>((options, dp) =>
    {
        var protector = dp.CreateProtector(
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
            SharedScheme,
            "v2");
        options.TicketDataFormat = new SharedCookieTicketDataFormat(protector);
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ── Application services ───────────────────────────────────────────────────────────────────
builder.Services.AddTransient<IDevengadoExtraRepository, DevengadoExtraRepository>();
builder.Services.AddTransient<IStatusContabilidadExtraRepository, StatusContabilidadExtraRepository>();
builder.Services.AddTransient<ILookupRepository, LookupRepository>();

builder.Services.AddScoped<IPagosService, PagosService>();
builder.Services.AddScoped<IStatusContabilidadService, StatusContabilidadService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Permission service — reads perms_json claim from Auth.Web cookie
builder.Services.AddScoped<IPermissionService, PermissionService>();

// perms_version check
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("AuthWeb", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["AuthWeb:BaseUrl"]
        ?? throw new InvalidOperationException("AuthWeb:BaseUrl not configured.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<IPermissionVersionService, PermissionVersionService>();

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

var app = builder.Build();

// ── HTTP pipeline ───────────────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "0";
    await next();
});

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Capture Cookie header for server-to-server Auth.Web permission version check
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var memCache = ctx.RequestServices.GetRequiredService<IMemoryCache>();
            memCache.Set(
                PermissionVersionService.CookieCacheKey(userId),
                ctx.Request.Headers["Cookie"].ToString(),
                TimeSpan.FromHours(4));
        }
    }
    await next();
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Root redirect to first allowed page
app.MapGet("/", (HttpContext ctx, IPermissionService permSvc) =>
{
    var pages = permSvc.GetAllowedPages(ctx.User);
    var target = AdminClaims.IsAdmin(ctx.User)
        ? "/pagos"
        : pages.Count > 0 ? pages[0] : "/Account/AccessDenied";
    ctx.Response.Redirect(target, permanent: false);
}).RequireAuthorization();

// Logout
app.MapGet("/Account/Logout", async (HttpContext ctx, IConfiguration cfg) =>
{
    await ctx.SignOutAsync(SharedScheme);
    var authWebBase = cfg["AuthWeb:BaseUrl"] ?? string.Empty;
    ctx.Response.Redirect($"{authWebBase}/Account/Login");
}).AllowAnonymous();

await app.RunAsync();

