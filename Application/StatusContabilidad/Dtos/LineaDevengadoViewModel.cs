#nullable enable

namespace SAF.Application.StatusContabilidad.Dtos;

/// <summary>
/// Una línea del ledger detrás de una fila agrupada del tablero (neto o retención).
/// Solo para mostrar en pantalla de dónde sale el importe sumado; no se exporta.
/// </summary>
public class LineaDevengadoViewModel
{
    public int Id { get; set; }
    public DateTime? FechaImputacion { get; set; }
    public string? Expediente { get; set; }
    public string? Empresa { get; set; }
    public decimal? Importe { get; set; }

    /// <summary>De esta línea salen el expediente, la empresa y los datos cargados.</summary>
    public bool EsRepresentante { get; set; }
}