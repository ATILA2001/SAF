#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tramitadores de Liquidaciones (lookup desplegable en vista Status Contabilidad).
/// </summary>
public class TramitadorLiquidacionesOpcion : IOpcionLista
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
