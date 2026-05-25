#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Datos propios del SAF para la vista PAGOS.
/// Un registro por devengado único (TipoDev, NroDev).
/// Los campos IVC (TipoDev, NroDev, FechaDevengado, Expediente, Empresa, Importe)
/// se leen desde IvcDbContext y no se almacenan aquí.
/// </summary>
public class DevengadoExtra
{
    public int Id { get; set; }

    // Clave de negocio (referencia al devengado de IVC)
    public string TipoDev { get; set; } = string.Empty;
    public int NroDev { get; set; }

    // STATUS DGAyF
    public int? StatusDgayfOpcionId { get; set; }
    public StatusDgayfOpcion? StatusDgayfOpcion { get; set; }

    // STATUS OP
    public int? StatusOpOpcionId { get; set; }
    public StatusOpOpcion? StatusOpOpcion { get; set; }

    public DateTime? FechaFirmaOp { get; set; }
    public string? Observaciones { get; set; }
    public string? Ccoo { get; set; }
    public DateTime? FechaCcoo { get; set; }
    public DateTime? FechaNotificacion { get; set; }
    public string? StatusContable { get; set; }
    public string? SegurosTeso { get; set; }
    public DateTime? FechaDePagoNoCaf { get; set; }
    public DateTime? FechaDePagoCaf { get; set; }
    public DateTime? FechaPagoTotal { get; set; }
    public DateTime? FechaSade { get; set; }
    public string? BuzonSade { get; set; }
    public DateTime? PedidoFactura2 { get; set; }
    public DateTime? PedidoFactura3 { get; set; }
    public DateTime? FechaFacturaCorrecta { get; set; }
    public bool? CafSiNo { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
}
