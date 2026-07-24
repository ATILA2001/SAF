#nullable enable
using System.ComponentModel.DataAnnotations;

namespace SAF.ViewModels.StatusContabilidad;

/// <summary>
/// ViewModel para la vista STATUS CONTABILIDAD (tablero contable).
/// Columnas derivadas de IVC + PAGOS + datos propios del SAF.
/// </summary>
public class StatusContabilidadViewModel
{
    // ─── De IVC / Pagos (solo lectura) ───────────────────────────────────────
    public string TipoDev { get; set; } = string.Empty;
    public int NroDev { get; set; }
    public string? Expediente { get; set; }
    public string? Empresa { get; set; }
    public decimal? ImporteTotal { get; set; }      // suma por grupo TipoDev+NroDev
    public string? StatusDgayfNombre { get; set; }   // col F
    public string? FirmadaPorMiguel { get; set; }    // col G = STATUS OP de PAGOS
    public DateTime? FechaPedidoFactura1 { get; set; } // col H = FECHA DEVENGADO

    // EE SADE derivado de Expediente
    public string? EeSade { get; set; }              // col R (calculado)

    // De PAGOS
    public string? Observaciones { get; set; }       // col S
    public string? Ccoo { get; set; }                // col T
    public DateTime? FechaCcoo { get; set; }         // col U
    public DateTime? FechaNotificacion { get; set; } // col V
    public string? BuzonSade { get; set; }           // col W

    // ─── Datos propios SAF (editables) ───────────────────────────────────────
    public DateTime? FechaPedidoFactura2 { get; set; }       // col I
    public DateTime? ReiterarPedidoFactura3 { get; set; }    // col J
    public DateTime? FechaIngresoFactura { get; set; }       // col K

    public int? StatusContableOpcionId { get; set; }          // col L
    public string? StatusContableNombre { get; set; }

    [MaxLength(500)]
    public string? ObservacionesCuentasPagar { get; set; }   // col M

    public int? TramitadorCuentasPagarOpcionId { get; set; }  // col N
    public string? TramitadorCuentasPagarNombre { get; set; }

    public int? TramitadorLiquidacionesOpcionId { get; set; } // col O
    public string? TramitadorLiquidacionesNombre { get; set; }

    [MaxLength(500)]
    public string? ObservacionesLiquidaciones { get; set; }  // col P

    public bool FaltaPoliza { get; set; }                    // col Q

    [MaxLength(200)]
    public string? UltimoMovimientoSade { get; set; }        // col X

    // col Y: calculado en memoria
    public int? DiasEnElArea => UltimoMovimientoSade is not null
        && DateTime.TryParse(UltimoMovimientoSade, out var dt)
        ? (int)(DateTime.Today - dt.Date).TotalDays
        : null;
}
