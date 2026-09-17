#nullable enable
using Radzen;

namespace SAF.Shared;

/// <summary>
/// Estado de las grillas que sobrevive a la navegación. Cambiar de vista destruye la
/// página y con ella la grilla, así que los filtros de columna, el orden, los anchos
/// y la búsqueda rápida se perdían al volver y había que rearmarlos cada vez. Es un
/// servicio scoped: en Blazor Server vive lo que vive el circuito (la pestaña), ni
/// más —no se comparte entre usuarios ni pestañas— ni menos —una recarga completa
/// (F5) lo vacía, que es lo esperable—.
/// </summary>
public sealed class EstadoGrillas
{
    private readonly Dictionary<string, EstadoGrilla> _porClave = new(StringComparer.Ordinal);

    /// <summary>El estado de una grilla; se crea vacío la primera vez que se pide.</summary>
    public EstadoGrilla De(string clave)
    {
        if (!_porClave.TryGetValue(clave, out var estado))
            _porClave[clave] = estado = new EstadoGrilla();
        return estado;
    }
}

/// <summary>Lo que una grilla guarda entre visitas a su vista.</summary>
public sealed class EstadoGrilla
{
    /// <summary>Filtros, orden, anchos y columnas visibles, tal como los reporta Radzen.</summary>
    public DataGridSettings? Settings { get; set; }

    /// <summary>Texto de la búsqueda rápida de la toolbar.</summary>
    public string TextoBusqueda { get; set; } = string.Empty;
}
