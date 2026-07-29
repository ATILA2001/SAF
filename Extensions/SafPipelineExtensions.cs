using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SAF.Components;
using SAF.Data;
using SAF.Services.Implementations;
using System.Security.Claims;

namespace SAF;

/// <summary>
/// Pipeline HTTP, endpoints y warmup, en el mismo orden en que los invoca Program.cs.
/// El orden de los middlewares es comportamiento: no reordenar.
/// </summary>
public static class SafPipelineExtensions
{
    public static WebApplication UseSafPipeline(this WebApplication app)
    {
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
        return app;
    }

    public static WebApplication MapSafEndpoints(this WebApplication app)
    {
        // Anónimo: el fallback policy exige usuario autenticado y un monitor no tiene sesión.
        app.MapHealthChecks("/healthz").AllowAnonymous();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Root redirect: landing común para todos los usuarios autenticados
        app.MapGet("/", (HttpContext ctx) =>
        {
            ctx.Response.Redirect("/home", permanent: false);
        }).RequireAuthorization();

        // Logout
        app.MapGet("/Account/Logout", async (HttpContext ctx, IConfiguration cfg) =>
        {
            await ctx.SignOutAsync(SafBuilderExtensions.SharedScheme);
            var authWebBase = cfg["AuthWeb:BaseUrl"] ?? string.Empty;
            ctx.Response.Redirect($"{authWebBase}/Account/Login");
        }).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Warmup en background: paga al arrancar la construcción del modelo EF y la apertura
    /// de conexiones a los SQL remotos, para que el primer usuario no lo sufra en su vista.
    /// Best-effort: si falla, el primer request paga el costo normal.
    /// </summary>
    public static WebApplication StartDatabaseWarmup(this WebApplication app)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var saf = await app.Services
                    .GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
                await saf.Devengados.AsNoTracking().AnyAsync();

                await using var ivc = await app.Services
                    .GetRequiredService<IDbContextFactory<IvcDbContext>>().CreateDbContextAsync();
                await ivc.PasesSade.AsNoTracking().AnyAsync();
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Warmup de bases falló; el primer request pagará el costo inicial.");
            }
        });

        return app;
    }
}
