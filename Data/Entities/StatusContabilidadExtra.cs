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

    // Col X: Último Movimiento SADE (manual o lookup)
    public string? UltimoMovimientoSade { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
}
