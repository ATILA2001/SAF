#nullable enable
using SAF.Application.ControlOp;
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>CRUD de correcciones de OP (tabla propia de SAF).</summary>
public interface ISigafOpCorreccionRepository
{
    Task<IReadOnlyList<SigafOpCorreccion>> GetAllAsync(CancellationToken ct = default);
    Task<SigafOpCorreccion?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>La corrección vigente de una OP (NRO_DOC_RESP + OP), o null.</summary>
    Task<SigafOpCorreccion?> GetByClaveAsync(long nroDocResp, string op, CancellationToken ct = default);

    /// <summary>
    /// Solo las claves, para aplicar las correcciones al cruzar SIGAF por expediente.
    /// La tabla es chica (una fila por OP corregida): se trae entera.
    /// </summary>
    Task<IReadOnlyList<CorreccionOpClave>> GetClavesAsync(CancellationToken ct = default);

    Task AddAsync(SigafOpCorreccion entity, CancellationToken ct = default);
    Task UpdateAsync(SigafOpCorreccion entity, CancellationToken ct = default);

    /// <summary>Borra exigiendo la versión que traía la grilla; conflicto si otro la modificó.</summary>
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
