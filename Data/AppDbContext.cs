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
    public DbSet<Devengado> Devengados { get; set; }
    public DbSet<DevengadoExtra> DevengadosExtra { get; set; }
    public DbSet<StatusContabilidadExtra> StatusContabilidadExtras { get; set; }
    public DbSet<StatusDgayfOpcion> StatusDgayfOpciones { get; set; }
    public DbSet<StatusOpOpcion> StatusOpOpciones { get; set; }
    public DbSet<StatusContableOpcion> StatusContableOpciones { get; set; }
    public DbSet<TramitadorCuentasPagarOpcion> TramitadoresCuentasPagar { get; set; }
    public DbSet<TramitadorLiquidacionesOpcion> TramitadoresLiquidaciones { get; set; }
    public DbSet<ExpedienteCaf> ExpedientesCaf { get; set; }
    public DbSet<ExpedienteSeguro> ExpedientesSeguro { get; set; }
    public DbSet<SeguroOpcion> SeguroOpciones { get; set; }

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

        // Tabla acumulativa de devengados (ledger histórico, filas sin agrupar)
        modelBuilder.Entity<Devengado>(entity =>
        {
            entity.HasIndex(e => new { e.TipoDev, e.NroDev });
            entity.HasIndex(e => e.FechaImputacion);
            entity.Property(e => e.Expediente).HasMaxLength(50);
            entity.Property(e => e.Empresa).HasMaxLength(255);
            entity.Property(e => e.ImportePp).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DevengadoExtra>(entity =>
        {
            // Un registro editable por FILA del ledger (no por devengado).
            entity.HasIndex(e => e.DevengadoId).IsUnique();
            entity.HasOne(e => e.Devengado)
                .WithMany()
                .HasForeignKey(e => e.DevengadoId);
            entity.HasIndex(e => new { e.TipoDev, e.NroDev });
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Ccoo).HasMaxLength(200);
        });

        modelBuilder.Entity<StatusContabilidadExtra>(entity =>
        {
            entity.HasIndex(e => new { e.TipoDev, e.NroDev }).IsUnique();
            entity.Property(e => e.ObservacionesCuentasPagar).HasMaxLength(500);
            entity.Property(e => e.ObservacionesLiquidaciones).HasMaxLength(500);
            entity.Property(e => e.SinFacturaMotivo).HasMaxLength(20);
        });

        modelBuilder.Entity<ExpedienteCaf>(entity =>
        {
            entity.HasIndex(e => e.Expediente);
            entity.HasIndex(e => e.Anio);
            entity.Property(e => e.Expediente).HasMaxLength(30);
            entity.Property(e => e.Op).HasMaxLength(50);
            entity.Property(e => e.Beneficiario).HasMaxLength(255);
            entity.Property(e => e.Cuenta).HasMaxLength(50);
            entity.Property(e => e.CcPagadora).HasMaxLength(100);
            entity.Property(e => e.Pase).HasMaxLength(100);
            entity.Property(e => e.ImporteNeto).HasPrecision(18, 2);
            entity.Property(e => e.Iibb).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ExpedienteSeguro>(entity =>
        {
            entity.HasIndex(e => e.Expediente);
            entity.Property(e => e.Expediente).HasMaxLength(30);
            entity.Property(e => e.Op).HasMaxLength(50);
            entity.Property(e => e.Beneficiario).HasMaxLength(255);
            entity.Property(e => e.Estado).HasMaxLength(100);
            entity.Property(e => e.ImporteNeto).HasPrecision(18, 2);
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

        // Lista vigente: hoja "lista" col. "STATUS GS" de "Status Contabilidad 2026.xlsx" (22 opciones).
        // "Frenar" (Id 2) venía de la lista vieja de "Pagos 2026.xlsx" y no existe en la vigente: queda inactiva.
        modelBuilder.Entity<StatusContableOpcion>().HasData(
            new StatusContableOpcion { Id = 1, Nombre = "Factura Pedida", Orden = 1 },
            new StatusContableOpcion { Id = 2, Nombre = "Frenar", Orden = 23, Activo = false },
            new StatusContableOpcion { Id = 3, Nombre = "Liquidaciones", Orden = 3 },
            new StatusContableOpcion { Id = 4, Nombre = "OP Lista", Orden = 4 },
            new StatusContableOpcion { Id = 5, Nombre = "Presupuesto", Orden = 5 },
            new StatusContableOpcion { Id = 6, Nombre = "Problema SIGAF", Orden = 6 },
            new StatusContableOpcion { Id = 7, Nombre = "Seguros", Orden = 7 },
            new StatusContableOpcion { Id = 8, Nombre = "Falta Poliza", Orden = 2 },
            new StatusContableOpcion { Id = 9, Nombre = "Error en la Factura", Orden = 8 },
            new StatusContableOpcion { Id = 10, Nombre = "Error en Documentación", Orden = 9 },
            new StatusContableOpcion { Id = 11, Nombre = "No presenta factura", Orden = 10 },
            new StatusContableOpcion { Id = 12, Nombre = "Activo Fijo", Orden = 11 },
            new StatusContableOpcion { Id = 13, Nombre = "Para pedir FC", Orden = 12 },
            new StatusContableOpcion { Id = 14, Nombre = "Fuera de financiera", Orden = 13 },
            new StatusContableOpcion { Id = 15, Nombre = "No presenta factura - Envío de CCOO", Orden = 14 },
            new StatusContableOpcion { Id = 16, Nombre = "Notificación cédula proveedor", Orden = 15 },
            new StatusContableOpcion { Id = 17, Nombre = "Guarda temporal- No presenta Factura", Orden = 16 },
            new StatusContableOpcion { Id = 18, Nombre = "DG Financiera", Orden = 17 },
            new StatusContableOpcion { Id = 19, Nombre = "Archivado", Orden = 18 },
            new StatusContableOpcion { Id = 20, Nombre = "Pendiente", Orden = 19 },
            new StatusContableOpcion { Id = 21, Nombre = "Anulado", Orden = 20 },
            new StatusContableOpcion { Id = 22, Nombre = "Falta Anexo I Banco", Orden = 21 },
            new StatusContableOpcion { Id = 23, Nombre = "No presenta anexo I", Orden = 22 }
        );

        // Tramitadores: hoja "lista" de "Status Contabilidad 2026.xlsx".
        modelBuilder.Entity<TramitadorCuentasPagarOpcion>().HasData(
            new TramitadorCuentasPagarOpcion { Id = 1, Nombre = "Daniela", Orden = 1 },
            new TramitadorCuentasPagarOpcion { Id = 2, Nombre = "Verónica", Orden = 2 },
            new TramitadorCuentasPagarOpcion { Id = 3, Nombre = "Mariela", Orden = 3 },
            new TramitadorCuentasPagarOpcion { Id = 4, Nombre = "Ignacio", Orden = 4 },
            new TramitadorCuentasPagarOpcion { Id = 5, Nombre = "Brian", Orden = 5 }
        );

        // Valores observados en la hoja SEGUROS del Excel (ok / CAF/ok, con typo "oK")
        // + "Pendiente" como estado no-ok para la regla "peor caso gana".
        modelBuilder.Entity<SeguroOpcion>().HasData(
            new SeguroOpcion { Id = 1, Nombre = "ok", Orden = 1, EsOk = true },
            new SeguroOpcion { Id = 2, Nombre = "CAF/ok", Orden = 2, EsOk = true },
            new SeguroOpcion { Id = 3, Nombre = "Pendiente", Orden = 3, EsOk = false }
        );

        modelBuilder.Entity<TramitadorLiquidacionesOpcion>().HasData(
            new TramitadorLiquidacionesOpcion { Id = 1, Nombre = "Mayra", Orden = 1 },
            new TramitadorLiquidacionesOpcion { Id = 2, Nombre = "Stephanie", Orden = 2 },
            new TramitadorLiquidacionesOpcion { Id = 3, Nombre = "Camila", Orden = 3 },
            new TramitadorLiquidacionesOpcion { Id = 4, Nombre = "Gabriel", Orden = 4 },
            new TramitadorLiquidacionesOpcion { Id = 5, Nombre = "Mauro", Orden = 5 },
            new TramitadorLiquidacionesOpcion { Id = 6, Nombre = "Tadeo", Orden = 6 }
        );
    }
}
