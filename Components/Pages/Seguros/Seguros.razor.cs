using Microsoft.AspNetCore.Components;
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

    protected override bool CargaProgresiva => true;

    protected override async Task<List<SeguroViewModel>> ObtenerLoteAsync(int skip, int take, CancellationToken ct) =>
        (await SeguroService.GetPageAsync(skip, take, ct)).ToList();

    protected override string DescripcionFila(SeguroViewModel item) =>
        $"Seguro del expediente {item.Expediente}";

    protected override IReadOnlyList<string> Validar(SeguroViewModel item, bool esAlta) =>
        SeguroValidator.Validar(item);

    protected override Task CrearAsync(SeguroViewModel item) => SeguroService.CreateAsync(item);

    protected override Task ActualizarAsync(SeguroViewModel item) => SeguroService.UpdateAsync(item);

    protected override Task EliminarAsync(SeguroViewModel item) =>
        item.Id != 0 ? SeguroService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}