#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

/// <summary>
/// Append idempotente de devengados IVC → tabla acumulativa de SAF.
/// Aplica el filtro de la vista Pagos: TIPO_DEV NOT IN ('C55','CPS') AND IMPORTE_PP > 0.
/// Nunca borra (conserva histórico). Replica la lógica del "segundo Excel".
/// </summary>
public class DevengadoSyncService(
    IDbContextFactory<IvcDbContext> ivcFactory,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<DevengadoSyncService> logger) : IDevengadoSyncService
{
    // Mismo filtro que la vista Pagos y el alta manual (fuente única).
    private static readonly string[] TiposExcluidos = ReglasDevengado.TiposExcluidos;

    // Una sincronización a la vez. El diff es leer-existentes → insertar-faltantes y
    // Devengados no puede llevar índice único (hay filas legítimamente repetidas: el
    // neto y sus retenciones comparten tipo, nro, fecha e importe), así que dos
    // corridas simultáneas duplicarían la bajada del día sin ningún error visible.
    // Alcanza con una instancia de la app; con varias haría falta sp_getapplock.
    private static readonly SemaphoreSlim Candado = new(1, 1);

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        await Candado.WaitAsync(ct);
        try
        {
            var resultado = await SincronizarAsync(ct);

            // Único rastro server-side de la sincronización: sin esto, "sincronicé y no
            // trajo nada" no se puede diagnosticar después.
            if (resultado.Status == SyncStatus.SinDatosEnIvc)
                logger.LogWarning("Sync devengados: la bajada de IVC no tiene filas para importar.");
            else
                logger.LogInformation(
                    "Sync devengados: {Status}, {Insertados} fila(s), fecha {Fecha:dd/MM/yyyy}.",
                    resultado.Status, resultado.Insertados, resultado.UltimaFecha);

            return resultado;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync devengados: falló la sincronización.");
            throw;
        }
        finally
        {
            Candado.Release();
        }
    }

    private async Task<SyncResult> SincronizarAsync(CancellationToken ct)
    {
        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Filtro de la vista Pagos.
        var query = ivc.Devengados.AsNoTracking()
            .Where(d => !TiposExcluidos.Contains(d.TipoDev) && d.ImportePp > 0);

        // Solo la última fecha de imputación presente en IVC ("último día según reporte").
        // La tabla IVC tiene histórico (2024+), pero solo se importa la fecha más reciente.
        var ultimaFecha = await query
            .Select(d => d.FechaImputacion)
            .MaxAsync(ct);

        if (ultimaFecha is null)
            return new SyncResult(SyncStatus.SinDatosEnIvc, 0, null);

        // Sin guarda por fecha máxima: el diff de filas corre siempre. Comparar solo
        // max(SAF) == max(IVC) descartaba el día entero cuando un alta manual fechada
        // hoy igualaba las fechas, o cuando una corrida previa importó un parcial
        // durante la recarga diaria de IVC. El diff ya garantiza la idempotencia.
        var candidatos = await query
            .Where(d => d.FechaImputacion == ultimaFecha)
            .ToListAsync(ct);
        if (candidatos.Count == 0)
            return new SyncResult(SyncStatus.YaActualizado, 0, ultimaFecha);

        // Idempotencia: no reinsertar filas de esa fecha ya presentes (misma línea exacta).
        var existentes = await db.Devengados.AsNoTracking()
            .Where(d => d.FechaImputacion == ultimaFecha)
            .Select(d => new { d.TipoDev, d.NroDev, d.FechaImputacion, d.ImportePp })
            .ToListAsync(ct);

        var existentesSet = existentes
            .Select(e => (e.TipoDev, e.NroDev, e.FechaImputacion, e.ImportePp))
            .ToHashSet();

        var nuevos = candidatos
            .Where(c => !existentesSet.Contains((c.TipoDev, c.NroDev, c.FechaImputacion, c.ImportePp)))
            .Select(c => new Devengado
            {
                TipoDev = c.TipoDev,
                NroDev = c.NroDev,
                FechaImputacion = c.FechaImputacion,
                // Misma normalización que el alta manual (PagosService): las dos vías de
                // ingesta deben producir la misma clave o los cruces CAF/Seguros fallan
                // en silencio. Si IVC trae un formato no reconocido, se conserva el crudo
                // (con Trim) para no perder la fila.
                Expediente = ExpedienteKey.Normalizar(c.EeFinanciera) ?? c.EeFinanciera?.Trim(),
                Empresa = c.Descripcion,
                ImportePp = c.ImportePp,
                FechaImportacion = DateTime.UtcNow,
            })
            .ToList();

        if (nuevos.Count == 0)
            return new SyncResult(SyncStatus.YaActualizado, 0, ultimaFecha);

        db.Devengados.AddRange(nuevos);
        await db.SaveChangesAsync(ct);
        return new SyncResult(SyncStatus.Importado, nuevos.Count, ultimaFecha);
    }
}
