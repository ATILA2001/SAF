#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tabla de opciones para el campo Seguro (vista Seguros). Normaliza el texto libre
/// del Excel original (ok / oK / CAF/ok) a una lista cerrada.
/// </summary>
public class SeguroOpcion : IOpcionLista
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
