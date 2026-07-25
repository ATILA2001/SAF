#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.Seguros.Dtos;

/// <summary>
/// ViewModel para la vista Seguros (carga editable del seguro por expediente).
/// [Display(Name)] = título de la columna en la planilla original (se usa al exportar).
/// </summary>
public class SeguroViewModel
{
    [ExportIgnore]
    public int Id { get; set; }

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

    [Display(Name = "ESTADO")]
    [MaxLength(100)]
    public string? Estado { get; set; }

    // Seguro normalizado a lista (ok / CAF/ok / Pendiente); regla "peor caso gana" en Pagos.
    [ExportIgnore]
    public int? SeguroOpcionId { get; set; }

    [Display(Name = "SEGURO")]
    public string? SeguroNombre { get; set; }

    // Versión de la fila al momento de cargarla: viaja a la grilla y vuelve al guardar,
    // para detectar que otro usuario la modificó mientras se editaba.
    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
