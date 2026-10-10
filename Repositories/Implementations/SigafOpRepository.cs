#nullable enable
using Microsoft.EntityFrameworkCore;
using SAF.Application.ControlOp;
using SAF.Application.ControlOp.Dtos;
using SAF.Data;
using SAF.Data.Entities;
using SAF.Repositories.Abstractions;

namespace SAF.Repositories.Implementations;

/// <summary>
/// Las correcciones de OP (tabla propia de SAF) se aplican acá y no en cada consumidor:
/// así nadie puede leer fechas de pago sin corregir por olvidarse de un paso.
/// </summary>
public class SigafOpRepository(
    IDbContextFactory<IvcDbContext> ivcFactory,
    ISigafOpCorreccionRepository correccionRepo) : ISigafOpRepository
{
    public async Task<IReadOnlyDictionary<string, DateTime>> GetFechaPagoByExpedientesAsync(
        IReadOnlyCollection<string> expedientes, CancellationToken ct = default)
    {
        var empty = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (expedientes is null || expedientes.Count == 0)
            return empty;

        // El ledger guarda la clave normalizada (8 dígitos) pero EE_FINANCIERA viene cruda,
        // a veces sin ceros a la izquierda: se piden las dos formas y FechaPagoSigaf
        // vuelve a normalizar lo que llega, así el resultado queda por clave del ledger.
        var distinct = expedientes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .SelectMany(e => ExpedienteBajada.VariantesEnBajada(e.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return empty;

        var correcciones = await correccionRepo.GetClavesAsync(ct);

        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);
        var pagadas = ivc.SigafOpPagos.AsNoTracking().Where(p => p.FechaPago != null);

        List<FechaPagoSigaf.Linea> lineas;
        if (correcciones.Count == 0)
        {
            // MAXIFS de la planilla tal cual: una fila por expediente.
            lineas = await pagadas
                .Where(p => p.EeFinanciera != null && distinct.Contains(p.EeFinanciera))
                .GroupBy(p => p.EeFinanciera!)
                .Select(g => new FechaPagoSigaf.Linea(null, null, g.Key, g.Max(x => x.FechaPago)))
                .ToListAsync(ct);
        }
        else
        {
            // Las OPs corregidas se traen por su clave aunque en la bajada figuren con un
            // expediente que nadie pidió (es lo que las mueve al corregido), y solo
            // ellas se separan del grupo de su expediente: el resto sigue agrupado por
            // expediente, así el tráfico desde IVC no crece con la cantidad de OPs.
            var ops = correcciones
                .Select(c => c.Op.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            lineas = await pagadas
                .Where(p => (p.EeFinanciera != null && distinct.Contains(p.EeFinanciera))
                         || (p.Op != null && ops.Contains(p.Op)))
                .GroupBy(p => new
                {
                    p.EeFinanciera,
                    Op = p.Op != null && ops.Contains(p.Op) ? p.Op : null,
                    NroDocResp = p.Op != null && ops.Contains(p.Op) ? p.NroDocResp : null,
                })
                .Select(g => new FechaPagoSigaf.Linea(g.Key.NroDocResp, g.Key.Op, g.Key.EeFinanciera, g.Max(x => x.FechaPago)))
                .ToListAsync(ct);
        }

        return FechaPagoSigaf.PorExpediente(lineas, correcciones);
    }

    public async Task<IReadOnlyList<OpSigafResumen>> BuscarOpsAsync(
        CriteriosBusquedaOp criterios, CancellationToken ct = default)
    {
        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);

        var query = ivc.SigafOpPagos.AsNoTracking().Where(p => p.Op != null);

        // Contains y no Like: EF escapa los comodines del texto tipeado.
        if (!string.IsNullOrWhiteSpace(criterios.Beneficiario))
        {
            var benef = criterios.Beneficiario.Trim();
            query = query.Where(p => p.BenefOpDesc != null && p.BenefOpDesc.Contains(benef));
        }
        if (criterios.FechaPagoDesde is DateTime desde)
            query = query.Where(p => p.FechaPago >= desde.Date);
        // Por si FECHA_PAGO trae hora: el día "hasta" entra completo.
        if (criterios.FechaPagoHasta is DateTime hasta)
            query = query.Where(p => p.FechaPago < hasta.Date.AddDays(1));

        var agrupado = Agrupar(query);

        // Con importe de referencia, las más cercanas primero (la igual queda arriba y
        // las parciales después); sin él, las más recientes.
        agrupado = criterios.ImporteReferencia is decimal referencia
            ? agrupado.OrderBy(o => Math.Abs((o.ImporteOp ?? 0m) - referencia)).ThenByDescending(o => o.FechaPago)
            : agrupado.OrderByDescending(o => o.FechaPago).ThenBy(o => o.Op);

        return await agrupado.Take(CriteriosBusquedaOp.MaxResultados).ToListAsync(ct);
    }

    public async Task<OpSigafResumen?> GetOpAsync(long nroDocResp, string op, CancellationToken ct = default)
    {
        var opBuscada = op.Trim();
        await using var ivc = await ivcFactory.CreateDbContextAsync(ct);

        // La collation de IVC no distingue mayúsculas: la igualdad alcanza.
        return await Agrupar(ivc.SigafOpPagos.AsNoTracking()
                .Where(p => p.NroDocResp == nroDocResp && p.Op == opBuscada))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Una fila por OP: las líneas (neto, retenciones) comparten expediente, beneficiario,
    /// bruto y fecha, así que MAX los devuelve tal cual. Proyección por miembros: EF
    /// compone OrderBy/Take sobre ella, no sobre un constructor.
    /// </summary>
    private static IQueryable<OpSigafResumen> Agrupar(IQueryable<SigafOpPago> query) =>
        query
            .GroupBy(p => new { p.Op, p.NroDocResp })
            .Select(g => new OpSigafResumen
            {
                Op = g.Key.Op!,
                NroDocResp = g.Key.NroDocResp,
                EeFinanciera = g.Max(x => x.EeFinanciera),
                Beneficiario = g.Max(x => x.BenefOpDesc),
                ImporteOp = g.Max(x => x.ImpOp),
                FechaPago = g.Max(x => x.FechaPago),
                ExpCorregidoIvc = g.Max(x => x.ExpCorregido),
                CorrecionIvc = g.Max(x => x.Correcion),
            });
}
