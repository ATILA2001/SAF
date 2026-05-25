#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data.Entities;
using System.Text.RegularExpressions;

namespace SAF.Data;

/// <summary>
/// DbContext principal del SAF. Contiene todas las tablas propias del sistema.
/// Aplica convención snake_case a los nombres de columna automáticamente.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DevengadoExtra> DevengadosExtra { get; set; }
    public DbSet<StatusContabilidadExtra> StatusContabilidadExtras { get; set; }
    public DbSet<StatusDgayfOpcion> StatusDgayfOpciones { get; set; }
    public DbSet<StatusOpOpcion> StatusOpOpciones { get; set; }
    public DbSet<StatusContableOpcion> StatusContableOpciones { get; set; }
    public DbSet<TramitadorCuentasPagarOpcion> TramitadoresCuentasPagar { get; set; }
    public DbSet<TramitadorLiquidacionesOpcion> TramitadoresLiquidaciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar convención snake_case a todas las columnas
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var columnName = Regex.Replace(property.Name, "([a-z])([A-Z])", "$1_$2").ToLower();
                property.SetColumnName(columnName);
            }
        }

        modelBuilder.Entity<DevengadoExtra>(entity =>
        {
            entity.HasIndex(e => new { e.TipoDev, e.NroDev }).IsUnique();
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Ccoo).HasMaxLength(200);
            entity.Property(e => e.StatusContable).HasMaxLength(100);
            entity.Property(e => e.SegurosTeso).HasMaxLength(200);
            entity.Property(e => e.BuzonSade).HasMaxLength(200);
        });

        modelBuilder.Entity<StatusContabilidadExtra>(entity =>
        {
            entity.HasIndex(e => new { e.TipoDev, e.NroDev }).IsUnique();
            entity.Property(e => e.ObservacionesCuentasPagar).HasMaxLength(500);
            entity.Property(e => e.ObservacionesLiquidaciones).HasMaxLength(500);
            entity.Property(e => e.UltimoMovimientoSade).HasMaxLength(200);
        });

        // Seed de datos para lookups
        modelBuilder.Entity<StatusDgayfOpcion>().HasData(
            new StatusDgayfOpcion { Id = 1, Nombre = "avanzar", Orden = 1 },
            new StatusDgayfOpcion { Id = 2, Nombre = "avanzar CAF", Orden = 2 },
            new StatusDgayfOpcion { Id = 3, Nombre = "no avanzar", Orden = 3 },
            new StatusDgayfOpcion { Id = 4, Nombre = "no avanzar CAF", Orden = 4 },
            new StatusDgayfOpcion { Id = 5, Nombre = "anulado", Orden = 5 },
            new StatusDgayfOpcion { Id = 6, Nombre = "fuera financiera", Orden = 6 },
            new StatusDgayfOpcion { Id = 7, Nombre = "en proceso baja", Orden = 7 },
            new StatusDgayfOpcion { Id = 8, Nombre = "CCOO inf al area", Orden = 8 },
            new StatusDgayfOpcion { Id = 9, Nombre = "para desafectar", Orden = 9 },
            new StatusDgayfOpcion { Id = 10, Nombre = "GUARDA TEMPORAL", Orden = 10 },
            new StatusDgayfOpcion { Id = 11, Nombre = "Proceso Reclamo", Orden = 11 }
        );

        modelBuilder.Entity<StatusOpOpcion>().HasData(
            new StatusOpOpcion { Id = 1, Nombre = "anulado", Orden = 1 },
            new StatusOpOpcion { Id = 2, Nombre = "OP Firmada", Orden = 2 },
            new StatusOpOpcion { Id = 3, Nombre = "Pasado al pago BONO", Orden = 3 },
            new StatusOpOpcion { Id = 4, Nombre = "pagado 2023", Orden = 4 }
        );

        modelBuilder.Entity<StatusContableOpcion>().HasData(
            new StatusContableOpcion { Id = 1, Nombre = "Factura Pedida", Orden = 1 },
            new StatusContableOpcion { Id = 2, Nombre = "Frenar", Orden = 2 },
            new StatusContableOpcion { Id = 3, Nombre = "Liquidaciones", Orden = 3 },
            new StatusContableOpcion { Id = 4, Nombre = "OP Lista", Orden = 4 },
            new StatusContableOpcion { Id = 5, Nombre = "Presupuesto", Orden = 5 },
            new StatusContableOpcion { Id = 6, Nombre = "Problema SIGAF", Orden = 6 },
            new StatusContableOpcion { Id = 7, Nombre = "Seguros", Orden = 7 }
        );
    }
}
