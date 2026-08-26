#nullable enable
using SAF.Application.StatusContabilidad.Dtos;

namespace SAF.Services.Abstractions;

public interface IStatusContabilidadService
{
    /// <summary>Fase propia: todas las columnas salvo las derivadas de IVC.</summary>
    Task<IReadOnlyList<StatusContabilidadViewModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Relee una sola fila del tablero (las líneas de un devengado), para refrescar en
    /// el lugar la que se acaba de guardar. Null: ya no hay líneas con esa clave.
    /// </summary>
    Task<StatusContabilidadViewModel?> GetPorDevengadoAsync(
        string tipoDev, int nroDev, CancellationToken ct = default);

    /// <summary>
    /// Rellena sobre los ítems ya pintados Buzón SADE y Último Movimiento (IVC). Se
    /// difiere porque la primera conexión a IVC puede tardar segundos y no debe frenar
    /// la primera pintada.
    /// </summary>
    Task CompletarIvcAsync(IReadOnlyList<StatusContabilidadViewModel> items, CancellationToken ct = default);
    Task UpsertAsync(StatusContabilidadViewModel vm, CancellationToken ct = default);
}
