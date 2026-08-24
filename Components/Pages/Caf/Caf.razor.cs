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

    // Búsqueda rápida por los campos que identifican la fila.
    protected override IEnumerable<string?> CamposBusqueda(CafViewModel item) =>
        [item.Expediente, item.Op, item.Beneficiario];

    protected override bool CargaProgresiva => true;

    protected override async Task<List<CafViewModel>> ObtenerLoteAsync(int skip, int take, CancellationToken ct) =>
        (await CafService.GetPageAsync(skip, take, ct)).ToList();

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

    protected override Task EliminarAsync(CafViewModel item) =>
        item.Id != 0 ? CafService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}