#nullable enable
using System.Security.Claims;

namespace SAF.Services.Abstractions;

/// <summary>
/// Verifica que el claim <c>perms_version</c> del usuario sea el actual
/// según Auth.Web. Falla abierto ante errores de red.
/// </summary>
public interface IPermissionVersionService
{
    Task<bool> IsVersionCurrentAsync(ClaimsPrincipal user);
}
