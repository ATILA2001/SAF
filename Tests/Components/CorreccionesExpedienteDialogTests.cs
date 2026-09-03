using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Pagos;
using SAF.Components.Pages.Pagos;

namespace Tests.Components;

/// <summary>
/// Regresión de render del diálogo de correcciones de expediente: el diff que el
/// usuario confirma tiene que mostrar el valor viejo y el nuevo de cada fila, y el
/// selector por fila existe porque el error también puede estar en IVC — destildar
/// una fila es cómo se conserva el valor de SAF.
/// </summary>
[TestClass]
public class CorreccionesExpedienteDialogTests
{
    private static readonly List<CorreccionExpediente> Correcciones =
    [
        new(1, "PRD", 10, new DateTime(2026, 3, 1), 1000m, "11111111/26", "99999999/26"),
        new(2, "DGG", 20, new DateTime(2026, 3, 2), 500m, null, "88888888/26"),
    ];

    private static IRenderedComponent<CorreccionesExpedienteDialog> Render(Bunit.TestContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(sp => new Radzen.DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        return ctx.RenderComponent<CorreccionesExpedienteDialog>(parameters => parameters
            .Add(p => p.Correcciones, Correcciones));
    }

    [TestMethod]
    public void MuestraElDiffCompletoDeCadaFila()
    {
        using var ctx = new Bunit.TestContext();
        var cut = Render(ctx);

        StringAssert.Contains(cut.Markup, "PRD 10");
        StringAssert.Contains(cut.Markup, "11111111/26");   // valor viejo
        StringAssert.Contains(cut.Markup, "99999999/26");   // valor corregido
        StringAssert.Contains(cut.Markup, "(vacío)");       // expediente faltante en SAF
        StringAssert.Contains(cut.Markup, "88888888/26");
    }

    [TestMethod]
    public void TodasTildadasPorDefectoYDestildarAchicaElLote()
    {
        using var ctx = new Bunit.TestContext();
        var cut = Render(ctx);

        // Selector de encabezado + uno por fila, todos tildados (aceptar IVC es el default).
        var checkboxes = cut.FindAll("input[type=checkbox]");
        Assert.AreEqual(Correcciones.Count + 1, checkboxes.Count);
        StringAssert.Contains(cut.Markup, "Aplicar 2 correcciones");

        // Destildar una fila: el botón refleja el lote real que se va a aplicar.
        cut.FindAll("input[type=checkbox]")[1].Change(false);
        StringAssert.Contains(cut.Markup, "Aplicar 1 corrección");
    }
}
