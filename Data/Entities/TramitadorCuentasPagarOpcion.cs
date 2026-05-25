#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tramitadores de Cuentas a Pagar (lookup desplegable en vista Status Contabilidad).
/// </summary>
public class TramitadorCuentasPagarOpcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
