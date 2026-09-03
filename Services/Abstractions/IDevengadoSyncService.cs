#nullable enable
using SAF.Application.Pagos;

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

    /// <summary>
    /// Compara TODO el ledger contra IVC (no solo la última fecha) y devuelve las
    /// filas cuyo expediente fue corregido en la fuente: el append de la sync nunca
    /// relee filas ya importadas, así que sin este diff la corrección del Excel
    /// diario no llega jamás a SAF.
    /// </summary>
    Task<DeteccionCorrecciones> DetectarCorreccionesExpedienteAsync(CancellationToken ct = default);

    /// <summary>
    /// Aplica sobre el ledger las correcciones que el usuario confirmó. Cada fila se
    /// corrige solo si su expediente sigue siendo el que se le mostró (guarda contra
    /// ediciones concurrentes); el guardado es un solo SaveChanges (todo o nada).
    /// </summary>
    /// <returns>Las correcciones efectivamente aplicadas (para auditar exactamente eso).</returns>
    Task<IReadOnlyList<CorreccionExpediente>> AplicarCorreccionesExpedienteAsync(
        IReadOnlyList<CorreccionExpediente> correcciones, CancellationToken ct = default);
}
