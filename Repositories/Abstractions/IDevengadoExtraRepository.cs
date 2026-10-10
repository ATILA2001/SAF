#nullable enable
using SAF.Application.ControlOp.Dtos;
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

public interface IDevengadoExtraRepository
{
    Task<IReadOnlyList<DevengadoExtra>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Los extras de esas filas del ledger, con las mismas navegaciones que la carga
    /// completa. Alimenta la relectura de una fila tras guardar.
    /// </summary>
    Task<IReadOnlyList<DevengadoExtra>> GetByDevengadoIdsAsync(
        IReadOnlyCollection<int> devengadoIds, CancellationToken ct = default);

    /// <summary>Inserta o actualiza por DevengadoId (un registro editable por fila del ledger).</summary>
    Task UpsertAsync(DevengadoExtra entity, CancellationToken ct = default);

    /// <summary>
    /// Filas con Fecha Firma OP anterior a la fecha de corte y Status DGAyF fuera de los
    /// excluidos (sin status también entra), proyectadas a la fila de la lista de
    /// control. Es la parte del filtro de Control OP que se resuelve en SQL; la
    /// condición "sin fecha de pago" la aplica el servicio, porque sale de IVC.
    /// </summary>
    Task<IReadOnlyList<ControlOpViewModel>> GetConFirmaOpAnteriorAsync(
        DateTime fechaCorte, IReadOnlyCollection<string> statusExcluidos, CancellationToken ct = default);
}
