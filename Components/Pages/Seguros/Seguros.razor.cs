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

    protected override Task EliminarAsync(SeguroViewModel item) =>
        item.Id != 0 ? SeguroService.DeleteAsync(item.Id, item.RowVersion) : Task.CompletedTask;
}