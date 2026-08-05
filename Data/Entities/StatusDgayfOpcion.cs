#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tabla de opciones para el campo STATUS DGAyF (vista Pagos).
/// </summary>
public class StatusDgayfOpcion : IOpcionLista
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
