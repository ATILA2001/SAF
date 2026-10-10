using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Services.Abstractions;
using SAF.Shared;
using Tests.TestSupport;
using ControlOpPage = SAF.Components.Pages.ControlOp.ControlOp;

namespace Tests.Components;

/// <summary>
/// Regresión de render de Control OP: dos grillas en pestañas sobre una sola página de
/// GridPageBase. La lista de control y las correcciones tienen que renderizarse las dos
/// (modo Client), con sus filas, y el botón de buscar OP solo con permiso de alta.
/// </summary>
[TestClass]
public class ControlOpPageRenderTests
{
    [TestMethod]
    public void RenderizaLasDosPestanasConSusFilas()
    {
        using var ctx = new Bunit.TestContext();
        ctx.AddPermissivePermissions();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var servicio = new Mock<IControlOpService>();
        servicio.Setup(s => s.GetListaControlAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ControlOpViewModel>
            {
                new() { DevengadoId = 1, TipoDev = "PRD", NroDev = 10, Expediente = "01212221/26", Empresa = "EMPRESA-PENDIENTE", Importe = 1000m, FechaFirmaOp = new DateTime(2026, 9, 1), StatusDgayf = "avanzar" },
            });
        servicio.Setup(s => s.GetCorreccionesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CorreccionOpViewModel>
            {
                new() { Id = 1, NroDocResp = 40076624, Op = "C41-356190-2026", ExpedienteOriginal = "40076624/26", ExpedienteCorregido = "01212221/26", Beneficiario = "BENEF-CORREGIDA" },
            });

        ctx.Services.AddSingleton(servicio.Object);
        ctx.Services.AddSingleton(Mock.Of<INotificationHelper>());
        ctx.Services.AddSingleton(Mock.Of<IExportService>());
        ctx.Services.AddSingleton(Mock.Of<IAuditoriaService>());
        ctx.Services.AddSingleton(sp => new Radzen.DialogService(
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));

        var cut = ctx.RenderComponent<ControlOpPage>();

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Markup, "Sin OP relacionada (1)");
            StringAssert.Contains(cut.Markup, "Correcciones (1)");
            StringAssert.Contains(cut.Markup, "EMPRESA-PENDIENTE");   // fila de la lista de control
            StringAssert.Contains(cut.Markup, "Buscar OP");           // acción con permiso de alta
            StringAssert.Contains(cut.Markup, "C41-356190-2026");     // fila de correcciones
            StringAssert.Contains(cut.Markup, "BENEF-CORREGIDA");
        }, timeout: TimeSpan.FromSeconds(5));

        // La lista se pide con el corte por defecto: hoy menos 3 días.
        servicio.Verify(s => s.GetListaControlAsync(ReglasControlOp.FechaCorte(DateTime.Today), It.IsAny<CancellationToken>()), Times.Once);
    }
}
