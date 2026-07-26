using Microsoft.AspNetCore.Components;
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

    protected override CafViewModel NuevaFila() => new() { Anio = DateTime.Now.Year };

    protected override string DescripcionFila(CafViewModel item) =>
        $"Expediente CAF {item.Expediente}";

    protected override string TextoConfirmacionEliminar(CafViewModel item) =>
        $"Se eliminará el expediente CAF {item.Expediente} (año {item.Anio}). " +
        "Esta acción no se puede deshacer.";

    protected override IReadOnlyList<string> Validar(CafViewModel item, bool esAlta) =>
        CafValidator.Validar(item);

    protected override Task CrearAsync(CafViewModel item) => CafService.CreateAsync(item);

    protected override Task ActualizarAsync(CafViewModel item) => CafService.UpdateAsync(item);

    protected override Task EliminarAsync(CafViewModel item) =>
        item.Id != 0 ? CafService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}