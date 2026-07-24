#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class SigafOpRepository(IvcDbContext ivc) : ISigafOpRepository
{
    public async Task<IReadOnlyDictionary<string, DateTime>> GetFechaPagoByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        // MAXIFS de la planilla: mayor FECHA_PAGO por expediente (cualquier fila).
        var rows = await ivc.SigafOpPagos.AsNoTracking()
            .Where(p => p.EeFinanciera != null
                     && p.FechaPago != null
                     && distinct.Contains(p.EeFinanciera))
            .GroupBy(p => p.EeFinanciera!)
            .Select(g => new { Expediente = g.Key, FechaPago = g.Max(x => x.FechaPago) })
            .ToListAsync(ct);

        var dict = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            if (r.FechaPago is DateTime f)
                dict[r.Expediente] = f;
        return dict;
    }
}
