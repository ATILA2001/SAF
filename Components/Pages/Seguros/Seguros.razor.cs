using Microsoft.AspNetCore.Components;
using Radzen;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.Seguros;
using SAF.Application.Seguros.Dtos;

namespace SAF.Components.Pages.Seguros;

public partial class Seguros
{
    [Inject] private ISeguroService SeguroService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;

    private IReadOnlyList<SeguroOpcion> _seguroOpciones = Array.Empty<SeguroOpcion>();

    protected override string PageUrl => "/seguros";
    protected override string TituloEntidad => "seguros";
    protected override string ExportNombreHoja => "Seguros";
    protected override string ExportNombreArchivo => "Seguros.xlsx";

    protected override async Task CargarAuxiliaresAsync() =>
        _seguroOpciones = await LookupService.GetSeguroOpcionesAsync();

    protected override async Task<List<SeguroViewModel>> ObtenerDatosAsync() =>
        (await SeguroService.GetAllAsync()).ToList();

    // Búsqueda rápida por los campos que identifican la fila.
    protected override IEnumerable<string?> CamposBusqueda(SeguroViewModel item) =>
        [item.Expediente, item.Op, item.Beneficiario];

    protected override bool CargaProgresiva => true;

    protected override async Task<List<SeguroViewModel>> ObtenerLoteAsync(int skip, int take, CancellationToken ct) =>
        (await SeguroService.GetPageAsync(skip, take, ct)).ToList();

    // Relectura de a una fila: es lo que deja refrescar lo guardado sin recargar la
    // grilla (que devolvía al usuario al primer registro).
    protected override async Task<SeguroViewModel?> ObtenerFilaAsync(SeguroViewModel item, CancellationToken ct) =>
        item.Id != 0 ? await SeguroService.GetByIdAsync(item.Id, ct) : null;

    protected override string DescripcionFila(SeguroViewModel item) =>
        $"Seguro del expediente {item.Expediente}";

    protected override int? IdAuditoria(SeguroViewModel item) => item.Id != 0 ? item.Id : null;

    // El dropdown edita SeguroOpcionId ([AuditIgnore]): sin resolver el nombre visible
    // antes del diff, el cambio de SEGURO —la columna central de esta vista— no
    // dejaría ningún rastro en el historial.
    protected override void PrepararParaGuardar(SeguroViewModel item) =>
        item.SeguroNombre = _seguroOpciones.FirstOrDefault(x => x.Id == item.SeguroOpcionId)?.Nombre;

    protected override IReadOnlyList<string> Validar(SeguroViewModel item, bool esAlta) =>
        SeguroValidator.Validar(item);

    protected override async Task CrearAsync(SeguroViewModel item)
    {
        // El Id y la versión del alta vuelven a la fila en pantalla: los usa la
        // auditoría (historial por Id) y una edición inmediata sin recargar.
        var creado = await SeguroService.CreateAsync(item);
        item.Id = creado.Id;
        item.RowVersion = creado.RowVersion;
    }

    protected override Task ActualizarAsync(SeguroViewModel item) => SeguroService.UpdateAsync(item);

    /// <summary>
    /// Edición en ventana: todos los campos con labels. Comparte el borrador con la
    /// edición inline; cerrar sin guardar lo descarta.
    /// </summary>
    private async Task EditarEnVentana(SeguroViewModel item)
    {
        var resultado = await DialogService.OpenAsync<SeguroEditor>(
            $"Editar — Seguro del expediente {item.Expediente}",
            new Dictionary<string, object?>
            {
                [nameof(SeguroEditor.Item)] = item,
                [nameof(SeguroEditor.Borrador)] = Buffer(item),
                [nameof(SeguroEditor.SeguroOpciones)] = _seguroOpciones,
                [nameof(SeguroEditor.GuardarAsync)] = (Func<Task<bool>>)(() => GuardarDesdeDialogoAsync(item)),
            },
            new DialogOptions { Width = "1000px", ShowTitle = false, CssClass = "saf-dialog-panel" });

        if (resultado is not true) DescartarBuffer(item);
    }

    /// <summary>
    /// Alta en ventana (reemplaza al alta inline): el panel de identidad se completa
    /// en vivo con lo tipeado. Cancelar descarta el objeto solo.
    /// </summary>
    private async Task NuevoEnVentana()
    {
        var item = NuevaFila();

        await DialogService.OpenAsync<SeguroEditor>(
            "Nuevo seguro",
            new Dictionary<string, object?>
            {
                // El mismo objeto como Item y Borrador: el panel refleja lo que se tipea.
                [nameof(SeguroEditor.Item)] = item,
                [nameof(SeguroEditor.Borrador)] = item,
                [nameof(SeguroEditor.EsAlta)] = true,
                [nameof(SeguroEditor.SeguroOpciones)] = _seguroOpciones,
                [nameof(SeguroEditor.GuardarAsync)] = (Func<Task<bool>>)(() => CrearDesdeDialogoAsync(item)),
            },
            new DialogOptions { Width = "1000px", ShowTitle = false, CssClass = "saf-dialog-panel" });
    }

    protected override Task EliminarAsync(SeguroViewModel item) =>
        item.Id != 0 ? SeguroService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}