using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAF.Components.Layout;

namespace Tests.Components;

/// <summary>
/// Smoke test del pipeline de bunit: si esto corre, el molde queda listo para tests
/// de componentes más grandes (GridPageBase y las páginas, vía AddPermissivePermissions).
/// </summary>
[TestClass]
public class ErrorLayoutTests
{
    [TestMethod]
    public void Renderiza_El_Contenido_Del_Body()
    {
        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<ErrorLayout>(parameters => parameters
            .Add(l => l.Body, "contenido de la página de error"));

        StringAssert.Contains(cut.Markup, "contenido de la página de error");
    }
}
