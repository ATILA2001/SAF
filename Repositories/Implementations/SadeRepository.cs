#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

public class SadeRepository(IDbContextFactory<IvcDbContext> ivcFactory) : ISadeRepository
{
    public async Task<IReadOnlyDictionary<string, PaseSade>> GetByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, PaseSade>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);

        var rows = await ivc.PasesSade.AsNoTracking()
            .Where(p => distinct.Contains(p.Expediente))
            .ToListAsync(ct);

        // PASES_SADE es única por expediente; el indexador tolera duplicados (último gana).
        var dict = new Dictionary<string, PaseSade>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            dict[r.Expediente] = r;
        return dict;
    }
}
