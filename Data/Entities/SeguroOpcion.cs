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

    /// <summary>
    /// True si el valor cuenta como "seguro en regla". Clasificación de la opción
    /// (se administra en Listas); el cruce a Pagos (SEGUROS TESO) resume por
    /// unanimidad de las OPs del expediente, sin regla de peor caso.
    /// </summary>
    public bool EsOk { get; set; }

    public bool Activo { get; set; } = true;
}
