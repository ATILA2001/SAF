#nullable enable
using SAF.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace SAF.Application.Pagos.Dtos;

/// <summary>
/// ViewModel para la vista PAGOS.
/// Combina datos de IVC (solo lectura) con datos propios del SAF (editables).
/// [Display(Name)] = título de la columna en la planilla original (se usa al exportar).
/// </summary>
public class PagoViewModel
{
    /// <summary>Id de la FILA del ledger de devengados. Cada fila se edita por separado.</summary>
    [ExportIgnore, AuditIgnore]
    public int Id { get; set; }

    // ─── Datos de IVC (solo lectura) ─────────────────────────────────────────
    [Display(Name = "TIPO DEV")]
    public string TipoDev { get; set; } = string.Empty;

    [Display(Name = "NRO DEV")]
    public int NroDev { get; set; }

    [Display(Name = "FECHA DEVENGADO")]
    public DateTime? FechaDevengado { get; set; }

    [Display(Name = "EXPEDIENTE")]
    public string? Expediente { get; set; }

    [Display(Name = "EMPRESA")]
    public string? Empresa { get; set; }

    [Display(Name = "IMPORTE")]
    public decimal? Importe { get; set; }

    // ─── Datos propios SAF (editables) ───────────────────────────────────────
    // Los Ids de lookups no se auditan: el cambio se registra por el Nombre visible.
    [ExportIgnore, AuditIgnore]
    public int? StatusDgayfOpcionId { get; set; }

    [Display(Name = "STATUS DGAyF")]
    public string? StatusDgayfNombre { get; set; }

    [ExportIgnore, AuditIgnore]
    public int? StatusOpOpcionId { get; set; }

    [Display(Name = "STATUS OP")]
    public string? StatusOpNombre { get; set; }

    [Display(Name = "FECHA FIRMA OP")]
    public DateTime? FechaFirmaOp { get; set; }

    [Display(Name = "OBSERVACIONES")]
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [Display(Name = "CCOO")]
    [MaxLength(200)]
    public string? Ccoo { get; set; }

    [Display(Name = "FECHA CCOO")]
    public DateTime? FechaCcoo { get; set; }

    [Display(Name = "FECHA NOTIFICACION")]
    public DateTime? FechaNotificacion { get; set; }

    [Display(Name = "STATUS CONTABLE")]
    [MaxLength(100)]
    public string? StatusContable { get; set; }

    [Display(Name = "SEGUROS TESO")]
    [MaxLength(200)]
    public string? SegurosTeso { get; set; }

    // Las cuatro columnas que muta CompletarIvcAsync en segundo plano no se auditan:
    // si cambian durante una edición no fue el usuario.
    [Display(Name = "FECHA DE PAGO - NO CAF")]
    [AuditIgnore]
    public DateTime? FechaDePagoNoCaf { get; set; }

    [Display(Name = "FECHA DE PAGO - CAF")]
    public DateTime? FechaDePagoCaf { get; set; }

    // Estado de las OPs del expediente en CAF: si están pagadas solo en parte no hay
    // fecha (sería informar un pago completo), pero el usuario tiene que ver por qué.
    [ExportIgnore]
    public int CafOps { get; set; }

    [ExportIgnore]
    public int CafOpsPagadas { get; set; }

    /// <summary>
    /// Hay OPs del expediente sin pagar (incluye "ninguna pagada"): no hay fecha, y el
    /// badge tiene que explicar por qué — una celda vacía se leería como "sin OPs en CAF".
    /// </summary>
    [ExportIgnore]
    public bool CafPagoIncompleto => CafOps > 0 && CafOpsPagadas < CafOps;

    /// <summary>Detalle de esas OPs, para ver cuáles se pagaron y cuáles faltan.</summary>
    [ExportIgnore]
    public IReadOnlyList<SAF.Application.Caf.Dtos.LineaCafViewModel> CafLineas { get; set; }
        = Array.Empty<SAF.Application.Caf.Dtos.LineaCafViewModel>();

    [Display(Name = "FECHA PAGO TOTAL")]
    [AuditIgnore]
    public DateTime? FechaPagoTotal { get; set; }

    [Display(Name = "FECHA SADE")]
    [AuditIgnore]
    public DateTime? FechaSade { get; set; }

    [Display(Name = "BUZON SADE")]
    [MaxLength(200)]
    [AuditIgnore]
    public string? BuzonSade { get; set; }

    [Display(Name = "PEDIDO FACTURA 2")]
    public DateTime? PedidoFactura2 { get; set; }

    [Display(Name = "PEDIDO FACTURA 3")]
    public DateTime? PedidoFactura3 { get; set; }

    [Display(Name = "FECHA FACTURA CORRECTA")]
    public DateTime? FechaFacturaCorrecta { get; set; }

    // En standby (fuente PRESUPUESTO 2026 no implementada); fuera del export.
    [ExportIgnore]
    public bool? CafSiNo { get; set; }

    // Versión de la fila al momento de cargarla: viaja a la grilla y vuelve al guardar,
    // para detectar que otro usuario la modificó mientras se editaba.
    [ExportIgnore]
    public byte[]? RowVersion { get; set; }
}
