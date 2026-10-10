#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.ControlOp.Dtos;

/// <summary>
/// Una corrección de OP vigente: la OP de SIGAF (NRO_DOC_RESP + OP) reasignada a otro
/// expediente. Los datos de SIGAF (expediente original, beneficiario, importe, fecha)
/// son una foto tomada al corregir, para que la grilla y el export no dependan de IVC.
/// [Display(Name)] = título de la columna en el export.
/// </summary>
public class CorreccionOpViewModel
{
    [ExportIgnore, AuditIgnore]
    public int Id { get; set; }

    [Display(Name = "NRO DOC RESP")]
    public long NroDocResp { get; set; }

    [Display(Name = "OP")]
    [MaxLength(80)]
    public string Op { get; set; } = string.Empty;

    [Display(Name = "EXPEDIENTE EN BAJADA OP")]
    public string? ExpedienteOriginal { get; set; }

    [Display(Name = "EXPEDIENTE CORREGIDO")]
    [MaxLength(100)]
    public string ExpedienteCorregido { get; set; } = string.Empty;

    [Display(Name = "BENEFICIARIO")]
    public string? Beneficiario { get; set; }

    [Display(Name = "IMPORTE OP")]
    public decimal? ImporteOp { get; set; }

    [Display(Name = "FECHA PAGO")]
    public DateTime? FechaPago { get; set; }

    [Display(Name = "ÚLTIMA MODIFICACIÓN")]
    [AuditIgnore]
    public DateTime? FechaModificacion { get; set; }

    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
