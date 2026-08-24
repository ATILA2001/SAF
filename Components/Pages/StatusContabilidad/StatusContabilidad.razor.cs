using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using SAF.Data.Entities;
using SAF.Security;
using SAF.Services.Abstractions;
using SAF.Application.StatusContabilidad;
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Components.Pages.StatusContabilidad;

public partial class StatusContabilidad
{
    [Inject] private IStatusContabilidadService StatusContabilidadService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;
    [Inject] private TooltipService TooltipService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;

    // Marcas de la col. "Fecha de Ingreso Factura (correcta)" del Excel cuando no hay fecha.
    private static readonly string[] _sinFacturaMotivos = ["N/C", "CCOO", "PAV", "Anulado"];

    private IReadOnlyList<StatusContableOpcion> _statusContableOpciones = Array.Empty<StatusContableOpcion>();
    private IReadOnlyList<TramitadorCuentasPagarOpcion> _tramitadoresCuentasPagar = Array.Empty<TramitadorCuentasPagarOpcion>();
    private IReadOnlyList<TramitadorLiquidacionesOpcion> _tramitadoresLiquidaciones = Array.Empty<TramitadorLiquidacionesOpcion>();

    // Edición por área: las columnas de Cuentas a Pagar y de Liquidaciones solo las
    // completan los usuarios de esas áreas (claims "area" de la cookie contra los IDs
    // configurados). Contabilidad y los admins editan todo. Sin config, quedan solo
    // para admins.
    private bool _editaCuentasPagar;
    private bool _editaLiquidaciones;

    protected override string PageUrl => "/status-contabilidad";
    protected override string TituloEntidad => "Status Contabilidad";
    protected override string ExportNombreHoja => "Status Contabilidad";
    protected override string ExportNombreArchivo => "StatusContabilidad.xlsx";

    protected override async Task CargarAuxiliaresAsync()
    {
        await CargarEdicionPorAreaAsync();

        // En paralelo: cada repositorio crea su propio DbContext (IDbContextFactory).
        var contables    = LookupService.GetStatusContableOpcionesAsync();
        var cuentasPagar = LookupService.GetTramitadoresCuentasPagarAsync();
        var liquidaciones = LookupService.GetTramitadoresLiquidacionesAsync();
        await Task.WhenAll(contables, cuentasPagar, liquidaciones);

        _statusContableOpciones    = contables.Result;
        _tramitadoresCuentasPagar  = cuentasPagar.Result;
        _tramitadoresLiquidaciones = liquidaciones.Result;
    }

    private async Task CargarEdicionPorAreaAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (AdminClaims.IsAdmin(user))
        {
            _editaCuentasPagar = _editaLiquidaciones = true;
            return;
        }

        var areasUsuario = user.FindAll("area")
            .Select(c => int.TryParse(c.Value, out var id) ? id : -1)
            .Where(id => id > 0)
            .ToHashSet();

        var areasCuentasPagar = Configuration.GetSection("StatusContabilidad:AreasCuentasPagar").Get<int[]>() ?? [];
        var areasLiquidaciones = Configuration.GetSection("StatusContabilidad:AreasLiquidaciones").Get<int[]>() ?? [];
        var areasContabilidad = Configuration.GetSection("StatusContabilidad:AreasContabilidad").Get<int[]>() ?? [];

        // Contabilidad supervisa el tablero completo: edita las columnas de ambas áreas.
        var esContabilidad = areasContabilidad.Any(areasUsuario.Contains);

        _editaCuentasPagar  = esContabilidad || areasCuentasPagar.Any(areasUsuario.Contains);
        _editaLiquidaciones = esContabilidad || areasLiquidaciones.Any(areasUsuario.Contains);
    }

    protected override async Task<List<StatusContabilidadViewModel>> ObtenerDatosAsync() =>
        (await StatusContabilidadService.GetAllAsync()).ToList();

    // Búsqueda rápida por los campos que identifican la fila.
    protected override IEnumerable<string?> CamposBusqueda(StatusContabilidadViewModel item) =>
        [item.TipoDev, item.NroDev.ToString(), item.Expediente, item.Empresa];

    // Buzón SADE, Último Movimiento y Días en el Área (IVC) llegan con la grilla pintada.
    protected override bool CompletaEnSegundoPlano => true;

    protected override Task CompletarAsync(List<StatusContabilidadViewModel> items, CancellationToken ct) =>
        StatusContabilidadService.CompletarIvcAsync(items, ct);

    protected override void PrepararParaGuardar(StatusContabilidadViewModel item)
    {
        item.StatusContableNombre = _statusContableOpciones.FirstOrDefault(x => x.Id == item.StatusContableOpcionId)?.Nombre;
        item.TramitadorCuentasPagarNombre = _tramitadoresCuentasPagar.FirstOrDefault(x => x.Id == item.TramitadorCuentasPagarOpcionId)?.Nombre;
        item.TramitadorLiquidacionesNombre = _tramitadoresLiquidaciones.FirstOrDefault(x => x.Id == item.TramitadorLiquidacionesOpcionId)?.Nombre;
    }

    protected override IReadOnlyList<string> Validar(StatusContabilidadViewModel item, bool esAlta) =>
        StatusContabilidadValidator.Validar(item);

    // El tablero es de solo edición: las filas nacen del ledger de devengados, no se crean ni borran acá.
    protected override Task ActualizarAsync(StatusContabilidadViewModel item) =>
        StatusContabilidadService.UpsertAsync(item);

    // El tablero agrupa por devengado: la clave de negocio es el historial (sin Id técnico).
    protected override string DescripcionFila(StatusContabilidadViewModel item) =>
        $"Devengado {item.TipoDev} {item.NroDev}";

    /// <summary>
    /// Edición en ventana: todos los campos a la vista, agrupados por área. Comparte
    /// el borrador con la edición inline; cerrar sin guardar lo descarta.
    /// </summary>
    private async Task EditarEnVentana(StatusContabilidadViewModel item)
    {
        var resultado = await DialogService.OpenAsync<StatusContabilidadEditor>(
            $"Editar — Devengado {item.TipoDev} {item.NroDev}",
            new Dictionary<string, object?>
            {
                [nameof(StatusContabilidadEditor.Item)] = item,
                [nameof(StatusContabilidadEditor.Borrador)] = Buffer(item),
                [nameof(StatusContabilidadEditor.EditaCuentasPagar)] = _editaCuentasPagar,
                [nameof(StatusContabilidadEditor.EditaLiquidaciones)] = _editaLiquidaciones,
                [nameof(StatusContabilidadEditor.StatusContableOpciones)] = _statusContableOpciones,
                [nameof(StatusContabilidadEditor.TramitadoresCuentasPagar)] = _tramitadoresCuentasPagar,
                [nameof(StatusContabilidadEditor.TramitadoresLiquidaciones)] = _tramitadoresLiquidaciones,
                [nameof(StatusContabilidadEditor.SinFacturaMotivos)] = _sinFacturaMotivos,
                [nameof(StatusContabilidadEditor.GuardarAsync)] = (Func<Task<bool>>)(() => GuardarDesdeDialogoAsync(item)),
            },
            new DialogOptions { Width = "780px" });

        // Cerrado sin guardar (Cancelar, la X o Escape): se descarta lo tipeado.
        if (resultado is not true) DescartarBuffer(item);
    }

    // El color vive en el token (tokens.css); acá solo la aplicación a la celda.
    private const string EstiloPedidoAtrasado = "background-color: var(--saf-atraso-bg);";

    // Aviso de pedidos atrasados: pinta la celda del pedido que ya corresponde hacer
    // (la regla vive en el ViewModel; acá solo el marcado y su explicación).
    private void OnCellRender(DataGridCellRenderEventArgs<StatusContabilidadViewModel> args)
    {
        if (args.Column is null || args.Data is null) return;

        // Contra el Atraso congelado y no los flags en vivo: el mismo criterio que usa
        // el filtro de la columna Atraso, así pintado y filtro nunca difieren.
        if (args.Column.Property == nameof(StatusContabilidadViewModel.FechaPedidoFactura2)
            && args.Data.Atraso == "Pedido 2")
        {
            args.Attributes["style"] = EstiloPedidoAtrasado;
            args.Attributes["title"] = "Más de 7 días desde el pedido 1 y la factura no ingresó: corresponde el 2º pedido.";
        }
        else if (args.Column.Property == nameof(StatusContabilidadViewModel.ReiterarPedidoFactura3)
            && args.Data.Atraso == "Pedido 3")
        {
            args.Attributes["style"] = EstiloPedidoAtrasado;
            args.Attributes["title"] = "Más de 7 días desde el pedido 2 y la factura no ingresó: corresponde reiterar el pedido (3º).";
        }
    }
}