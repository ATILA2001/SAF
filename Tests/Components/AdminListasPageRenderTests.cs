using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.AdminListas;
using SAF.Application.AdminListas.Dtos;
using SAF.Services.Abstractions;
using SAF.Shared;
using Tests.TestSupport;
using AdminListasPage = SAF.Components.Pages.AdminListas.AdminListas;

namespace Tests.Components;

/// <summary>
/// Regresión de render de la página de administración de listas: selector de lista
/// en la toolbar + grilla editable. Mismo criterio que CafPageRenderTests.
/// </summary>
[TestClass]
public class AdminListasPageRenderTests
{
    [TestMethod]
    public void RenderizaLaGrillaConSusFilas()
    {
        using var ctx = new Bunit.TestContext();
        ctx.AddPermissivePermissions();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var filas = new List<OpcionListaViewModel>
        {
            new() { Id = 1, Nombre = "PRUEBA-OPCION", Orden = 1, Activo = true },
            new() { Id = 2, Nombre = "PRUEBA-OPCION-2", Orden = 2, Activo = false },
        };
        var servicio = new Mock<IListaAdminService>();
        servicio.Setup(s => s.GetAllAsync(ListasAdmin.StatusDgayf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(filas);

        ctx.Services.AddSingleton(servicio.Object);
        ctx.Services.AddSingleton(Mock.Of<INotificationHelper>());
        ctx.Services.AddSingleton(Mock.Of<IExportService>());
        ctx.Services.AddSingleton(Mock.Of<IAuditoriaService>());
        ctx.Services.AddSingleton(sp => new Radzen.DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        var cut = ctx.RenderComponent<AdminListasPage>();

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Markup, "rz-data-grid");     // la grilla existe
            StringAssert.Contains(cut.Markup, "Orden");            // encabezados presentes
            StringAssert.Contains(cut.Markup, "PRUEBA-OPCION");    // y las filas también
        }, timeout: TimeSpan.FromSeconds(5));
    }
}
