#nullable enable

namespace SAF.Repositories.Abstractions;

/// <summary>
/// Lectura de las fechas de pago del SIGAF OP (IVC) por expediente.
/// </summary>
public interface ISigafOpRepository
{
    /// <summary>
    /// Devuelve la última fecha de pago (MAX FECHA_PAGO) de cada expediente
    /// solicitado. Clave = expediente (EE_FINANCIERA), comparación case-insensitive.
    /// Solo incluye expedientes que tienen al menos una fecha de pago cargada.
    /// </summary>
    Task<IReadOnlyDictionary<string, DateTime>> GetFechaPagoByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default);
}
