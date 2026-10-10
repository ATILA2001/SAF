#nullable enable

namespace SAF.Application.ControlOp;

/// <summary>
/// Clave de una corrección de OP vigente: la OP (NRO_DOC_RESP + OP, que en la bajada
/// identifican una OP sin ambigüedad) y el expediente al que SAF la reasigna.
/// </summary>
public sealed record CorreccionOpClave(long NroDocResp, string Op, string ExpedienteCorregido);
