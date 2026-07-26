using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Components.Shared;
using SAF.Data.Entities;
using SAF.Services.Abstractions;

namespace Tests.Components;

[TestClass]
public class HistorialAuditoriaTests
{
    [TestMethod]
    public void AgrupaPorLote_YMuestraLosCambios()
    {
        using var ctx = new Bunit.TestContext();

        var lote1 = Guid.NewGuid();
        var lote2 = Guid.NewGuid();
        var filas = new List<CambioAuditoria>
        {
            new() { Lote = lote1, Fecha = new DateTime(2026, 7, 26, 14, 32, 0, DateTimeKind.Utc),
                    Usuario = "ncarracedo", Accion = "Edición",
                    Campo = "STATUS DGAyF", ValorAnterior = "frenar", ValorNuevo = "avanzar CAF" },
            new() { Lote = lote1, Fecha = new DateTime(2026, 7, 26, 14, 32, 0, DateTimeKind.Utc),
                    Usuario = "ncarracedo", Accion = "Edición",
                    Campo = "OBSERVACIONES", ValorAnterior = null, ValorNuevo = "espera póliza" },
            new() { Lote = lote2, Fecha = new DateTime(2026, 7, 24, 11, 20, 0, DateTimeKind.Utc),
                    Usuario = "mgarcia", Accion = "Alta",
                    Campo = "TIPO DEV", ValorAnterior = null, ValorNuevo = "PRD" },
        };

        var servicio = new Mock<IAuditoriaService>();
        servicio.Setup(s => s.GetHistorialAsync("/pagos", 1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(filas);
        ctx.Services.AddSingleton(servicio.Object);

        var cut = ctx.RenderComponent<HistorialAuditoria>(p => p
            .Add(c => c.Vista, "/pagos")
            .Add(c => c.EntidadId, 1)
            .Add(c => c.ClaveNegocio, "Devengado PRD 5"));

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Markup, "ncarracedo");
            StringAssert.Contains(cut.Markup, "STATUS DGAyF");
            StringAssert.Contains(cut.Markup, "frenar");
            StringAssert.Contains(cut.Markup, "avanzar CAF");
            StringAssert.Contains(cut.Markup, "(vacío)");     // el alta muestra antes vacío
            StringAssert.Contains(cut.Markup, "mgarcia");
        });
    }

    [TestMethod]
    public void SinCambios_MuestraElMensajeVacio()
    {
        using var ctx = new Bunit.TestContext();
        var servicio = new Mock<IAuditoriaService>();
        servicio.Setup(s => s.GetHistorialAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CambioAuditoria>());
        ctx.Services.AddSingleton(servicio.Object);

        var cut = ctx.RenderComponent<HistorialAuditoria>(p => p.Add(c => c.Vista, "/caf"));

        cut.WaitForAssertion(() => StringAssert.Contains(cut.Markup, "Sin cambios registrados"));
    }
}
