#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Lectura de la tabla [IVC].[dbo].[DEVENGADOS] (solo lectura, sin clave EF).
/// Esta tabla la recarga IVC todos los días (borra + reinserta), por lo que NO
/// se lee directamente en las vistas: solo alimenta la tabla acumulativa
/// <see cref="Devengado"/> de SAF mediante la sincronización manual.
/// El mapeo de columnas se define en IvcDbContext.
/// </summary>
public class IvcDevengado
{
    public string TipoDev { get; set; } = string.Empty;   // TIPO_DEV
    public int NroDev { get; set; }                        // NUMERO_DEVENGADO
    public DateTime? FechaImputacion { get; set; }         // FECHA_IMPUTACION
    public string? EeFinanciera { get; set; }              // EE_FINANCIERA (Expediente en Pagos)
    public string? Descripcion { get; set; }               // DESCRIPCION (Empresa en Pagos)
    public decimal? ImportePp { get; set; }                // IMPORTE_PP (Importe en Pagos)
}
