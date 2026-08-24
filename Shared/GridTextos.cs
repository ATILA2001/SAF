namespace SAF.Shared;

/// <summary>
/// Textos compartidos de las grillas. Centralizados para que las cinco vistas
/// digan siempre lo mismo: cuando estaban repetidos en cada markup, un cambio
/// de criterio llegaba a unas vistas y a otras no (los textos traducidos del
/// selector de columnas estuvieron un tiempo solo en Status Contabilidad).
/// </summary>
public static class GridTextos
{
    public const string Vacio = "No hay filas para mostrar. Si aplicaste filtros, podés quitarlos con el botón de la barra superior.";

    // Popup de filtro por columna.
    public const string LimpiarFiltro = "Limpiar";
    public const string AplicarFiltro = "Aplicar";

    // Selector de columnas (column picker).
    public const string Columnas = "Columnas";
    public const string TodasLasColumnas = "Todas";
    public const string ColumnasMostradas = "columnas mostradas";
}
