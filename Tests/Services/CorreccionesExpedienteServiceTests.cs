using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAF.Application.Pagos;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// Aplicar correcciones de expediente corre después de una confirmación del usuario
/// basada en un diff que puede haber envejecido: cada fila se corrige solo si sigue
/// diciendo lo que el diálogo mostró. Sin esa guarda, una edición o baja concurrente
/// quedaría pisada en silencio con un valor que el usuario nunca vio.
/// </summary>
[TestClass]
public class CorreccionesExpedienteServiceTests
{
    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private static DbContextOptions<AppDbContext> OpcionesUnicas() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [TestMethod]
    public async Task AplicaSoloLasFilasQueSiguenComoSeMostraron()
    {
        var options = OpcionesUnicas();
        await using (var db = new AppDbContext(options))
        {
            db.Devengados.AddRange(
                new Devengado { Id = 1, TipoDev = "PRD", NroDev = 10, Expediente = "11111111/26" },
                new Devengado { Id = 2, TipoDev = "PRD", NroDev = 20, Expediente = "22222222/26" });
            await db.SaveChangesAsync();
        }

        var service = new DevengadoSyncService(
            Mock.Of<IDbContextFactory<IvcDbContext>>(),
            new Factory(options),
            NullLogger<DevengadoSyncService>.Instance);

        // La segunda corrección quedó vieja: la fila 2 ya no dice lo que vio el usuario.
        var aplicadas = await service.AplicarCorreccionesExpedienteAsync(
        [
            new CorreccionExpediente(1, "PRD", 10, null, null, "11111111/26", "99999999/26"),
            new CorreccionExpediente(2, "PRD", 20, null, null, "00000000/00", "88888888/26"),
        ]);

        Assert.AreEqual(1, aplicadas.Count);
        Assert.AreEqual(1, aplicadas[0].DevengadoId);

        await using var verificacion = new AppDbContext(options);
        Assert.AreEqual("99999999/26",
            (await verificacion.Devengados.SingleAsync(d => d.Id == 1)).Expediente);
        Assert.AreEqual("22222222/26",
            (await verificacion.Devengados.SingleAsync(d => d.Id == 2)).Expediente,
            "la fila que cambió en el medio no debía tocarse");
    }

    [TestMethod]
    public async Task UnaFilaBorradaEnElMedioSeSalteaSinError()
    {
        var options = OpcionesUnicas();
        await using (var db = new AppDbContext(options))
        {
            db.Devengados.Add(new Devengado { Id = 1, TipoDev = "PRD", NroDev = 10, Expediente = "11111111/26" });
            await db.SaveChangesAsync();
        }

        var service = new DevengadoSyncService(
            Mock.Of<IDbContextFactory<IvcDbContext>>(),
            new Factory(options),
            NullLogger<DevengadoSyncService>.Instance);

        var aplicadas = await service.AplicarCorreccionesExpedienteAsync(
        [
            new CorreccionExpediente(1, "PRD", 10, null, null, "11111111/26", "99999999/26"),
            new CorreccionExpediente(7, "DGG", 99, null, null, "33333333/26", "44444444/26"),
        ]);

        Assert.AreEqual(1, aplicadas.Count);
        Assert.AreEqual(1, aplicadas[0].DevengadoId);
    }

    [TestMethod]
    public async Task SinCorreccionesNoTocaLaBase()
    {
        var service = new DevengadoSyncService(
            Mock.Of<IDbContextFactory<IvcDbContext>>(),
            Mock.Of<IDbContextFactory<AppDbContext>>(), // explota si se usa: no debe usarse
            NullLogger<DevengadoSyncService>.Instance);

        var aplicadas = await service.AplicarCorreccionesExpedienteAsync([]);

        Assert.AreEqual(0, aplicadas.Count);
    }
}
