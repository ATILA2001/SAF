using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.Caf.Dtos;
using SAF.Services.Abstractions;
using SAF.Shared;
using Tests.TestSupport;
using CafPage = SAF.Components.Pages.Caf.Caf;

namespace Tests.Components;

/// <summary>
/// Regresión de render de una página de grilla completa (permisos + GridPageBase +
/// RadzenDataGrid virtualizado). Atrapa roturas de markup que compilan igual, como un
/// comentario Razor dentro del tag del componente desplazando las columnas.
/// </summary>
[TestClass]
public class CafPageRenderTests
{
    [TestMethod]
    public void RenderizaLaGrillaConSusFilas()
    {
        using var ctx = new Bunit.TestContext();
        ctx.AddPermissivePermissions();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var filas = new List<CafViewModel>
        {
            new() { Id = 1, Anio = 2026, Expediente = "01212221/26", Beneficiario = "PRUEBA-BENEF" },
            new() { Id = 2, Anio = 2026, Expediente = "01212222/26", Beneficiario = "PRUEBA-BENEF-2" },
        };
        var caf = new Mock<ICafService>();
        caf.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(filas);
        caf.Setup(s => s.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(filas);

        ctx.Services.AddSingleton(caf.Object);
        ctx.Services.AddSingleton(Mock.Of<INotificationHelper>());
        ctx.Services.AddSingleton(Mock.Of<IExportService>());
        ctx.Services.AddSingleton(Mock.Of<IAuditoriaService>());
        ctx.Services.AddSingleton(sp => new Radzen.DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        var cut = ctx.RenderComponent<CafPage>();

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Markup, "rz-data-grid");   // la grilla existe
            StringAssert.Contains(cut.Markup, "Beneficiario");   // encabezados presentes
            StringAssert.Contains(cut.Markup, "PRUEBA-BENEF");   // y las filas también
        }, timeout: TimeSpan.FromSeconds(5));
    }
}
