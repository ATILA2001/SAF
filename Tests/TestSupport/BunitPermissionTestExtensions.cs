using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SAF.Services.Abstractions;

namespace Tests.TestSupport;

/// <summary>
/// Autoriza al usuario de prueba y le da todos los permisos: es el piso para renderizar
/// con bunit cualquier página que herede de PermissionPageBase.
/// </summary>
internal static class BunitPermissionTestExtensions
{
    public static void AddPermissivePermissions(this Bunit.TestContext context)
    {
        var authContext = context.AddTestAuthorization();
        authContext.SetAuthorized("test-user");

        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(p => p.CanAccess(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .Returns(true);
        permissionService
            .Setup(p => p.CanPerform(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        permissionService
            .Setup(p => p.GetAllowedPages(It.IsAny<ClaimsPrincipal>()))
            .Returns(Array.Empty<string>());

        context.Services.AddSingleton(permissionService.Object);
    }
}
