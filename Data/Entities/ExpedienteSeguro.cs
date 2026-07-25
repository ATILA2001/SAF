#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Seguro de un expediente (carga propia de SAF, editable). Reemplaza y mejora la hoja
/// SEGUROS que alimenta la columna "Seguros Teso" del tablero PAGOS (archivo original
/// "PAGOS PENDIENTES-.xlsx" hoja Pagos). El campo <see cref="Seguro"/> (ej. "ok") es el
/// que se cruza a Pagos por expediente financiera.
///
/// Mejora: columna ÚNICA de expediente, guardada ya normalizada a NNNNNNNN/AA
/// (ver ExpedienteKey.Normalizar; la UI valida y rechaza formatos inválidos).
/// </summary>
public class ExpedienteSeguro
{
    public int Id { get; set; }

    public string Expediente { get; set; } = string.Empty;   // clave financiera "NNNNNNNN/AA" (cruce con Pagos)

    public string? Op { get; set; }
    public string? Beneficiario { get; set; }
    public decimal? ImporteNeto { get; set; }
    public string? Estado { get; set; }

    // Seguro normalizado a lista de valores (antes texto libre "ok"/"oK"/"CAF/ok").
    // Alimenta SEGUROS TESO en Pagos con la regla "peor caso gana".
    public int? SeguroOpcionId { get; set; }
    public SeguroOpcion? SeguroOpcion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    /// <summary>Control de concurrencia optimista: SQL Server la actualiza en cada UPDATE.</summary>
    public byte[]? RowVersion { get; set; }
}
