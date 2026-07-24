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
    public DbSet<IvcDevengado> Devengados => Set<IvcDevengado>();
    public DbSet<PaseSade> PasesSade => Set<PaseSade>();
    public DbSet<SigafOpPago> SigafOpPagos => Set<SigafOpPago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IvcDevengado>(entity =>
        {
            entity.HasNoKey(); // Solo lectura; sin clave gestionada por EF
            entity.ToTable("DEVENGADOS", "dbo");

            // Nombres reales de columna verificados contra IVC_TEST.dbo.DEVENGADOS
            entity.Property(e => e.TipoDev).HasColumnName("TIPO_DEV");
            entity.Property(e => e.NroDev).HasColumnName("NUMERO_DEVENGADO");
            entity.Property(e => e.FechaImputacion).HasColumnName("FECHA_IMPUTACION");
            entity.Property(e => e.EeFinanciera).HasColumnName("EE_FINANCIERA");
            entity.Property(e => e.Descripcion).HasColumnName("DESCRIPCION");
            entity.Property(e => e.ImportePp).HasColumnName("IMPORTE_PP");
        });

        modelBuilder.Entity<PaseSade>(entity =>
        {
            entity.HasNoKey(); // Solo lectura
            entity.ToTable("PASES_SADE", "dbo");

            // EXPEDIENTE es varchar: IsUnicode(false) para que el parámetro sea varchar
            // y se compare con la collation de la columna (evita conflicto de intercalación).
            entity.Property(e => e.Expediente).HasColumnName("EXPEDIENTE").IsUnicode(false);
            entity.Property(e => e.FechaUltimoPase).HasColumnName("FECHA ULTIMO PASE");
            entity.Property(e => e.BuzonDestino).HasColumnName("BUZON DESTINO");
        });

        modelBuilder.Entity<SigafOpPago>(entity =>
        {
            entity.HasNoKey(); // Solo lectura; la bajada IVC tiene 43 columnas, solo mapeamos 2.
            entity.ToTable("SIGAF_OP", "dbo");

            entity.Property(e => e.EeFinanciera).HasColumnName("EE_FINANCIERA");
            entity.Property(e => e.FechaPago).HasColumnName("FECHA_PAGO");
        });
    }
}
