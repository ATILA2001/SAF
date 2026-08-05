#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Forma común de las tablas de opciones (los desplegables de las vistas).
/// Permite administrarlas con un único CRUD genérico (ver ListaAdminService).
/// </summary>
public interface IOpcionLista
{
    int Id { get; set; }
    string Nombre { get; set; }
    int Orden { get; set; }
    bool Activo { get; set; }
}
