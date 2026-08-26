using Microsoft.AspNetCore.Components;
using Radzen;
using SAF.Services.Abstractions;
using SAF.Application.Caf;
using SAF.Application.Caf.Dtos;

namespace SAF.Components.Pages.Caf;

public partial class Caf
{
    [Inject] private ICafService CafService { get; set; } = null!;

    protected override string PageUrl => "/caf";
    protected override string TituloEntidad => "expedientes CAF";
    protected override string ExportNombreHoja => "Expedientes CAF";
    protected override string ExportNombreArchivo => "ExpedientesCaf.xlsx";

    protected override async Task<List<CafViewModel>> ObtenerDatosAsync() =>
        (await CafService.GetAllAsync()).ToList();

    // Búsqueda rápida por los campos que identifican la fila.
    protected override IEnumerable<string?> CamposBusqueda(CafViewModel item) =>
        [item.Expediente, item.Op, item.Beneficiario];

    protected override bool CargaProgresiva => true;

    protected override async Task<List<CafViewModel>> ObtenerLoteAsync(int skip, int take, CancellationToken ct) =>
        (await CafService.GetPageAsync(skip, take, ct)).ToList();

    // Relectura de a una fila: es lo que deja refrescar lo guardado sin recargar la
    // grilla (que devolvía al usuario al primer registro).
    protected override async Task<CafViewModel?> ObtenerFilaAsync(CafViewModel item, CancellationToken ct) =>
        item.Id != 0 ? await CafService.GetByIdAsync(item.Id, ct) : null;

    protected override CafViewModel NuevaFila() => new() { Anio = DateTime.Now.Year };

    protected override string DescripcionFila(CafViewModel item) =>
        $"Expediente CAF {item.Expediente}";

    protected override string TextoConfirmacionEliminar(CafViewModel item) =>
        $"Se eliminará el expediente CAF {item.Expediente} (año {item.Anio}). " +
        "Esta acción no se puede deshacer.";

    protected override IReadOnlyList<string> Validar(CafViewModel item, bool esAlta) =>
        CafValidator.Validar(item);

    protected override int? IdAuditoria(CafViewModel item) => item.Id != 0 ? item.Id : null;

    protected override async Task CrearAsync(CafViewModel item)
    {
        // El Id y la versión del alta vuelven a la fila en pantalla: los usa la
        // auditoría (historial por Id) y una edición inmediata sin recargar.
        var creado = await CafService.CreateAsync(item);
        item.Id = creado.Id;
        item.RowVersion = creado.RowVersion;
    }

    protected override Task ActualizarAsync(CafViewModel item) => CafService.UpdateAsync(item);

    /// <summary>
    /// Edición en ventana: todos los campos con labels. Comparte el borrador con la
    /// edición inline; cerrar sin guardar lo descarta.
    /// </summary>
    private async Task EditarEnVentana(CafViewModel item)
    {
        var resultado = await DialogService.OpenAsync<CafEditor>(
            $"Editar — Expediente CAF {item.Expediente}",
            new Dictionary<string, object?>
            {
                [nameof(CafEditor.Item)] = item,
                [nameof(CafEditor.Borrador)] = Buffer(item),
                [nameof(CafEditor.GuardarAsync)] = (Func<Task<bool>>)(() => GuardarDesdeDialogoAsync(item)),
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

        await DialogService.OpenAsync<CafEditor>(
            "Nuevo expediente CAF",
            new Dictionary<string, object?>
            {
                // El mismo objeto como Item y Borrador: el panel refleja lo que se tipea.
                [nameof(CafEditor.Item)] = item,
                [nameof(CafEditor.Borrador)] = item,
                [nameof(CafEditor.EsAlta)] = true,
                [nameof(CafEditor.GuardarAsync)] = (Func<Task<bool>>)(() => CrearDesdeDialogoAsync(item)),
            },
            new DialogOptions { Width = "1000px", ShowTitle = false, CssClass = "saf-dialog-panel" });
    }

    protected override Task EliminarAsync(CafViewModel item) =>
        item.Id != 0 ? CafService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}