#nullable enable

namespace SAF.Services.Abstractions;

/// <summary>
/// Sincroniza (append) los devengados de IVC hacia la tabla acumulativa de SAF.
/// Disparo manual desde la UI.
/// </summary>
public interface IDevengadoSyncService
{
    /// <summary>
    /// Importa desde IVC los devengados que pasan el filtro de la vista Pagos
    /// y que aún no estén en la tabla acumulativa de SAF. Si SAF ya tiene la
    /// última fecha de IVC, no importa nada (guarda de "una vez por día").
    /// </summary>
    /// <returns>Desenlace de la operación con la cantidad de filas insertadas.</returns>
    Task<SyncResult> SyncAsync(CancellationToken ct = default);
}
