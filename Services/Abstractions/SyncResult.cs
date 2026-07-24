#nullable enable

namespace SAF.Services.Abstractions;

/// <summary>Desenlace de una sincronización de devengados IVC → SAF.</summary>
public enum SyncStatus
{
    /// <summary>IVC no tiene devengados que pasen el filtro de la vista Pagos.</summary>
    SinDatosEnIvc,

    /// <summary>SAF ya tiene la última fecha de IVC; no se importó nada (guarda diaria).</summary>
    YaActualizado,

    /// <summary>Se importaron filas nuevas.</summary>
    Importado,
}

/// <summary>
/// Resultado de <see cref="IDevengadoSyncService.SyncAsync"/>.
/// </summary>
/// <param name="Status">Desenlace de la operación.</param>
/// <param name="Insertados">Cantidad de filas nuevas insertadas.</param>
/// <param name="UltimaFecha">Máxima FECHA_IMPUTACION vigente en SAF tras la operación.</param>
public record SyncResult(SyncStatus Status, int Insertados, DateTime? UltimaFecha);
