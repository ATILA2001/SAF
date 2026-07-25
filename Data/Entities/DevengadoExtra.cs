#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Datos propios del SAF para la vista PAGOS.
/// Un registro por FILA del ledger de devengados (DevengadoId): un devengado puede
/// tener varias filas (neto + retenciones) y cada una se edita por separado.
/// </summary>
public class DevengadoExtra
{
    public int Id { get; set; }

    // Clave: fila del ledger de SAF (cada fila tiene sus datos editables propios)
    public int DevengadoId { get; set; }
    public Devengado? Devengado { get; set; }

    // Denormalizados de la fila (facilitan consultas y el cruce del tablero contable)
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
    public bool? CafSiNo { get; set; }

    // Derivadas (no se persisten, se calculan en PagosService):
    //   StatusContable ← tablero StatusContabilidad + regla "OP Lista"
    //   SegurosTeso ← tabla ExpedienteSeguro (columna Seguro)
    //   PedidoFactura2/3, FechaFacturaCorrecta ← tablero StatusContabilidad
    //   FechaDePagoNoCaf ← IVC.SIGAF_OP (MAX FECHA_PAGO)
    //   FechaDePagoCaf ← tabla ExpedienteCaf (cuando StatusDGAyF = "avanzar CAF")
    //   FechaPagoTotal ← No CAF + CAF + status
    //   FechaSade / BuzonSade ← IVC.PASES_SADE (por expediente)

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    public byte[]? RowVersion { get; set; }
}
