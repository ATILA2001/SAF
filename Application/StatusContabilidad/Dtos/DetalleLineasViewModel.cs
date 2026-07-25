#nullable enable

namespace SAF.Application.StatusContabilidad.Dtos;

/// <summary>
/// Comparación de las líneas del ledger que el tablero resume en una sola fila.
/// Solo trae las columnas cuyo valor difiere entre líneas: repetir lo que es igual
/// en todas no aporta nada. Es para mostrar en pantalla; no se exporta.
/// </summary>
public class DetalleLineasViewModel
{
    /// <summary>Títulos de las columnas que difieren, en el orden de la grilla.</summary>
    public IReadOnlyList<string> Columnas { get; set; } = Array.Empty<string>();

    public IReadOnlyList<FilaLineaViewModel> Filas { get; set; } = Array.Empty<FilaLineaViewModel>();

    /// <summary>Las líneas son idénticas en todas las columnas comparadas.</summary>
    public bool SinDiferencias => Columnas.Count == 0;
}

/// <summary>Una línea del ledger, con sus valores alineados a <see cref="DetalleLineasViewModel.Columnas"/>.</summary>
public class FilaLineaViewModel
{
    /// <summary>De esta línea salen el expediente, la empresa y los datos cargados.</summary>
    public bool EsPrincipal { get; set; }

    public IReadOnlyList<string> Valores { get; set; } = Array.Empty<string>();
}