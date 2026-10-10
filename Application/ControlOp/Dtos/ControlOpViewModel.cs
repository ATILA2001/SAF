#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.ControlOp.Dtos;

/// <summary>
/// Una fila de la lista de control: devengado con la OP firmada que todavía no cruza
/// con ninguna OP pagada. Mismas columnas que la hoja "control OP" del Excel
/// (CHOOSECOLS 1;2;3;9;4 de PAGOS!D:R) más la identidad del devengado.
/// </summary>
public class ControlOpViewModel
{
    [ExportIgnore]
    public int DevengadoId { get; set; }

    [Display(Name = "TIPO DEV")]
    public string TipoDev { get; set; } = string.Empty;

    [Display(Name = "NRO DEV")]
    public int NroDev { get; set; }

    [Display(Name = "EXPEDIENTE")]
    public string? Expediente { get; set; }

    [Display(Name = "EMPRESA")]
    public string? Empresa { get; set; }

    [Display(Name = "IMPORTE")]
    public decimal? Importe { get; set; }

    [Display(Name = "FECHA FIRMA OP")]
    public DateTime? FechaFirmaOp { get; set; }

    [Display(Name = "STATUS DGAyF")]
    public string? StatusDgayf { get; set; }
}
