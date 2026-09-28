using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Services.Abstractions;
using SAF.Services.Implementations;

namespace Tests.Services;

/// <summary>
/// Toda fila que la sync importa nace con su registro de datos manuales y Status DGAyF
/// "avanzar" (el estado inicial del circuito, que en la planilla se tipeaba a mano).
/// Solo las nuevas: las filas que ya estaban en SAF no se tocan, tengan o no status.
/// Y si la opción no existe (un admin la desactivó desde Listas), la sync importa igual.
/// </summary>
[TestClass]
public class DevengadoSyncServiceTests
{
    private static readonly DateTime Hoy = new(2026, 9, 25);
    private static readonly DateTime Ayer = new(2026, 9, 24);

    /// <summary>Fila con clave que respalda a DEVENGADOS en memoria (la real es keyless y no se puede Add).</summary>
    public sealed class FilaIvc
    {
        public int Id { get; set; }
        public string TipoDev { get; set; } = string.Empty;
        public int NroDev { get; set; }
        public DateTime? FechaImputacion { get; set; }
        public string? EeFinanciera { get; set; }
        public string? Descripcion { get; set; }
        public decimal? ImportePp { get; set; }
    }

    /// <summary>
    /// IVC en memoria: DEVENGADOS se sirve como consulta definida que proyecta desde la
    /// tabla auxiliar con clave, que es lo que el proveedor en memoria sabe traducir.
    /// </summary>
    private sealed class IvcEnMemoria(DbContextOptions<IvcDbContext> options) : IvcDbContext(options)
    {
        public DbSet<FilaIvc> Filas => Set<FilaIvc>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<FilaIvc>().HasKey(f => f.Id);
            modelBuilder.Entity<IvcDevengado>().ToInMemoryQuery(() => Filas.Select(f => new IvcDevengado
            {
                TipoDev = f.TipoDev,
                NroDev = f.NroDev,
                FechaImputacion = f.FechaImputacion,
                EeFinanciera = f.EeFinanciera,
                Descripcion = f.Descripcion,
                ImportePp = f.ImportePp,
            }));
        }
    }

    private sealed class IvcFactory(DbContextOptions<IvcDbContext> options) : IDbContextFactory<IvcDbContext>
    {
        public IvcDbContext CreateDbContext() => new IvcEnMemoria(options);
    }

    private sealed class SafFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private static DbContextOptions<AppDbContext> OpcionesSaf() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static DbContextOptions<IvcDbContext> OpcionesIvc() =>
        new DbContextOptionsBuilder<IvcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static FilaIvc Ivc(string tipo, int nro, DateTime fecha, decimal importe) => new()
    {
        TipoDev = tipo, NroDev = nro, FechaImputacion = fecha, ImportePp = importe,
        EeFinanciera = "01212221/26", Descripcion = "EMPRESA X",
    };

    private static async Task CargarIvcAsync(DbContextOptions<IvcDbContext> options, params FilaIvc[] filas)
    {
        await using var ivc = new IvcEnMemoria(options);
        ivc.Filas.AddRange(filas);
        await ivc.SaveChangesAsync();
    }

    private static DevengadoSyncService Servicio(
        DbContextOptions<IvcDbContext> ivc, DbContextOptions<AppDbContext> saf) =>
        new(new IvcFactory(ivc), new SafFactory(saf), NullLogger<DevengadoSyncService>.Instance);

    [TestMethod]
    public async Task LasFilasNuevasNacenConStatusDgayfAvanzarYLasViejasNoSeTocan()
    {
        var saf = OpcionesSaf();
        await using (var db = new AppDbContext(saf))
        {
            db.StatusDgayfOpciones.Add(new StatusDgayfOpcion { Id = 1, Nombre = "avanzar", Orden = 1 });
            // Ya importada en una corrida anterior de hoy, sin datos manuales.
            db.Devengados.Add(new Devengado
            {
                Id = 1, TipoDev = "PRD", NroDev = 10, FechaImputacion = Hoy, ImportePp = 1000m, Expediente = "01212221/26",
            });
            await db.SaveChangesAsync();
        }

        var ivc = OpcionesIvc();
        await CargarIvcAsync(ivc,
            Ivc("PRD", 10, Hoy, 1000m),   // ya está en SAF: no se reimporta
            Ivc("PRD", 20, Hoy, 500m),    // nueva
            Ivc("C55", 30, Hoy, 700m),    // tipo excluido
            Ivc("PRD", 40, Ayer, 900m));  // día anterior: solo se importa la última fecha

        var resultado = await Servicio(ivc, saf).SyncAsync();

        Assert.AreEqual(SyncStatus.Importado, resultado.Status);
        Assert.AreEqual(1, resultado.Insertados);

        await using var verificacion = new AppDbContext(saf);
        var extras = await verificacion.DevengadosExtra.Include(e => e.Devengado).ToListAsync();

        var extra = extras.Single();
        Assert.AreEqual(20, extra.Devengado!.NroDev, "el extra es de la fila nueva");
        Assert.AreEqual("PRD", extra.TipoDev);
        Assert.AreEqual(20, extra.NroDev);
        Assert.AreEqual(1, extra.StatusDgayfOpcionId, "la fila nueva nace en \"avanzar\"");
        Assert.IsFalse(extras.Any(e => e.DevengadoId == 1), "la fila que ya estaba no recibe extra");
    }

    [TestMethod]
    public async Task SinLaOpcionAvanzarActivaImportaIgualConElStatusVacio()
    {
        var saf = OpcionesSaf();
        await using (var db = new AppDbContext(saf))
        {
            db.StatusDgayfOpciones.Add(new StatusDgayfOpcion { Id = 1, Nombre = "avanzar", Orden = 1, Activo = false });
            await db.SaveChangesAsync();
        }

        var ivc = OpcionesIvc();
        await CargarIvcAsync(ivc, Ivc("PRD", 20, Hoy, 500m));

        var resultado = await Servicio(ivc, saf).SyncAsync();

        Assert.AreEqual(SyncStatus.Importado, resultado.Status);
        Assert.AreEqual(1, resultado.Insertados);

        await using var verificacion = new AppDbContext(saf);
        var extra = await verificacion.DevengadosExtra.SingleAsync();
        Assert.IsNull(extra.StatusDgayfOpcionId, "sin opción activa, el status queda vacío pero la fila entra");
    }
}
