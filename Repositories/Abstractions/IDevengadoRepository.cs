#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// Lectura de la tabla acumulativa de devengados propia de SAF.
/// </summary>
public interface IDevengadoRepository
{
    Task<IReadOnlyList<Devengado>> GetAllAsync(CancellationToken ct = default);
    Task<Devengado?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Devengado?> GetByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default);

    /// <summary>
    /// Suma de IMPORTE_PP de todas las filas del devengado (un devengado puede tener
    /// varias filas: neto + retenciones). Es el IMPORTE del tablero Status Contabilidad.
    /// </summary>
    Task<decimal> GetSumImportePpByKeyAsync(string tipoDev, int nroDev, CancellationToken ct = default);

    /// <summary>
    /// Máxima FECHA_IMPUTACION presente en la tabla acumulativa; refleja hasta qué
    /// fecha están sincronizados los devengados. Null si la tabla está vacía.
    /// </summary>
    Task<DateTime?> GetMaxFechaImputacionAsync(CancellationToken ct = default);

    /// <summary>Alta manual de una fila del ledger (complementa la sincronización con IVC).</summary>
    Task AddAsync(Devengado entity, CancellationToken ct = default);

    /// <summary>Elimina una fila del ledger; sus datos editables (DevengadoExtra) caen en cascada.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>True si ya existe una fila idéntica (misma clave, fecha e importe).</summary>
    Task<bool> ExistsExactoAsync(string tipoDev, int nroDev, DateTime? fechaImputacion,
        decimal? importePp, CancellationToken ct = default);
}
