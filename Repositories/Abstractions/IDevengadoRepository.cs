#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// Lectura de la tabla acumulativa de devengados propia de SAF.
/// </summary>
public interface IDevengadoRepository
{
    Task<IReadOnlyList<Devengado>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Una sola fila del ledger, para releerla sin recargar la vista entera.</summary>
    Task<Devengado?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Las líneas de un devengado (TipoDev + NroDev), que es la fila del tablero de
    /// Status Contabilidad. Mismo orden que la carga completa: el representante del
    /// grupo tiene que salir igual que ahí.
    /// </summary>
    Task<IReadOnlyList<Devengado>> GetByClaveAsync(string tipoDev, int nroDev, CancellationToken ct = default);

    /// <summary>
    /// Máxima FECHA_IMPUTACION presente en la tabla acumulativa; refleja hasta qué
    /// fecha están sincronizados los devengados. Null si la tabla está vacía.
    /// </summary>
    Task<DateTime?> GetMaxFechaImputacionAsync(CancellationToken ct = default);

    /// <summary>Alta manual de una fila del ledger (complementa la sincronización con IVC).</summary>
    Task AddAsync(Devengado entity, CancellationToken ct = default);

    /// <summary>Página del ledger con orden determinístico, para la carga progresiva.</summary>
    Task<IReadOnlyList<Devengado>> GetPageAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>Borra la fila del ledger; la versión del extra detecta cambios de otro usuario.</summary>
    Task DeleteAsync(int id, byte[]? extraRowVersion, CancellationToken ct = default);

    /// <summary>True si ya existe una fila idéntica (misma clave, fecha e importe).</summary>
    Task<bool> ExistsExactoAsync(string tipoDev, int nroDev, DateTime? fechaImputacion,
        decimal? importePp, CancellationToken ct = default);
}
