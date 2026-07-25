#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.StatusContabilidad.Dtos;

/// <summary>
/// ViewModel para la vista STATUS CONTABILIDAD (tablero contable).
/// Columnas derivadas de IVC + PAGOS + datos propios del SAF.
/// [Display(Name)] = título de la columna en la planilla original (se usa al exportar).
/// </summary>
public class StatusContabilidadViewModel
{
    // ─── De IVC / Pagos (solo lectura) ───────────────────────────────────────
    [Display(Name = "TIPO DEV")]
    public string TipoDev { get; set; } = string.Empty;

    [Display(Name = "NRO DEV")]
    public int NroDev { get; set; }

    [Display(Name = "EXPEDIENTE")]
    public string? Expediente { get; set; }

    [Display(Name = "EMPRESA")]
    public string? Empresa { get; set; }

    [Display(Name = "IMPORTE")]
    public decimal? ImporteTotal { get; set; }      // suma por grupo TipoDev+NroDev

    /// <summary>
    /// Cuántas líneas del ledger resume esta fila (neto + retenciones). El tablero agrupa
    /// y en Pagos se ven separadas: se avisa en la grilla cuando hay más de una.
    /// </summary>
    [ExportIgnore]
    public int CantidadLineas { get; set; } = 1;

    [Display(Name = "STATUS DGAyF")]
    public string? StatusDgayfNombre { get; set; }   // col F

    [Display(Name = "firmada por Miguel?")]
    public string? FirmadaPorMiguel { get; set; }    // col G = STATUS OP de PAGOS

    [Display(Name = "Fecha pedido de factura 1")]
    public DateTime? FechaPedidoFactura1 { get; set; } // col H = FECHA DEVENGADO

    // EE SADE derivado de Expediente
    [Display(Name = "EE SADE")]
    public string? EeSade { get; set; }              // col R (calculado)

    // De PAGOS
    [Display(Name = "OBSERVACIONES")]
    public string? Observaciones { get; set; }       // col S

    [Display(Name = "CCOO")]
    public string? Ccoo { get; set; }                // col T

    [Display(Name = "FECHA CCOO")]
    public DateTime? FechaCcoo { get; set; }         // col U

    [Display(Name = "FECHA NOTIFICACION")]
    public DateTime? FechaNotificacion { get; set; } // col V

    // De IVC.PASES_SADE (VLOOKUP a la hoja SADE en el Excel, solo lectura)
    [Display(Name = "Buzón Sade")]
    public string? BuzonSade { get; set; }           // col W

    // ─── Datos propios SAF (editables) ───────────────────────────────────────
    [Display(Name = "Fecha pedido de factura 2")]
    public DateTime? FechaPedidoFactura2 { get; set; }       // col I

    [Display(Name = "Reiterar Pedido de Factura 3")]
    public DateTime? ReiterarPedidoFactura3 { get; set; }    // col J

    [Display(Name = "Fecha de Ingreso Factura (correcta)")]
    public DateTime? FechaIngresoFactura { get; set; }       // col K

    // col K (variante texto del Excel): marca cuando no hay fecha de factura.
    [Display(Name = "Sin Factura (motivo)")]
    [MaxLength(20)]
    public string? SinFacturaMotivo { get; set; }            // N/C · CCOO · PAV · Anulado

    [ExportIgnore]
    public int? StatusContableOpcionId { get; set; }          // col L

    [Display(Name = "Status Contable")]
    public string? StatusContableNombre { get; set; }

    [Display(Name = "Observaciones Ctas. a Pagar")]
    [MaxLength(500)]
    public string? ObservacionesCuentasPagar { get; set; }   // col M

    [ExportIgnore]
    public int? TramitadorCuentasPagarOpcionId { get; set; }  // col N

    [Display(Name = "Usuario Tramitador Cuentas a Pagar")]
    public string? TramitadorCuentasPagarNombre { get; set; }

    [ExportIgnore]
    public int? TramitadorLiquidacionesOpcionId { get; set; } // col O

    [Display(Name = "Usuario tramitador Liquidaciones")]
    public string? TramitadorLiquidacionesNombre { get; set; }

    [Display(Name = "Observaciones Liquidaciones")]
    [MaxLength(500)]
    public string? ObservacionesLiquidaciones { get; set; }  // col P

    [Display(Name = "FALTA POLIZA")]
    public bool FaltaPoliza { get; set; }                    // col Q

    // col X: derivada de IVC.PASES_SADE (fecha del último pase, solo lectura)
    [Display(Name = "Ultimo Movimiento")]
    public DateTime? UltimoMovimientoSade { get; set; }

    // col Y — Excel: =TODAY()-X. Calculado en memoria.
    [Display(Name = "Dias en el area")]
    public int? DiasEnElArea => UltimoMovimientoSade is DateTime dt
        ? (int)(DateTime.Today - dt.Date).TotalDays
        : null;

    // Versión de la fila al momento de cargarla: viaja a la grilla y vuelve al guardar,
    // para detectar que otro usuario la modificó mientras se editaba.
    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
