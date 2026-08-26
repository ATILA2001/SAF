using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Radzen;
using SAF.Application.Caf.Dtos;
using SAF.Services.Abstractions;
using SAF.Shared;
using Tests.TestSupport;
using CafPage = SAF.Components.Pages.Caf.Caf;

namespace Tests.Components;

/// <summary>
/// Guardar o borrar una fila no puede recargar la vista entera: eso rearmaba la lista
/// y dejaba la grilla en el primer registro, así que el usuario perdía el scroll y
/// tenía que volver a buscar el expediente que acababa de tocar. La fila se relee sola
/// y se refresca (o se quita) en su lugar — salvo mientras la carga progresiva sigue
/// en vuelo, donde recargar es el único camino coherente.
/// </summary>
[TestClass]
public class GridRefrescoFilaTests
{
    /// <summary>Expone lo que la página hereda protegido, para operar sin pasar por la grilla.</summary>
    private sealed class CafPageProbe : CafPage
    {
        public Task<bool> Guardar(CafViewModel item) => GuardarDesdeDialogoAsync(item);
        public Task Eliminar(CafViewModel item) => DeleteRow(item);
        public List<CafViewModel> Filas => _items;
        public bool CargandoResto => _cargandoResto;
    }

    /// <summary>
    /// Confirma sin abrir diálogo: DeleteRow espera la confirmación del usuario y en
    /// bUnit no hay nadie que la cierre.
    /// </summary>
    private sealed class DialogServiceQueConfirma(NavigationManager nav, Microsoft.JSInterop.IJSRuntime js)
        : DialogService(nav, js)
    {
        public override Task<bool?> Confirm(string message = "Confirm?", string title = "Confirm",
            ConfirmOptions? options = null, CancellationToken? cancellationToken = null)
            => Task.FromResult<bool?>(true);
    }

    private static Mock<ICafService> ArmarContexto(Bunit.TestContext ctx, List<CafViewModel> filas,
        bool confirmarDialogos = false)
    {
        ctx.AddPermissivePermissions();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var caf = new Mock<ICafService>();
        caf.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(filas);
        caf.Setup(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(filas);

        ctx.Services.AddSingleton(caf.Object);
        ctx.Services.AddSingleton(Mock.Of<INotificationHelper>());
        ctx.Services.AddSingleton(Mock.Of<IExportService>());
        ctx.Services.AddSingleton(Mock.Of<IAuditoriaService>());
        ctx.Services.AddSingleton<DialogService>(sp => confirmarDialogos
            ? new DialogServiceQueConfirma(
                sp.GetRequiredService<NavigationManager>(),
                sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>())
            : new DialogService(
                sp.GetRequiredService<NavigationManager>(),
                sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        return caf;
    }

    [TestMethod]
    public async Task GuardarRefrescaSoloLaFilaYNoRecargaLaGrilla()
    {
        using var ctx = new Bunit.TestContext();
        var filas = new List<CafViewModel>
        {
            new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "BENEF-1" },
            new() { Id = 2, Anio = 2026, Expediente = "01212222/26", Beneficiario = "BENEF-2" },
        };
        var caf = ArmarContexto(ctx, filas);

        // La fila releída trae lo que quedó guardado (y una versión nueva).
        caf.Setup(s => s.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CafViewModel
            {
                Id = 2,
                Anio = 2026,
                Expediente = "01212222/26",
                Beneficiario = "BENEF-2-EDITADO",
                RowVersion = [9, 9],
            });

        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "BENEF-2"),
            timeout: TimeSpan.FromSeconds(5));

        var segunda = cut.Instance.Filas[1];
        var guardado = await cut.InvokeAsync(() => cut.Instance.Guardar(segunda));

        Assert.IsTrue(guardado);
        caf.Verify(s => s.UpdateAsync(It.IsAny<CafViewModel>(), It.IsAny<CancellationToken>()), Times.Once);

        // La relectura es de a una fila: la carga de la vista no se repite.
        caf.Verify(s => s.GetByIdAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        caf.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        caf.Verify(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once, "la grilla se recargó entera en vez de refrescar la fila guardada");

        // Misma lista, misma posición y misma instancia: la grilla no se mueve.
        Assert.AreEqual(2, cut.Instance.Filas.Count);
        Assert.AreSame(segunda, cut.Instance.Filas[1]);

        // Y la fila quedó con lo releído (incluida la versión, que la próxima edición necesita).
        Assert.AreEqual("BENEF-2-EDITADO", segunda.Beneficiario);
        CollectionAssert.AreEqual(new byte[] { 9, 9 }, segunda.RowVersion);
    }

    [TestMethod]
    public async Task SinFilaReleidaSeRecargaLaVista()
    {
        using var ctx = new Bunit.TestContext();
        var filas = new List<CafViewModel>
        {
            new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "BENEF-1" },
        };
        var caf = ArmarContexto(ctx, filas);

        // Otro usuario la borró mientras se editaba: no hay fila que refrescar.
        caf.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CafViewModel?)null);

        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "BENEF-1"),
            timeout: TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => cut.Instance.Guardar(cut.Instance.Filas[0]));

        // Dos cargas: la inicial y la recarga de respaldo.
        caf.Verify(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task RelecturaFallidaNoSeReportaComoFalloDeGuardado()
    {
        using var ctx = new Bunit.TestContext();
        var filas = new List<CafViewModel>
        {
            new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "BENEF-1" },
        };
        var caf = ArmarContexto(ctx, filas);

        // El guardado persiste pero la relectura revienta (falla transitoria de la base).
        caf.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("base caída"));

        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "BENEF-1"),
            timeout: TimeSpan.FromSeconds(5));

        var guardado = await cut.InvokeAsync(() => cut.Instance.Guardar(cut.Instance.Filas[0]));

        // El guardado fue exitoso y así debe reportarse (el diálogo tiene que cerrarse);
        // la falla de la relectura se resuelve con la recarga de respaldo.
        Assert.IsTrue(guardado, "una falla releyendo la fila se reportó como fallo del guardado");
        caf.Verify(s => s.UpdateAsync(It.IsAny<CafViewModel>(), It.IsAny<CancellationToken>()), Times.Once);
        caf.Verify(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task EliminarSinCargaDeFondoSacaLaFilaEnElLugar()
    {
        using var ctx = new Bunit.TestContext();
        var filas = new List<CafViewModel>
        {
            new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "BENEF-1", RowVersion = [1] },
            new() { Id = 2, Anio = 2026, Expediente = "01212222/26", Beneficiario = "BENEF-2", RowVersion = [2] },
        };
        var caf = ArmarContexto(ctx, filas, confirmarDialogos: true);

        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "BENEF-2"),
            timeout: TimeSpan.FromSeconds(5));

        var segunda = cut.Instance.Filas[1];
        await cut.InvokeAsync(() => cut.Instance.Eliminar(segunda));

        caf.Verify(s => s.DeleteAsync(2, It.IsAny<byte[]?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Sin recarga (una sola carga: la inicial) y la fila ya no está en la lista.
        caf.Verify(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once, "el borrado recargó la vista entera en vez de sacar la fila en el lugar");
        Assert.AreEqual(1, cut.Instance.Filas.Count);
        Assert.AreEqual(1, cut.Instance.Filas[0].Id);
    }

    [TestMethod]
    public async Task EliminarConCargaDeFondoEnVueloRecargaLaVista()
    {
        using var ctx = new Bunit.TestContext();

        // Primer lote lleno (100 = TamanoPrimerLote): la página cree que faltan lotes y
        // deja la carga de fondo en vuelo; el lote siguiente nunca responde, así que
        // _cargandoResto queda estable en true durante el test.
        var primerLote = Enumerable.Range(1, 100)
            .Select(i => new CafViewModel
            {
                Id = i,
                Anio = 2026,
                Expediente = $"{i:00000000}/26",
                Beneficiario = $"BENEF-{i}",
                RowVersion = [1],
            })
            .ToList();

        var caf = ArmarContexto(ctx, primerLote, confirmarDialogos: true);
        caf.Setup(s => s.GetPageAsync(0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(primerLote);
        caf.Setup(s => s.GetPageAsync(It.Is<int>(skip => skip > 0), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IReadOnlyList<CafViewModel>>().Task);

        var cut = ctx.RenderComponent<CafPageProbe>();
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Instance.CargandoResto),
            timeout: TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => cut.Instance.Eliminar(cut.Instance.Filas[0]));

        caf.Verify(s => s.DeleteAsync(1, It.IsAny<byte[]?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Con lotes en vuelo el atajo in-place saltearía filas (el DELETE corre el
        // Skip/Take del runner): tiene que recargar desde cero — dos cargas del lote 0.
        caf.Verify(s => s.GetPageAsync(0, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2), "el borrado con carga de fondo en vuelo no recargó la vista");
    }
}
