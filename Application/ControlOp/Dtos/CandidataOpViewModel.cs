#nullable enable

namespace SAF.Application.ControlOp.Dtos;

/// <summary>Cómo se compara el importe bruto de la OP con el del devengado buscado.</summary>
public enum CoincidenciaImporte
{
    SinReferencia,
    Igual,
    /// <summary>La OP es menor: puede ser un pago parcial del expediente.</summary>
    Parcial,
    Mayor,
}

/// <summary>
/// Una OP candidata a ser la del devengado que no cruza: lo que trae la bajada más las
/// señales que acortan el análisis (el expediente que tiene no existe en devengados,
/// ya fue corregida en SAF, ya fue corregida en IVC).
/// </summary>
public class CandidataOpViewModel
{
    public string Op { get; set; } = string.Empty;
    public long? NroDocResp { get; set; }

    /// <summary>Expediente que la OP tiene hoy en la bajada, normalizado (null si no lo trae).</summary>
    public string? ExpedienteSigaf { get; set; }

    public string? Beneficiario { get; set; }
    public decimal? ImporteOp { get; set; }
    public DateTime? FechaPago { get; set; }

    public CoincidenciaImporte Coincidencia { get; set; }

    /// <summary>El expediente de la bajada no está en el ledger: señal fuerte de error de carga.</summary>
    public bool ExpedienteSinDevengado { get; set; }

    /// <summary>Expediente al que SAF ya reasignó esta OP (null si no tiene corrección).</summary>
    public string? CorregidaEnSafA { get; set; }

    /// <summary>IVC ya la corrigió por su lado (CORRECION distinto de 0 y EXP_CORREGIDO cargado).</summary>
    public bool CorregidaEnIvc { get; set; }
}
