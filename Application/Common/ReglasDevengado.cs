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

    /// <summary>
    /// Status DGAyF con el que nace toda fila nueva del ledger: la sincronización lo
    /// carga sola y el alta manual lo trae preseleccionado. Es el estado inicial del
    /// circuito (en la planilla se tipeaba a mano en cada fila importada). Se resuelve
    /// por nombre entre las opciones activas de la lista, que se administra desde la app.
    /// </summary>
    public const string StatusDgayfInicial = "avanzar";
}