using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Radzen;
using SAF.Data;
using SAF.Repositories.Abstractions;
using SAF.Repositories.Implementations;
using SAF.Security;
using SAF.Services.Abstractions;
using SAF.Services.Implementations;

namespace SAF;

/// <summary>
/// Registro de servicios, un método por concern y en el mismo orden en que los
/// invoca Program.cs. Solo organiza: no cambia qué se registra ni cómo.
/// </summary>
public static class SafBuilderExtensions
{
    /// <summary>Esquema de la cookie compartida con Auth.Web (el que emite Identity).</summary>
    public const string SharedScheme = "Identity.Application";

    public static WebApplicationBuilder AddPresentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddRadzenComponents();
        // Persiste el tema (claro/oscuro) en cookie. IsSecure=false para que la cookie
        // también viaje por http (dev) y el servidor la lea en cada navegación; sin esto,
        // sobre http la cookie Secure no se envía y el tema se "reinicia" al navegar.
        builder.Services.AddRadzenCookieThemeService(options =>
        {
            options.Name = "RadzenTheme";
            options.Duration = TimeSpan.FromDays(365);
            options.IsSecure = false;
        });

        builder.Services.AddCascadingAuthenticationState();
        return builder;
    }

    public static WebApplicationBuilder AddDatabases(this WebApplicationBuilder builder)
    {
        // Factory, no contexto scoped: en Blazor Server el scope dura todo el circuito del
        // usuario, así que un contexto inyectado se comparte entre operaciones solapadas y
        // EF Core lanza "A second operation was started on this context instance".
        // Cada repositorio crea y descarta el suyo por operación.
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.RequireConnectionString("DefaultConnection"), sqlOptions =>
                sqlOptions.EnableRetryOnFailure()));

        // IVC connection string (read-only access to IVC database)
        builder.Services.AddDbContextFactory<IvcDbContext>(options =>
            options.UseSqlServer(builder.Configuration.RequireConnectionString("IvcConnection"), sqlOptions =>
                sqlOptions.EnableRetryOnFailure()));

        // Separate context that points to Auth.Web's database to share the DataProtection key ring.
        // Con reintentos como los otros contextos: si un corte transitorio impide leer el key
        // ring, ninguna cookie se puede desproteger y todos los usuarios rebotan al login.
        builder.Services.AddDbContext<DataProtectionDbContext>(options =>
            options.UseSqlServer(builder.Configuration.RequireConnectionString("AuthWebConnection"), sqlOptions =>
                sqlOptions.EnableRetryOnFailure()));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Estado de las bases externas. IVC se reporta como "degraded" y no como caída: sin
        // ella las vistas propias siguen funcionando, solo se pierden las columnas derivadas.
        // Timeout propio: sin él, la estrategia de reintentos de EF hace que el monitor reciba
        // un cuelgue en vez de un estado cuando la base no responde.
        builder.Services.AddHealthChecks()
            .AddCheck<DbContextHealthCheck<AppDbContext>>(
                "saf", failureStatus: null, tags: null, timeout: TimeSpan.FromSeconds(5))
            .AddCheck<DbContextHealthCheck<IvcDbContext>>(
                "ivc", HealthStatus.Degraded, tags: null, timeout: TimeSpan.FromSeconds(5));

        return builder;
    }

    public static WebApplicationBuilder AddSharedCookieAuth(this WebApplicationBuilder builder)
    {
        var dataProtectionAppName = builder.Configuration["SharedCookie:ApplicationName"];
        if (string.IsNullOrWhiteSpace(dataProtectionAppName))
            throw new InvalidOperationException("SharedCookie:ApplicationName not configured.");

        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionDbContext>()
            .SetApplicationName(dataProtectionAppName);

        var sharedCookieName = builder.Configuration["SharedCookie:Name"];
        if (string.IsNullOrWhiteSpace(sharedCookieName))
            throw new InvalidOperationException("SharedCookie:Name not configured.");
        var sharedCookieDomain = builder.Configuration["SharedCookie:Domain"];

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

        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IDevengadoRepository, DevengadoRepository>();
        builder.Services.AddTransient<ISadeRepository, SadeRepository>();
        builder.Services.AddTransient<ISigafOpRepository, SigafOpRepository>();
        builder.Services.AddTransient<IDevengadoExtraRepository, DevengadoExtraRepository>();
        builder.Services.AddTransient<IStatusContabilidadExtraRepository, StatusContabilidadExtraRepository>();
        builder.Services.AddTransient<ICafRepository, CafRepository>();
        builder.Services.AddTransient<ISeguroRepository, SeguroRepository>();
        builder.Services.AddTransient<ILookupRepository, LookupRepository>();
        builder.Services.AddTransient<IListaAdminRepository, ListaAdminRepository>();

        builder.Services.AddScoped<IPagosService, PagosService>();
        builder.Services.AddScoped<IStatusContabilidadService, StatusContabilidadService>();
        builder.Services.AddScoped<ICafService, CafService>();
        builder.Services.AddScoped<ISeguroService, SeguroService>();
        builder.Services.AddScoped<ILookupService, LookupService>();
        builder.Services.AddScoped<IListaAdminService, ListaAdminService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<IDevengadoSyncService, DevengadoSyncService>();
        builder.Services.AddScoped<SAF.Shared.INotificationHelper, SAF.Shared.NotificationHelper>();
        builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

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

        return builder;
    }

    public static WebApplicationBuilder AddHttpsHardening(this WebApplicationBuilder builder)
    {
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

        return builder;
    }

    private static string RequireConnectionString(this IConfiguration cfg, string name)
    {
        var cs = cfg.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException($"Connection string '{name}' not found or empty.");
        return cs;
    }
}
