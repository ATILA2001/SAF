#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Lectura de la tabla [IVC].[dbo].[SIGAF_OP] (solo lectura, sin clave EF).
/// Es la bajada del SIGAF OP (reporte de OP pagadas). Hay varias filas por OP (neto,
/// retenciones de IIBB, etc.) que comparten expediente, beneficiario, importe bruto y
/// fecha de pago, por lo que la FECHA DE PAGO NO CAF de la vista Pagos se calcula como
/// MAX(FECHA_PAGO) cruzando por expediente (EE_FINANCIERA), replicando el MAXIFS de la
/// planilla original.
///
/// La OP se identifica por NRO_DOC_RESP + OP (verificado: ningún par tiene más de un
/// expediente y ninguna OP aparece con dos NRO_DOC_RESP). NRO_DOC_RESP y ANIO_DOC_RESP
/// son el número y año del expediente del documento respaldatorio: EE_FINANCIERA se
/// deriva de ellos salvo cuando IVC la corrigió (CORRECION distinto de 0 y
/// EXP_CORREGIDO cargado).
///
/// La tabla real tiene 43 columnas; aquí solo se mapean las que consume SAF.
/// El mapeo de columnas se define en IvcDbContext.
/// </summary>
public class SigafOpPago
{
    public string? Op { get; set; }              // OP (ej. "C41-356190-2026")
    public long? NroDocResp { get; set; }        // NRO_DOC_RESP
    public string? EeFinanciera { get; set; }    // EE_FINANCIERA (Expediente, formato "NNNNNNNN/AA")
    public DateTime? FechaPago { get; set; }     // FECHA_PAGO
    public string? BenefOpDesc { get; set; }     // BENEF_OP_DESC (beneficiario; coincide con la EMPRESA del devengado)
    public decimal? ImpOp { get; set; }          // IMP_OP (bruto de la OP; es el que coincide con el importe devengado)
    public string? Correcion { get; set; }       // CORRECION (0 = sin corregir en IVC)
    public string? ExpCorregido { get; set; }    // EXP_CORREGIDO (corrección hecha en IVC)
}
