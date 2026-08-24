using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SAF.Application.AdminListas;
using SAF.Application.AdminListas.Dtos;
using SAF.Security;
using SAF.Services.Abstractions;

namespace SAF.Components.Pages.AdminListas;

public partial class AdminListas
{
    [Inject] private IListaAdminService ListaAdminService { get; set; } = null!;
    [Inject] private IPermissionService PermissionService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private ListaAdminDefinicion _lista = ListasAdmin.Todas[0];
    private IReadOnlyList<ListaAdminDefinicion> _listasVisibles = [];

    protected override string PageUrl => "/admin/listas";
    protected override string TituloEntidad => "opciones de lista";
    protected override string ExportNombreHoja => _lista.Titulo;
    protected override string ExportNombreArchivo => $"Lista_{_lista.Key}.xlsx";

    protected override async Task CargarAuxiliaresAsync()
    {
        // La lectura de las vistas la tienen todos: lo que habilita a administrar
        // la lista de un desplegable es poder EDITAR la vista que lo usa.
        var user = (await AuthStateProvider.GetAuthenticationStateAsync()).User;
        _listasVisibles = AdminClaims.IsAdmin(user)
            ? ListasAdmin.Todas
            : ListasAdmin.Todas.Where(l => PermissionService.CanPerform(user, l.VistaUrl, "edit")).ToList();

        if (!_listasVisibles.Contains(_lista) && _listasVisibles.Count > 0)
            _lista = _listasVisibles[0];
    }

    protected override async Task<List<OpcionListaViewModel>> ObtenerDatosAsync() =>
        _listasVisibles.Count == 0
            ? []
            : (await ListaAdminService.GetAllAsync(_lista.Key)).ToList();

    protected override OpcionListaViewModel NuevaFila() => new()
    {
        Activo = true,
        // Al final de la lista: es lo esperable para un valor nuevo y evita
        // renumerar; el orden fino se ajusta editando la columna Orden.
        Orden = _items.Count == 0 ? 1 : _items.Max(x => x.Orden) + 1,
        EsOk = _lista.TieneEsOk ? false : null,
    };

    protected override void PrepararParaGuardar(OpcionListaViewModel item) =>
        item.Nombre = item.Nombre.Trim();

    protected override string DescripcionFila(OpcionListaViewModel item) =>
        $"\"{item.Nombre}\" ({_lista.Titulo})";

    protected override string TextoConfirmacionEliminar(OpcionListaViewModel item) =>
        $"Se eliminará la opción \"{item.Nombre}\" de {_lista.Titulo}. " +
        "Si ya se usó en registros existentes el borrado va a fallar: en ese caso desactivala. " +
        "Esta acción no se puede deshacer.";

    protected override IReadOnlyList<string> Validar(OpcionListaViewModel item, bool esAlta)
    {
        var errores = OpcionListaValidator.Validar(item).ToList();

        // Duplicado contra lo ya cargado: el servicio revalida contra la base, pero
        // frenar acá deja la fila abierta en vez de cerrar y perder lo tipeado.
        // La exclusión es por Id y no por referencia: lo que llega acá es el buffer de
        // edición (una copia), nunca el mismo objeto que está en la lista.
        if (_items.Any(x => x.Id != item.Id
                && string.Equals(x.Nombre.Trim(), item.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            errores.Add($"Ya existe una opción \"{item.Nombre.Trim()}\" en {_lista.Titulo}.");

        return errores;
    }

    // El historial se busca por clave de negocio: los Ids se repiten entre las 6
    // tablas y bajo la misma Vista mezclarían historiales de listas distintas.
    protected override int? IdAuditoria(OpcionListaViewModel item) => null;

    protected override string ClaveAuditoria(OpcionListaViewModel item) =>
        $"{_lista.Key}:{item.Nombre}";

    protected override async Task CrearAsync(OpcionListaViewModel item)
    {
        // El Id del alta vuelve a la fila en pantalla para poder borrarla o
        // editarla sin recargar.
        var creado = await ListaAdminService.CreateAsync(_lista.Key, item);
        item.Id = creado.Id;
    }

    protected override Task ActualizarAsync(OpcionListaViewModel item) =>
        ListaAdminService.UpdateAsync(_lista.Key, item);

    protected override Task EliminarAsync(OpcionListaViewModel item) =>
        item.Id != 0 ? ListaAdminService.DeleteAsync(_lista.Key, item.Id) : Task.CompletedTask;

    private async Task OnListaChanged()
    {
        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Informar(ex, "El cambio de lista", $"Error al cargar {_lista.Titulo}");
        }
    }
}
