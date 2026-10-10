using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Components.Pages.ControlOp;
using SAF.Services.Abstractions;

namespace Tests.Components;

/// <summary>
/// El diálogo de búsqueda replica cómo se buscaba la OP en la consulta de SIGAF del
/// Excel: arranca solo con la empresa, la fecha de firma y el importe del devengado, y
/// muestra las señales que acortan el análisis (importe igual o parcial, expediente sin
/// devengado, ya corregida).
/// </summary>
[TestClass]
public class BuscarOpDialogTests
{
    private static readonly ControlOpViewModel Fila = new()
    {
        DevengadoId = 1, TipoDev = "PRD", NroDev = 10, Expediente = "01212221/26",
        Empresa = "EMPRESA X", Importe = 1000m, FechaFirmaOp = new DateTime(2026, 9, 1),
    };

    [TestMethod]
    public void BuscaSolaConLosDatosDelDevengadoYMuestraLasSenales()
    {
        using var ctx = new Bunit.TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        CriteriosBusquedaOp? recibidos = null;
        var servicio = new Mock<IControlOpService>();
        servicio.Setup(s => s.BuscarCandidatasAsync(It.IsAny<CriteriosBusquedaOp>(), It.IsAny<CancellationToken>()))
            .Callback<CriteriosBusquedaOp, CancellationToken>((c, _) => recibidos = c)
            .ReturnsAsync(new List<CandidataOpViewModel>
            {
                new() { Op = "C41-1-2026", NroDocResp = 1, ExpedienteSigaf = "01212221/25", Beneficiario = "EMPRESA X", ImporteOp = 1000m, FechaPago = new DateTime(2026, 9, 1), Coincidencia = CoincidenciaImporte.Igual, ExpedienteSinDevengado = true },
                new() { Op = "C41-2-2026", NroDocResp = 2, ExpedienteSigaf = "00000002/26", Beneficiario = "EMPRESA X", ImporteOp = 400m, Coincidencia = CoincidenciaImporte.Parcial, CorregidaEnSafA = "00000009/26" },
            });

        ctx.Services.AddSingleton(servicio.Object);
        ctx.Services.AddSingleton(sp => new Radzen.DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        var cut = ctx.RenderComponent<BuscarOpDialog>(p => p.Add(x => x.Fila, Fila));

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Markup, "C41-1-2026");
            StringAssert.Contains(cut.Markup, "sin devengado");
            StringAssert.Contains(cut.Markup, "igual");
            StringAssert.Contains(cut.Markup, "parcial");
            StringAssert.Contains(cut.Markup, "ya corregida → 00000009/26");

            // La primera, con importe igual, se destaca y explica el porqué, incluida la
            // diferencia con el expediente del devengado (01212221/26), en palabras.
            StringAssert.Contains(cut.Markup, "Mejor coincidencia");
            StringAssert.Contains(cut.Markup, "Mismo número, distinto año");
            StringAssert.Contains(cut.Markup, "mismo beneficiario");
            StringAssert.Contains(cut.Markup, "pagada el día de la firma");

            // El panel de identidad muestra el devengado buscado.
            StringAssert.Contains(cut.Markup, "PRD 10");
            StringAssert.Contains(cut.Markup, "OP firmada · 01/09/2026");
        }, timeout: TimeSpan.FromSeconds(5));

        Assert.IsNotNull(recibidos);
        Assert.AreEqual("EMPRESA X", recibidos.Beneficiario);
        Assert.AreEqual(new DateTime(2026, 9, 1), recibidos.FechaPagoDesde);
        Assert.IsNull(recibidos.FechaPagoHasta);
        Assert.AreEqual(1000m, recibidos.ImporteReferencia);

        // La OP ya corregida no se puede volver a asignar desde acá.
        var botones = cut.FindAll("button").Where(b => b.TextContent.Contains("Asignar")).ToList();
        Assert.AreEqual(2, botones.Count);
        Assert.IsFalse(botones[0].HasAttribute("disabled"));
        Assert.IsTrue(botones[1].HasAttribute("disabled"));
    }
}
