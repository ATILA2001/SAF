#nullable enable
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;

namespace SAF.Services.Abstractions;

/// <summary>
/// Circuito de la hoja "control OP": lista de devengados con OP firmada que no cruzan
/// con ninguna OP pagada, búsqueda de la OP en SIGAF y corrección de su expediente.
/// </summary>
public interface IControlOpService
{
    /// <summary>
    /// Devengados con Fecha Firma OP anterior a la fecha de corte, status no excluido y
    /// sin Fecha Pago Total (calculada como en Pagos, correcciones de OP incluidas).
    /// </summary>
    Task<IReadOnlyList<ControlOpViewModel>> GetListaControlAsync(DateTime fechaCorte, CancellationToken ct = default);

    /// <summary>OPs de SIGAF que cumplen los criterios, con las señales de Control OP.</summary>
    Task<IReadOnlyList<CandidataOpViewModel>> BuscarCandidatasAsync(CriteriosBusquedaOp criterios, CancellationToken ct = default);

    Task<IReadOnlyList<CorreccionOpViewModel>> GetCorreccionesAsync(CancellationToken ct = default);

    /// <summary>Relee una corrección para refrescarla en el lugar. Null: otro usuario la borró.</summary>
    Task<CorreccionOpViewModel?> GetCorreccionByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Crea la corrección: valida, verifica que la OP exista en SIGAF con ese
    /// NRO_DOC_RESP, que no tenga ya una corrección y que el expediente cambie de
    /// verdad; toma de SIGAF la foto (expediente original, beneficiario, importe, fecha).
    /// </summary>
    Task<CorreccionOpViewModel> CreateAsync(CorreccionOpViewModel vm, CancellationToken ct = default);

    /// <summary>Cambia el expediente corregido; la OP (clave) no se edita.</summary>
    Task UpdateAsync(CorreccionOpViewModel vm, CancellationToken ct = default);

    /// <summary>Elimina la corrección: la OP vuelve al expediente que tiene en SIGAF.</summary>
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
