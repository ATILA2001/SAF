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
    IDbContextFactory<AppDbContext> dbFactory) : IDevengadoSyncService
{
    // Mismo filtro que la vista Pagos y el alta manual (fuente única).
    private static readonly string[] TiposExcluidos = ReglasDevengado.TiposExcluidos;

    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
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
