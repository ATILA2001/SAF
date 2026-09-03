#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Application.Pagos;
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
    // Devengados no lleva índice único (un devengado tiene varias líneas —neto y
    // retenciones— que comparten tipo, nro y fecha, y en casos aislados también el
    // importe), así que dos corridas simultáneas duplicarían la bajada del día
    // sin ningún error visible.
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

        // Por multiplicidad, no por conjunto: un devengado puede tener varias líneas y en
        // raras ocasiones dos comparten los cuatro campos (1 caso en 35k filas del
        // histórico de IVC). Con un HashSet, si SAF ya tuviera una de esas líneas, la otra
        // no entraría nunca y el importe del expediente quedaría corto en silencio.
        var faltantesPorClave = existentes
            .GroupBy(e => (e.TipoDev, e.NroDev, e.FechaImputacion, e.ImportePp))
            .ToDictionary(g => g.Key, g => g.Count());

        var nuevos = candidatos
            .Where(c =>
            {
                var clave = (c.TipoDev, c.NroDev, c.FechaImputacion, c.ImportePp);
                if (!faltantesPorClave.TryGetValue(clave, out var yaImportadas) || yaImportadas == 0)
                    return true;

                faltantesPorClave[clave] = yaImportadas - 1;   // consume una y sigue
                return false;
            })
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

    public async Task<DeteccionCorrecciones> DetectarCorreccionesExpedienteAsync(CancellationToken ct = default)
    {
        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Mismo filtro que la sync, pero TODAS las fechas: la corrección del Excel
        // puede ser sobre un devengado viejo, e IVC retiene histórico 2024+.
        var filasIvc = await ivc.Devengados.AsNoTracking()
            .Where(d => !TiposExcluidos.Contains(d.TipoDev) && d.ImportePp > 0)
            .Select(d => new CorreccionExpedienteDetector.FilaIvc(
                d.TipoDev, d.NroDev, d.FechaImputacion, d.ImportePp, d.EeFinanciera))
            .ToListAsync(ct);

        var filasSaf = await db.Devengados.AsNoTracking()
            .Select(d => new CorreccionExpedienteDetector.FilaLedger(
                d.Id, d.TipoDev, d.NroDev, d.FechaImputacion, d.ImportePp, d.Expediente))
            .ToListAsync(ct);

        var resultado = CorreccionExpedienteDetector.Detectar(filasSaf, filasIvc);

        if (resultado.Correcciones.Count > 0 || resultado.ClavesAmbiguas.Count > 0)
            logger.LogInformation(
                "Diff de expedientes SAF ↔ IVC: {Correcciones} corrección(es), {Ambiguas} clave(s) ambigua(s).",
                resultado.Correcciones.Count, resultado.ClavesAmbiguas.Count);

        return resultado;
    }

    public async Task<IReadOnlyList<CorreccionExpediente>> AplicarCorreccionesExpedienteAsync(
        IReadOnlyList<CorreccionExpediente> correcciones, CancellationToken ct = default)
    {
        if (correcciones.Count == 0) return Array.Empty<CorreccionExpediente>();

        // Mismo candado que la sync: que un import no corra mientras se corrige.
        await Candado.WaitAsync(ct);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var ids = correcciones.Select(c => c.DevengadoId).ToList();
            var filas = (await db.Devengados.Where(d => ids.Contains(d.Id)).ToListAsync(ct))
                .ToDictionary(d => d.Id);

            var aplicadas = new List<CorreccionExpediente>(correcciones.Count);
            foreach (var c in correcciones)
            {
                // Entre la detección y la confirmación la fila pudo borrarse o cambiar:
                // solo se corrige si sigue diciendo lo que el usuario vio en el diálogo.
                if (!filas.TryGetValue(c.DevengadoId, out var fila)) continue;
                if (!string.Equals(fila.Expediente, c.ExpedienteActual, StringComparison.OrdinalIgnoreCase)) continue;

                fila.Expediente = c.ExpedienteNuevo;
                aplicadas.Add(c);
            }

            if (aplicadas.Count > 0)
                await db.SaveChangesAsync(ct); // un solo SaveChanges: el lote entra todo o nada

            logger.LogInformation(
                "Corrección de expedientes: {Aplicadas} de {Total} fila(s) actualizadas desde IVC.",
                aplicadas.Count, correcciones.Count);

            return aplicadas;
        }
        finally
        {
            Candado.Release();
        }
    }
}
