#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// CRUD de seguros por expediente (tabla propia de SAF) y cruce del estado de seguro.
/// </summary>
public interface ISeguroRepository
{
    Task<IReadOnlyList<ExpedienteSeguro>> GetAllAsync(CancellationToken ct = default);
    Task<ExpedienteSeguro?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(ExpedienteSeguro entity, CancellationToken ct = default);
    Task UpdateAsync(ExpedienteSeguro entity, CancellationToken ct = default);
    /// <summary>Página con orden determinístico, para la carga progresiva.</summary>
    Task<IReadOnlyList<ExpedienteSeguro>> GetPageAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>Borra exigiendo la versión que traía la grilla; conflicto si otro la modificó.</summary>
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);

    /// <summary>
    /// Devuelve el estado de seguro de cada expediente solicitado, cruzando por la clave
    /// financiera normalizada (case-insensitive). Una entrada por expediente.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetSeguroByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default);
}
