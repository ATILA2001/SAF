#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Tabla de opciones para el campo Seguro (vista Seguros). Normaliza el texto libre
/// del Excel original (ok / oK / CAF/ok) a una lista cerrada.
/// </summary>
public class SeguroOpcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }

    /// <summary>
    /// True si el valor cuenta como "seguro en regla". En el cruce a Pagos
    /// (SEGUROS TESO) rige "peor caso gana": si alguna fila del expediente
    /// tiene una opción con EsOk=false, se muestra esa.
    /// </summary>
    public bool EsOk { get; set; }

    public bool Activo { get; set; } = true;
}
