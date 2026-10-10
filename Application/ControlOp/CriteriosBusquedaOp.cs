#nullable enable

namespace SAF.Application.ControlOp;

/// <summary>
/// Cómo se buscaba la OP en la consulta de SIGAF del Excel: por beneficiario (la
/// empresa del devengado), por fecha de pago a partir de la firma de la OP, y mirando
/// el importe. El importe no filtra: ordena y marca la coincidencia, porque un
/// expediente puede pagarse en varias OPs parciales y hay que verlas todas.
/// </summary>
public sealed record CriteriosBusquedaOp(
    string? Beneficiario,
    DateTime? FechaPagoDesde,
    DateTime? FechaPagoHasta,
    decimal? ImporteReferencia)
{
    /// <summary>Tope de candidatas por búsqueda: sin él, una búsqueda vacía traería las 20 mil OPs.</summary>
    public const int MaxResultados = 200;

    /// <summary>Hace falta al menos un criterio que acote: beneficiario o rango de fechas.</summary>
    public bool Acota => !string.IsNullOrWhiteSpace(Beneficiario) || FechaPagoDesde is not null || FechaPagoHasta is not null;
}
