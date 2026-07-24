#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Reglas del ledger de devengados compartidas por la vista Pagos, el alta manual
/// y la sincronización con IVC (deben coincidir o la vista y el sync divergen).
/// </summary>
public static class ReglasDevengado
{
    /// <summary>Tipos que la vista Pagos excluye (mismo filtro que el sync con IVC).</summary>
    public static readonly string[] TiposExcluidos = ["C55", "CPS"];
}