#nullable enable
using System.ComponentModel.DataAnnotations.Schema;

namespace SAF.Data.Entities;

/// <summary>
/// Mapea la tabla [IVC].[dbo].[DEVENGADOS] (solo lectura).
/// La clave es compuesta (TipoDev, NroDev).
/// Los nombres de columna deben verificarse contra el esquema real de IVC.
/// </summary>
[Table("DEVENGADOS", Schema = "dbo")]
public class Devengado
{
    [Column("TIPO_DEV")]
    public string TipoDev { get; set; } = string.Empty;

    [Column("NRO_DEV")]
    public int NroDev { get; set; }

    [Column("FECHA_DEVENGADO")]
    public DateTime? FechaDevengado { get; set; }

    [Column("EXPEDIENTE")]
    public string? Expediente { get; set; }

    [Column("EMPRESA")]
    public string? Empresa { get; set; }

    [Column("IMPORTE")]
    public decimal? Importe { get; set; }
}
