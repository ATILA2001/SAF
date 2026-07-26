#nullable enable
using SAF.Application.Common;
using SAF.Data.Entities;

namespace SAF.Services.Abstractions;

/// <summary>Un guardado exitoso con sus campos modificados, listo para auditar.</summary>
public sealed record RegistroAuditoria(
    string Vista,
    int? EntidadId,
    string ClaveNegocio,
    string Accion,
    string Usuario,
    IReadOnlyList<AuditoriaDiff.Cambio> Cambios);

public interface IAuditoriaService
{
    /// <summary>Persiste un lote de cambios. Nunca debe voltear el guardado que audita.</summary>
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default);

    /// <summary>
    /// Historial de una fila: por Id técnico si lo hay; si no (tablero), por la clave
    /// de negocio. Más reciente primero.
    /// </summary>
    Task<IReadOnlyList<CambioAuditoria>> GetHistorialAsync(
        string vista, int? entidadId, string claveNegocio, CancellationToken ct = default);
}
