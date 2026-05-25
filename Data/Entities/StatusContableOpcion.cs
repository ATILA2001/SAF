#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tabla de opciones para el campo Status Contable (vista Status Contabilidad).
/// </summary>
public class StatusContableOpcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
