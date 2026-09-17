using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Radzen;
using Radzen.Blazor;
using SAF.Application.Caf.Dtos;
using SAF.Services.Abstractions;
using SAF.Shared;
using Tests.TestSupport;
using CafPage = SAF.Components.Pages.Caf.Caf;

namespace Tests.Components;

/// <summary>
/// Cambiar de vista destruye la página y con ella la grilla, así que los filtros de
/// columna y la búsqueda rápida se perdían al volver y había que rearmarlos cada vez.
/// El estado vive ahora en un servicio de la sesión (EstadoGrillas) y la página nueva
/// lo repone al crearse; Quitar filtros lo limpia también ahí. Acá "cambiar de vista"
/// se simula destruyendo los componentes y volviendo a renderizar sobre los mismos
/// servicios, que es exactamente lo que hace el router dentro de un circuito.
/// </summary>
[TestClass]
public class GridFiltrosPersistenTests
{
    /// <summary>Expone lo que la página hereda protegido, para operar sin pasar por la UI.</summary>
    private sealed class CafPageProbe : CafPage
    {
        public RadzenDataGrid<CafViewModel> Grilla => _grid;
        public string TextoBusqueda => _textoBusqueda;
        public bool FiltrosActivos => HayFiltrosActivos;
        public Task Buscar(string texto) => OnBusquedaChanged(texto);
        public Task Limpiar() => LimpiarFiltros();
    }

    private static List<CafViewModel> Filas() =>
    [
        new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "BENEF-1" },
        new() { Id = 2, Anio = 2026, Expediente = "01212222/26", Beneficiario = "BENEF-2" },
    ];

    private static void ArmarContexto(Bunit.TestContext ctx)
    {
        ctx.AddPermissivePermissions();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var caf = new Mock<ICafService>();
        caf.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Filas);
        caf.Setup(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Filas);

        ctx.Services.AddSingleton(caf.Object);
        ctx.Services.AddSingleton(Mock.Of<INotificationHelper>());
        ctx.Services.AddSingleton(Mock.Of<IExportService>());
        ctx.Services.AddSingleton(Mock.Of<IAuditoriaService>());
        ctx.Services.AddSingleton(sp => new DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
    }

    private static IRenderedComponent<CafPageProbe> Renderizar(Bunit.TestContext ctx)
    {
        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "rz-data-grid"),
            timeout: TimeSpan.FromSeconds(5));
        return cut;
    }

    /// <summary>Filtra Beneficiario como lo haría el popup de la columna (aplicar + guardar settings).</summary>
    private static Task FiltrarBeneficiario(IRenderedComponent<CafPageProbe> cut, string valor) =>
        cut.InvokeAsync(async () =>
        {
            var grilla = cut.Instance.Grilla;
            var columna = grilla.ColumnsCollection.First(c => c.Property == nameof(CafViewModel.Beneficiario));
            columna.SetFilterOperator(FilterOperator.Contains);
            columna.SetFilterValue(valor);
            await grilla.ApplyFilter(columna, closePopup: false);
        });

    [TestMethod]
    public async Task ElFiltroDeColumnaSobreviveAlVolverALaVista()
    {
        using var ctx = new Bunit.TestContext();
        ArmarContexto(ctx);

        var primera = Renderizar(ctx);
        await FiltrarBeneficiario(primera, "BENEF-2");
        primera.WaitForAssertion(() => Assert.IsFalse(primera.Markup.Contains("BENEF-1"), "el filtro no se aplicó"));

        // Ir a otra vista destruye la página; volver crea otra, sobre los mismos servicios.
        ctx.DisposeComponents();
        var segunda = Renderizar(ctx);

        segunda.WaitForAssertion(() =>
        {
            StringAssert.Contains(segunda.Markup, "BENEF-2");
            Assert.IsFalse(segunda.Markup.Contains("BENEF-1"), "la fila filtrada volvió a aparecer");
            Assert.IsTrue(segunda.Instance.FiltrosActivos, "Quitar filtros no refleja el filtro repuesto");
            // La toolbar se pinta antes de que la grilla reponga los settings: el botón
            // tiene que terminar habilitado en el markup, no solo en la propiedad.
            StringAssert.Contains(segunda.Markup, "Quitar todos los filtros", "el botón quedó deshabilitado en pantalla");
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task LaBusquedaRapidaSobreviveAlVolverALaVista()
    {
        using var ctx = new Bunit.TestContext();
        ArmarContexto(ctx);

        var primera = Renderizar(ctx);
        await primera.InvokeAsync(() => primera.Instance.Buscar("BENEF-2"));

        ctx.DisposeComponents();
        var segunda = Renderizar(ctx);

        Assert.AreEqual("BENEF-2", segunda.Instance.TextoBusqueda);
        segunda.WaitForAssertion(() =>
        {
            StringAssert.Contains(segunda.Markup, "BENEF-2");
            Assert.IsFalse(segunda.Markup.Contains("BENEF-1"), "la búsqueda repuesta no filtró la primera pintada");
            StringAssert.Contains(segunda.Markup, "value=\"BENEF-2\"", "el cuadro de búsqueda no muestra el texto repuesto");
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task QuitarFiltrosTambienBorraLoGuardado()
    {
        using var ctx = new Bunit.TestContext();
        ArmarContexto(ctx);

        var primera = Renderizar(ctx);
        await FiltrarBeneficiario(primera, "BENEF-2");
        await primera.InvokeAsync(() => primera.Instance.Buscar("BENEF-2"));
        await primera.InvokeAsync(() => primera.Instance.Limpiar());

        ctx.DisposeComponents();
        var segunda = Renderizar(ctx);

        segunda.WaitForAssertion(() =>
        {
            StringAssert.Contains(segunda.Markup, "BENEF-1");
            StringAssert.Contains(segunda.Markup, "BENEF-2");
            Assert.IsFalse(segunda.Instance.FiltrosActivos, "volvió un filtro que se había quitado");
            StringAssert.Contains(segunda.Markup, "No hay filtros aplicados", "el botón Quitar filtros sigue habilitado");
        }, timeout: TimeSpan.FromSeconds(5));
        Assert.AreEqual(string.Empty, segunda.Instance.TextoBusqueda);
    }

    [TestMethod]
    public void CadaVistaTieneSuPropioEstado()
    {
        var estados = new EstadoGrillas();

        var pagos = estados.De("/pagos");
        pagos.TextoBusqueda = "algo";

        Assert.AreSame(pagos, estados.De("/pagos"), "la misma clave tiene que devolver el mismo estado");
        Assert.AreEqual(string.Empty, estados.De("/seguros").TextoBusqueda, "el estado de una vista se filtró a otra");
    }
}
