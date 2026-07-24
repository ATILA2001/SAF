#nullable enable

namespace SAF.Data.Entities;

/// <summary>
/// Lectura de la tabla [IVC].[dbo].[PASES_SADE] (solo lectura).
/// Una fila por expediente con su último pase SADE. Se usa para calcular
/// FECHA SADE y BUZÓN SADE de la vista Pagos, cruzando por expediente
/// (EE_FINANCIERA). Mapeo de columnas en IvcDbContext.
/// </summary>
public class PaseSade
{
    public string Expediente { get; set; } = string.Empty;   // EXPEDIENTE
    public DateTime? FechaUltimoPase { get; set; }            // [FECHA ULTIMO PASE]
    public string? BuzonDestino { get; set; }                 // [BUZON DESTINO]
}
