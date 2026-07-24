#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Lectura de la tabla [IVC].[dbo].[SIGAF_OP] (solo lectura, sin clave EF).
/// Es la bajada del SIGAF OP (hoja 2026 del reporte de OP pagadas). Hay varias
/// filas por expediente (neto, retenciones de IIBB, etc.), por lo que la FECHA DE
/// PAGO NO CAF de la vista Pagos se calcula como MAX(FECHA_PAGO) cruzando por
/// expediente (EE_FINANCIERA), replicando el MAXIFS de la planilla original.
///
/// La tabla real tiene 43 columnas; aquí solo se mapean las que consume SAF.
/// El mapeo de columnas se define en IvcDbContext.
/// </summary>
public class SigafOpPago
{
    public string? EeFinanciera { get; set; }   // EE_FINANCIERA (Expediente, formato "NNNNNNNN/AA")
    public DateTime? FechaPago { get; set; }     // FECHA_PAGO
}
