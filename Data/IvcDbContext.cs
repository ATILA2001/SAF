#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data.Entities;

namespace SAF.Data;

/// <summary>
/// Acceso de solo lectura a la base IVC para leer la tabla DEVENGADOS.
/// No genera migraciones para este contexto.
/// </summary>
public class IvcDbContext(DbContextOptions<IvcDbContext> options) : DbContext(options)
{
    public DbSet<Devengado> Devengados => Set<Devengado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Devengado>(entity =>
        {
            entity.HasNoKey(); // Sin clave gestionada por EF; lectura con SQL raw si es necesario
            entity.ToTable("DEVENGADOS", "dbo");

            // Nombres exactos de columna de IVC (actualizar si difieren)
            entity.Property(e => e.TipoDev).HasColumnName("TIPO_DEV");
            entity.Property(e => e.NroDev).HasColumnName("NRO_DEV");
            entity.Property(e => e.FechaDevengado).HasColumnName("FECHA_DEVENGADO");
            entity.Property(e => e.Expediente).HasColumnName("EXPEDIENTE");
            entity.Property(e => e.Empresa).HasColumnName("EMPRESA");
            entity.Property(e => e.Importe).HasColumnName("IMPORTE");
        });
    }
}
