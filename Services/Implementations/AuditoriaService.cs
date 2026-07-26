#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

public class AuditoriaService(IDbContextFactory<AppDbContext> dbFactory) : IAuditoriaService
{
    // Techo defensivo: el historial de una fila son decenas de cambios, no miles.
    private const int MaxFilasHistorial = 500;

    public async Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        if (registro.Cambios.Count == 0) return;

        var lote = Guid.NewGuid();
        var fecha = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.CambiosAuditoria.AddRange(registro.Cambios.Select(c => new CambioAuditoria
        {
            Lote = lote,
            Fecha = fecha,
            Usuario = Truncar(registro.Usuario, 150) ?? string.Empty,
            Vista = Truncar(registro.Vista, 50) ?? string.Empty,
            EntidadId = registro.EntidadId,
            ClaveNegocio = Truncar(registro.ClaveNegocio, 200) ?? string.Empty,
            Accion = Truncar(registro.Accion, 10) ?? string.Empty,
            Campo = Truncar(c.Campo, 100),
            ValorAnterior = Truncar(c.Antes, 500),
            ValorNuevo = Truncar(c.Despues, 500),
        }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CambioAuditoria>> GetHistorialAsync(
        string vista, int? entidadId, string claveNegocio, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.CambiosAuditoria.AsNoTracking().Where(c => c.Vista == vista);

        query = entidadId is not null
            ? query.Where(c => c.EntidadId == entidadId)
            : query.Where(c => c.ClaveNegocio == claveNegocio);

        return await query
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Take(MaxFilasHistorial)
            .ToListAsync(ct);
    }

    private static string? Truncar(string? valor, int max) =>
        valor is null || valor.Length <= max ? valor : valor[..max];
}
