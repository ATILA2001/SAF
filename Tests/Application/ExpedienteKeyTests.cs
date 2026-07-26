using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Application.Common;

namespace Tests.Application;

/// <summary>
/// La clave de cruce de todo el sistema: cada formato que acepta o rechaza está
/// decidido contra datos reales de IVC (ver rama SADE acotada a 6-8 dígitos).
/// </summary>
[TestClass]
public class ExpedienteKeyTests
{
    [TestMethod]
    [DataRow("1212221/26", "01212221/26")]      // pad a 8 dígitos
    [DataRow("01212221/26", "01212221/26")]     // ya normalizado
    [DataRow("123456/26", "00123456/26")]       // mínimo de 6 dígitos
    [DataRow("01212221-26", "01212221/26")]     // separador guión
    [DataRow("01212221 26", "01212221/26")]     // separador espacio
    [DataRow(" 01212221/26 ", "01212221/26")]   // espacios alrededor
    public void FormatoFinanciera_Normaliza(string entrada, string esperado)
        => Assert.AreEqual(esperado, ExpedienteKey.Normalizar(entrada));

    [TestMethod]
    [DataRow("EX-2026-01212221-GCABA-IVC", "01212221/26")]
    [DataRow("EX 2026 01212221 GCABA IVC", "01212221/26")]  // tolera espacios
    [DataRow("EX-2024-123456-GCABA-IVC", "00123456/24")]     // 6 dígitos con pad
    public void FormatoSade_Normaliza(string entrada, string esperado)
        => Assert.AreEqual(esperado, ExpedienteKey.Normalizar(entrada));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("abc")]
    [DataRow("12345/26")]                        // 5 dígitos: menos del mínimo
    [DataRow("123456789/26")]                    // 9 dígitos: nunca cruza con Devengados
    [DataRow("EX-2026-123456789-GCABA-IVC")]     // rama SADE también acota a 8
    [DataRow("EX-01212221-GCABA-IVC")]           // SADE sin año
    public void FormatoInvalido_DevuelveNull(string? entrada)
        => Assert.IsNull(ExpedienteKey.Normalizar(entrada));
}
