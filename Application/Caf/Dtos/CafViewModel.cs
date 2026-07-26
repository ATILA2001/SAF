#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.Caf.Dtos;

/// <summary>
/// ViewModel para la vista CAF (carga editable de expedientes CAF).
/// [Display(Name)] = título de la columna en la planilla original (se usa al exportar).
/// </summary>
public class CafViewModel
{
    [ExportIgnore, AuditIgnore]
    public int Id { get; set; }

    [Display(Name = "Año")]
    public int Anio { get; set; }

    // Columna única: clave financiera NNNNNNNN/AA. Se normaliza y valida al guardar
    // (ver ExpedienteKey.Normalizar; formatos aceptados en ExpedienteKey.FormatosAceptados).
    [Display(Name = "EXPEDIENTE")]
    [Required(ErrorMessage = "El expediente es obligatorio.")]
    [MaxLength(100)]
    public string Expediente { get; set; } = string.Empty;

    [Display(Name = "OP")]
    [MaxLength(50)]
    public string? Op { get; set; }

    [Display(Name = "BENEFICIARIO")]
    [MaxLength(255)]
    public string? Beneficiario { get; set; }

    [Display(Name = "IMPORTE NETO")]
    public decimal? ImporteNeto { get; set; }

    [Display(Name = "IIBB (9010/8)")]
    public decimal? Iibb { get; set; }

    [Display(Name = "CUENTA")]
    [MaxLength(50)]
    public string? Cuenta { get; set; }

    [Display(Name = "FECHA PAGO")]
    public DateTime? FechaPago { get; set; }

    [Display(Name = "CARGADO")]
    public DateTime? Cargado { get; set; }

    [Display(Name = "CC PAGADORA")]
    [MaxLength(100)]
    public string? CcPagadora { get; set; }

    [Display(Name = "PASE")]
    [MaxLength(100)]
    public string? Pase { get; set; }

    [Display(Name = "REVISADO")]
    public DateTime? Revisado { get; set; }

    // Versión de la fila al momento de cargarla: viaja a la grilla y vuelve al guardar,
    // para detectar que otro usuario la modificó mientras se editaba.
    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
