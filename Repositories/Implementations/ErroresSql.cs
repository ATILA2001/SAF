#nullable enable
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SAF.Repositories.Implementations;

/// <summary>
/// Reconocimiento de errores de SQL Server que la aplicación sabe manejar.
/// </summary>
internal static class ErroresSql
{
    private const int FilaDuplicadaEnIndiceUnico = 2601;
    private const int ViolacionDeRestriccionUnica = 2627;

    /// <summary>
    /// La fila ya existía: otro usuario la insertó entre el chequeo y el guardado.
    /// </summary>
    public static bool EsClaveDuplicada(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && sql.Number is FilaDuplicadaEnIndiceUnico or ViolacionDeRestriccionUnica;
}