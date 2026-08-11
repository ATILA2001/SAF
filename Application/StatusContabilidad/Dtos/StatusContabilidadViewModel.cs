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
    [ExportIgnore, AuditIgnore]
    public int CantidadLineas { get; set; } = 1;

    /// <summary>En qué difieren esas líneas, para mostrar de dónde sale lo que ve el tablero.</summary>
    [ExportIgnore, AuditIgnore]
    public DetalleLineasViewModel DetalleLineas { get; set; } = new();

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

    // De IVC.PASES_SADE (VLOOKUP a la hoja SADE en el Excel, solo lectura). No se
    // audita ni se restaura al cancelar: la muta CompletarIvcAsync, no el usuario.
    [Display(Name = "Buzón Sade")]
    [AuditIgnore, RestoreIgnore]
    public string? BuzonSade { get; set; }           // col W

    // ─── Datos propios SAF (editables) ───────────────────────────────────────
    [Display(Name = "Fecha pedido de factura 2")]
    public DateTime? FechaPedidoFactura2 { get; set; }       // col I

    [Display(Name = "Reiterar Pedido de Factura 3")]
    public DateTime? ReiterarPedidoFactura3 { get; set; }    // col J

    [Display(Name = "Fecha de rechazo")]
    public DateTime? FechaRechazo { get; set; }

    [Display(Name = "Fecha de Ingreso Factura (correcta)")]
    public DateTime? FechaIngresoFactura { get; set; }       // col K

    // col K (variante texto del Excel): marca cuando no hay fecha de factura.
    [Display(Name = "Sin Factura (motivo)")]
    [MaxLength(20)]
    public string? SinFacturaMotivo { get; set; }            // N/C · CCOO · PAV · Anulado

    [ExportIgnore, AuditIgnore]
    public int? StatusContableOpcionId { get; set; }          // col L

    [Display(Name = "Status Contable")]
    public string? StatusContableNombre { get; set; }

    [Display(Name = "Observaciones Ctas. a Pagar")]
    [MaxLength(500)]
    public string? ObservacionesCuentasPagar { get; set; }   // col M

    [ExportIgnore, AuditIgnore]
    public int? TramitadorCuentasPagarOpcionId { get; set; }  // col N

    [Display(Name = "Usuario Tramitador Cuentas a Pagar")]
    public string? TramitadorCuentasPagarNombre { get; set; }

    [ExportIgnore, AuditIgnore]
    public int? TramitadorLiquidacionesOpcionId { get; set; } // col O

    [Display(Name = "Usuario tramitador Liquidaciones")]
    public string? TramitadorLiquidacionesNombre { get; set; }

    [Display(Name = "Observaciones Liquidaciones")]
    [MaxLength(500)]
    public string? ObservacionesLiquidaciones { get; set; }  // col P

    // col X: derivada de IVC.PASES_SADE (fecha del último pase, solo lectura)
    [Display(Name = "Ultimo Movimiento")]
    [AuditIgnore, RestoreIgnore] // la muta CompletarIvcAsync en segundo plano, no el usuario
    public DateTime? UltimoMovimientoSade { get; set; }

    // col Y — Excel: =TODAY()-X. Calculado en memoria.
    [Display(Name = "Dias en el area")]
    public int? DiasEnElArea => UltimoMovimientoSade is DateTime dt
        ? (int)(DateTime.Today - dt.Date).TotalDays
        : null;

    // ─── Avisos de pedidos de factura atrasados ──────────────────────────────
    // La factura todavía no ingresó: sin fecha correcta y sin marca "Sin factura"
    // (N/C · CCOO · PAV · Anulado, que significan que no va a ingresar).
    private bool SinIngresoFactura =>
        FechaIngresoFactura is null && string.IsNullOrWhiteSpace(SinFacturaMotivo);

    /// <summary>
    /// Toca hacer el 2º pedido: pasaron más de 7 días corridos desde el pedido 1,
    /// la factura no ingresó y el pedido 2 sigue sin cargarse.
    /// </summary>
    [ExportIgnore, AuditIgnore]
    public bool PedidoFactura2Atrasado =>
        SinIngresoFactura
        && FechaPedidoFactura2 is null
        && FechaPedidoFactura1 is DateTime f1
        && (DateTime.Today - f1.Date).TotalDays > 7;

    /// <summary>
    /// Toca reiterar el pedido (3º): pasaron más de 7 días corridos desde el pedido 2,
    /// la factura no ingresó y el pedido 3 sigue sin cargarse.
    /// </summary>
    [ExportIgnore, AuditIgnore]
    public bool PedidoFactura3Atrasado =>
        SinIngresoFactura
        && ReiterarPedidoFactura3 is null
        && FechaPedidoFactura2 is DateTime f2
        && (DateTime.Today - f2.Date).TotalDays > 7;

    // El aviso como valor de columna: así el filtro nativo de la grilla puede listar
    // los atrasos (el resaltado de la celda es estilo, no un dato filtrable). Congelado
    // al cargar y no calculado en vivo: los editores escriben directo sobre la fila, y
    // si cargar la fecha del pedido apagara el atraso al instante, el filtro sacaría la
    // fila de la vista en plena edición, sin poder guardarla. Se recalcula al recargar.
    [ExportIgnore, AuditIgnore]
    public string? Atraso { get; set; }

    /// <summary>Fija <see cref="Atraso"/> según el estado actual de la fila (excluyentes:
    /// el 2 exige pedido 2 vacío y el 3 exige pedido 2 cargado).</summary>
    public void CalcularAtraso() => Atraso =
        PedidoFactura2Atrasado ? "Pedido 2"
        : PedidoFactura3Atrasado ? "Pedido 3"
        : null;

    // Versión de la fila al momento de cargarla: viaja a la grilla y vuelve al guardar,
    // para detectar que otro usuario la modificó mientras se editaba.
    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
