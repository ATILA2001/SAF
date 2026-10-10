#nullable enable

namespace SAF.Application.ControlOp.Dtos;

/// <summary>
/// Una OP de la bajada de OP (SIGAF_OP) agrupada: la bajada tiene varias líneas por OP
/// (neto, retenciones), que comparten expediente, beneficiario, importe bruto y fecha
/// de pago. Miembros con init y no constructor posicional: EF compone OrderBy/Take
/// sobre una proyección por miembros, no sobre los argumentos de un constructor.
/// </summary>
public sealed record OpSigafResumen
{
    public required string Op { get; init; }
    public long? NroDocResp { get; init; }
    public string? EeFinanciera { get; init; }
    public string? Beneficiario { get; init; }
    public decimal? ImporteOp { get; init; }
    public DateTime? FechaPago { get; init; }
    public string? ExpCorregidoIvc { get; init; }
    public string? CorrecionIvc { get; init; }
}
