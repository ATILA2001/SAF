#nullable enable

namespace SAF.Application.Common;

/// <summary>
/// Otro usuario modificó o borró la fila mientras esta se editaba. Se traduce en el
/// repositorio para que la UI no dependa de las excepciones de EF Core.
/// </summary>
public class ConflictoDeConcurrenciaException(string message) : Exception(message)
{
    public const string MensajeParaElUsuario =
        "Otro usuario modificó esta fila mientras la editabas. Se recargaron los datos: " +
        "revisá los valores y volvé a aplicar tu cambio.";

    public ConflictoDeConcurrenciaException() : this(MensajeParaElUsuario) { }
}