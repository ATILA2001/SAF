#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Corrección de OP (tabla propia de SAF, editable): una OP de SIGAF reasignada a otro
/// expediente. Reemplaza el paso manual de la hoja "control OP" del Excel, en el que se
/// anotaban NRO_DOC_RESP y OP junto con el expediente correcto.
///
/// SIGAF_OP es de solo lectura para SAF (y la bajada de IVC se recarga), así que la
/// corrección no se escribe en IVC: se aplica al leer. Donde SAF cruza SIGAF por
/// expediente (Fecha Pago No CAF de Pagos), la OP corregida cuenta solo para
/// <see cref="ExpedienteCorregido"/>. La corrección de SAF prevalece aunque IVC
/// corrija después por su lado (columnas CORRECION / EXP_CORREGIDO de la bajada).
///
/// Los datos de SIGAF (expediente original, beneficiario, importe, fecha de pago) son
/// una foto tomada al corregir: la grilla y el export no dependen de que IVC responda.
/// </summary>
public class SigafOpCorreccion
{
    public int Id { get; set; }

    // Clave de la OP en SIGAF: NRO_DOC_RESP + OP (índice único).
    public long NroDocResp { get; set; }
    public string Op { get; set; } = string.Empty;

    /// <summary>EE_FINANCIERA que la OP tenía en SIGAF al corregirla.</summary>
    public string? ExpedienteOriginal { get; set; }

    /// <summary>Clave financiera normalizada NNNNNNNN/AA (ver ExpedienteKey.Normalizar).</summary>
    public string ExpedienteCorregido { get; set; } = string.Empty;

    public string? Beneficiario { get; set; }
    public decimal? ImporteOp { get; set; }
    public DateTime? FechaPago { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    public byte[]? RowVersion { get; set; }
}
