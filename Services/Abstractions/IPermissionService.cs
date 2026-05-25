#nullable enable
using System.Security.Claims;

namespace SAF.Services.Abstractions;

/// <summary>
/// Evalúa los permisos del usuario a partir del claim <c>perms_json</c>
/// emitido por Auth.Web.
/// </summary>
public interface IPermissionService
{
    bool CanAccess(ClaimsPrincipal user, string pageUrl);
    bool CanPerform(ClaimsPrincipal user, string pageUrl, string action);
    IReadOnlyList<string> GetAllowedPages(ClaimsPrincipal user);
}
