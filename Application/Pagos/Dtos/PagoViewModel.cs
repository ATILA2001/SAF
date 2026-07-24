#nullable enable
using System.ComponentModel.DataAnnotations;

namespace SAF.ViewModels.Pagos;

/// <summary>
/// ViewModel para la vista PAGOS.
/// Combina datos de IVC (solo lectura) con datos propios del SAF (editables).
/// </summary>
public class PagoViewModel
{
    // ─── Datos de IVC (solo lectura) ─────────────────────────────────────────
    public string TipoDev { get; set; } = string.Empty;
    public int NroDev { get; set; }
    public DateTime? FechaDevengado { get; set; }
    public string? Expediente { get; set; }
    public string? Empresa { get; set; }
    public decimal? Importe { get; set; }

    // ─── Datos propios SAF (editables) ───────────────────────────────────────
    public int? StatusDgayfOpcionId { get; set; }
    public string? StatusDgayfNombre { get; set; }

    public int? StatusOpOpcionId { get; set; }
    public string? StatusOpNombre { get; set; }

    public DateTime? FechaFirmaOp { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(200)]
    public string? Ccoo { get; set; }

    public DateTime? FechaCcoo { get; set; }
    public DateTime? FechaNotificacion { get; set; }

    [MaxLength(100)]
    public string? StatusContable { get; set; }

    [MaxLength(200)]
    public string? SegurosTeso { get; set; }

    public DateTime? FechaDePagoNoCaf { get; set; }
    public DateTime? FechaDePagoCaf { get; set; }
    public DateTime? FechaPagoTotal { get; set; }
    public DateTime? FechaSade { get; set; }

    [MaxLength(200)]
    public string? BuzonSade { get; set; }

    public DateTime? PedidoFactura2 { get; set; }
    public DateTime? PedidoFactura3 { get; set; }
    public DateTime? FechaFacturaCorrecta { get; set; }
    public bool? CafSiNo { get; set; }
}
