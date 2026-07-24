#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tabla acumulativa propia de SAF (ledger de devengados), equivalente al
/// "segundo Excel" histórico. Se llena por sincronización manual desde IVC
/// (ver DevengadoSyncService) aplicando el filtro de la vista Pagos
/// (TIPO_DEV NOT IN ('C55','CPS') AND IMPORTE_PP > 0).
/// Conserva histórico aunque IVC borre/recargue su tabla a diario.
/// Las filas se guardan tal cual (sin agrupar).
/// </summary>
public class Devengado
{
    public int Id { get; set; }

    // Clave de negocio (referencia al devengado de IVC)
    public string TipoDev { get; set; } = string.Empty;
    public int NroDev { get; set; }

    public DateTime? FechaImputacion { get; set; }   // FECHA_IMPUTACION de IVC
    public string? Expediente { get; set; }          // EE_FINANCIERA de IVC
    public string? Empresa { get; set; }             // DESCRIPCION de IVC
    public decimal? ImportePp { get; set; }          // IMPORTE_PP de IVC

    /// <summary>Momento en que SAF importó esta fila desde IVC.</summary>
    public DateTime FechaImportacion { get; set; } = DateTime.UtcNow;
}
