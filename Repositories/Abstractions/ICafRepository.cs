#nullable enable
using SAF.Application.Caf.Dtos;
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// CRUD de expedientes CAF (tabla propia de SAF) y cruce de fecha de pago por expediente.
/// </summary>
public interface ICafRepository
{
    Task<IReadOnlyList<ExpedienteCaf>> GetAllAsync(CancellationToken ct = default);
    Task<ExpedienteCaf?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(ExpedienteCaf entity, CancellationToken ct = default);
    Task UpdateAsync(ExpedienteCaf entity, CancellationToken ct = default);
    /// <summary>Borra exigiendo la versión que traía la grilla; conflicto si otro la modificó.</summary>
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);

    /// <summary>
    /// Devuelve la fecha de pago CAF (MAX FECHA_PAGO) de cada expediente solicitado,
    /// cruzando por la clave financiera normalizada (case-insensitive).
    /// </summary>
    Task<IReadOnlyDictionary<string, ResumenCafViewModel>> GetResumenCafByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default);
}
