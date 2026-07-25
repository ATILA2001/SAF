using Microsoft.AspNetCore.Components;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Components.Pages.StatusContabilidad;

public partial class StatusContabilidad
{
    [Inject] private IStatusContabilidadService StatusContabilidadService { get; set; } = null!;
    [Inject] private ILookupService LookupService { get; set; } = null!;

    // Marcas de la col. "Fecha de Ingreso Factura (correcta)" del Excel cuando no hay fecha.
    private static readonly string[] _sinFacturaMotivos = ["N/C", "CCOO", "PAV", "Anulado"];

    private IReadOnlyList<StatusContableOpcion> _statusContableOpciones = Array.Empty<StatusContableOpcion>();
    private IReadOnlyList<TramitadorCuentasPagarOpcion> _tramitadoresCuentasPagar = Array.Empty<TramitadorCuentasPagarOpcion>();
    private IReadOnlyList<TramitadorLiquidacionesOpcion> _tramitadoresLiquidaciones = Array.Empty<TramitadorLiquidacionesOpcion>();

    protected override string PageUrl => "/status-contabilidad";
    protected override string TituloEntidad => "Status Contabilidad";
    protected override string ExportNombreHoja => "Status Contabilidad";
    protected override string ExportNombreArchivo => "StatusContabilidad.xlsx";

    protected override async Task CargarAuxiliaresAsync()
    {
        // En paralelo: cada repositorio crea su propio DbContext (IDbContextFactory).
        var contables    = LookupService.GetStatusContableOpcionesAsync();
        var cuentasPagar = LookupService.GetTramitadoresCuentasPagarAsync();
        var liquidaciones = LookupService.GetTramitadoresLiquidacionesAsync();
        await Task.WhenAll(contables, cuentasPagar, liquidaciones);

        _statusContableOpciones    = contables.Result;
        _tramitadoresCuentasPagar  = cuentasPagar.Result;
        _tramitadoresLiquidaciones = liquidaciones.Result;
    }

    protected override async Task<List<StatusContabilidadViewModel>> ObtenerDatosAsync() =>
        (await StatusContabilidadService.GetAllAsync()).ToList();

    protected override void PrepararParaGuardar(StatusContabilidadViewModel item)
    {
        item.StatusContableNombre = _statusContableOpciones.FirstOrDefault(x => x.Id == item.StatusContableOpcionId)?.Nombre;
        item.TramitadorCuentasPagarNombre = _tramitadoresCuentasPagar.FirstOrDefault(x => x.Id == item.TramitadorCuentasPagarOpcionId)?.Nombre;
        item.TramitadorLiquidacionesNombre = _tramitadoresLiquidaciones.FirstOrDefault(x => x.Id == item.TramitadorLiquidacionesOpcionId)?.Nombre;
    }

    // El tablero es de solo edición: las filas nacen del ledger de devengados, no se crean ni borran acá.
    protected override Task ActualizarAsync(StatusContabilidadViewModel item) =>
        StatusContabilidadService.UpsertAsync(item);
}