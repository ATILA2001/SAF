#nullable enable
using SAF.Data.Entities;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// Lectura de la información SADE (PASES_SADE) desde IVC, por expediente.
/// </summary>
public interface ISadeRepository
{
    /// <summary>
    /// Devuelve el último pase SADE de cada expediente solicitado
    /// (clave = expediente, comparación case-insensitive).
    /// </summary>
    Task<IReadOnlyDictionary<string, PaseSade>> GetByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default);
}
