#nullable enable
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;

namespace SAF.Repositories.Abstractions;

/// <summary>
/// Lectura de la bajada de OP (IVC.SIGAF_OP): fechas de pago por expediente para Pagos
/// y búsqueda de OPs para Control OP.
/// </summary>
public interface ISigafOpRepository
{
    /// <summary>
    /// Devuelve la última fecha de pago (MAX FECHA_PAGO) de cada expediente solicitado,
    /// con las correcciones de OP de SAF ya aplicadas: una OP corregida cuenta solo
    /// para su expediente corregido. Clave = expediente, comparación case-insensitive.
    /// Solo incluye expedientes con al menos una fecha de pago.
    /// </summary>
    Task<IReadOnlyDictionary<string, DateTime>> GetFechaPagoByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default);

    /// <summary>
    /// OPs que cumplen los criterios (beneficiario por contenido, rango de fecha de
    /// pago), agrupadas por OP. Con importe de referencia, las más cercanas primero;
    /// si no, las más recientes. Tope <see cref="CriteriosBusquedaOp.MaxResultados"/>.
    /// </summary>
    Task<IReadOnlyList<OpSigafResumen>> BuscarOpsAsync(CriteriosBusquedaOp criterios, CancellationToken ct = default);

    /// <summary>Una OP por su clave en la bajada (NRO_DOC_RESP + OP). Null si no está.</summary>
    Task<OpSigafResumen?> GetOpAsync(long nroDocResp, string op, CancellationToken ct = default);
}
