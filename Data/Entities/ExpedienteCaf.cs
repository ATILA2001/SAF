#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Expediente CAF (carga propia de SAF, editable). Reemplaza y mejora la planilla
/// "EXPEDIENTES CAF - 2026.xlsx" (hoja CAF-2026 / "Parte CAF (TESO)").
/// Hay varias filas por expediente (neto + retención IIBB).
///
/// Mejoras sobre el Excel:
///  - Columna ÚNICA de expediente: se guarda ya normalizado a la clave financiera
///    NNNNNNNN/AA (ver ExpedienteKey.Normalizar; la UI valida y rechaza formatos inválidos),
///    corrigiendo el bug de la fórmula original que generaba 9 dígitos y rompía el cruce.
///  - Cargado / Revisado combinan fecha + hora en un solo DateTime.
/// </summary>
public class ExpedienteCaf
{
    public int Id { get; set; }

    public int Anio { get; set; }                        // año de la carga (2026+)

    public string Expediente { get; set; } = string.Empty;   // clave financiera "NNNNNNNN/AA" (cruce con Pagos)

    public string? Op { get; set; }                      // 469337/25
    public string? Beneficiario { get; set; }
    public decimal? ImporteNeto { get; set; }
    public decimal? Iibb { get; set; }                   // IIBB (9010/8)
    public string? Cuenta { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime? Cargado { get; set; }               // CARGADO FECHA + HORA
    public string? CcPagadora { get; set; }
    public string? Pase { get; set; }
    public DateTime? Revisado { get; set; }              // REVISADO FECHA + HORA

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    public byte[]? RowVersion { get; set; }
}
