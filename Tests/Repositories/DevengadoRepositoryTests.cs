using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Implementations;

namespace Tests.Repositories;

/// <summary>
/// El alta manual persiste la fila del ledger y su registro de datos manuales en un
/// solo SaveChanges: el extra llega con DevengadoId 0 (la fila todavía no existe) y la
/// FK tiene que resolverse por la navegación, o el registro quedaría huérfano o el
/// INSERT fallaría por la FK.
/// </summary>
[TestClass]
public class DevengadoRepositoryTests
{
    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    [TestMethod]
    public async Task ElAltaGuardaLaFilaYSuExtraEnlazados()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var repo = new DevengadoRepository(new Factory(options));

        var fila = new Devengado
        {
            TipoDev = "PRD", NroDev = 77, FechaImputacion = new DateTime(2026, 9, 25),
            Expediente = "01212221/26", ImportePp = 1000m,
        };
        var extra = new DevengadoExtra { TipoDev = "PRD", NroDev = 77, StatusDgayfOpcionId = 1 };

        await repo.AddAsync(fila, extra);

        Assert.AreNotEqual(0, fila.Id, "la fila recibió su Id");
        Assert.AreEqual(fila.Id, extra.DevengadoId, "el extra apunta a la fila recién creada");

        await using var db = new AppDbContext(options);
        var guardado = await db.DevengadosExtra.SingleAsync();
        Assert.AreEqual(fila.Id, guardado.DevengadoId);
        Assert.AreEqual(1, guardado.StatusDgayfOpcionId);
    }
}
