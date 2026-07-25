#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Datos propios del SAF para la vista STATUS CONTABILIDAD.
/// Un registro por devengado único (TipoDev, NroDev).
/// Columnas I–Q y X del tablero contable.
/// </summary>
public class StatusContabilidadExtra
{
    public int Id { get; set; }

    // Clave de negocio
    public string TipoDev { get; set; } = string.Empty;
    public int NroDev { get; set; }

    // Col I: Fecha pedido de factura 2
    public DateTime? FechaPedidoFactura2 { get; set; }

    // Col J: Reiterar pedido de factura 3
    public DateTime? ReiterarPedidoFactura3 { get; set; }

    // Col K: Fecha de ingreso factura (correcta)
    public DateTime? FechaIngresoFactura { get; set; }

    // Col K (variante texto): en el Excel la misma columna mezcla fechas con marcas
    // "N/C" / "CCOO" / "PAV" / "Anulado". Acá se separan: fecha tipada + motivo aparte.
    public string? SinFacturaMotivo { get; set; }

    // Col L: Status Contable
    public int? StatusContableOpcionId { get; set; }
    public StatusContableOpcion? StatusContableOpcion { get; set; }

    // Col M: Observaciones Ctas. a Pagar
    public string? ObservacionesCuentasPagar { get; set; }

    // Col N: Usuario Tramitador Cuentas a Pagar
    public int? TramitadorCuentasPagarOpcionId { get; set; }
    public TramitadorCuentasPagarOpcion? TramitadorCuentasPagarOpcion { get; set; }

    // Col O: Usuario tramitador Liquidaciones
    public int? TramitadorLiquidacionesOpcionId { get; set; }
    public TramitadorLiquidacionesOpcion? TramitadorLiquidacionesOpcion { get; set; }

    // Col P: Observaciones Liquidaciones
    public string? ObservacionesLiquidaciones { get; set; }

    // Col Q: Falta Póliza
    public bool FaltaPoliza { get; set; } = false;

    // Col W (Buzón SADE), X (Último Movimiento) e Y (Días en el área) son derivadas
    // de IVC.PASES_SADE por expediente (VLOOKUP a la hoja SADE en el Excel):
    // se calculan en StatusContabilidadService y no se persisten aquí.

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    public byte[]? RowVersion { get; set; }
}
