#nullable enable

namespace SAF.Application.AdminListas;

/// <summary>
/// Una lista administrable: clave estable (viaja a la auditoría y al servicio),
/// título visible y si la tabla tiene el campo extra EsOk (solo Seguros).
/// </summary>
public sealed record ListaAdminDefinicion(string Key, string Titulo, bool TieneEsOk = false);

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
        new(StatusDgayf, "Status DGAyF (Pagos)"),
        new(StatusOp, "Status OP (Pagos)"),
        new(StatusContable, "Status Contable"),
        new(TramitadoresCuentasPagar, "Tramitadores Cuentas a Pagar"),
        new(TramitadoresLiquidaciones, "Tramitadores Liquidaciones"),
        new(Seguros, "Seguros", TieneEsOk: true),
    ];
}
