#nullable enable

namespace SAF.Application.AdminListas;

/// <summary>
/// Una lista administrable: clave estable (viaja a la auditoría y al servicio),
/// título visible, la vista cuyos desplegables alimenta (decide quién puede
/// administrarla) y si la tabla tiene el campo extra EsOk (solo Seguros).
/// </summary>
public sealed record ListaAdminDefinicion(string Key, string Titulo, string VistaUrl, bool TieneEsOk = false);

/// <summary>
/// Catálogo de las listas de opciones que se administran en /admin/listas.
/// Las claves son estables: la auditoría las usa como prefijo de la clave de
/// negocio, así que renombrarlas rompería el historial ya registrado.
/// </summary>
public static class ListasAdmin
{
    public const string StatusDgayf = "status-dgayf";
    public const string StatusOp = "status-op";
    public const string StatusContable = "status-contable";
    public const string TramitadoresCuentasPagar = "tramitadores-cuentas-pagar";
    public const string TramitadoresLiquidaciones = "tramitadores-liquidaciones";
    public const string Seguros = "seguros";

    // Los títulos también son nombre de hoja al exportar: máximo 31 caracteres.
    public static readonly IReadOnlyList<ListaAdminDefinicion> Todas =
    [
        new(StatusDgayf, "Status DGAyF (Pagos)", "/pagos"),
        new(StatusOp, "Status OP (Pagos)", "/pagos"),
        new(StatusContable, "Status Contable", "/status-contabilidad"),
        new(TramitadoresCuentasPagar, "Tramitadores Cuentas a Pagar", "/status-contabilidad"),
        new(TramitadoresLiquidaciones, "Tramitadores Liquidaciones", "/status-contabilidad"),
        new(Seguros, "Seguros", "/seguros", TieneEsOk: true),
    ];
}
