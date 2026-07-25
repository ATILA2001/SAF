#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SAF.Data;

/// <summary>
/// Verifica que se pueda abrir conexión con la base de un contexto.
/// Permite que un monitor (o IIS) detecte una base caída sin esperar al primer usuario.
/// </summary>
public sealed class DbContextHealthCheck<TContext>(IDbContextFactory<TContext> factory)
    : IHealthCheck where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "Sin conexión.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, ex.Message, ex);
        }
    }
}