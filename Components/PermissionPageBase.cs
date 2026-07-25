#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SAF.Security;
using SAF.Services.Abstractions;
using System.Security.Claims;

namespace SAF.Components;

/// <summary>
/// Clase base para páginas que requieren permisos.
/// Expone CanCreate, CanEdit, CanDelete basándose en perms_json.
/// </summary>
public abstract class PermissionPageBase : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    [Inject]
    private IPermissionService PermissionService { get; set; } = null!;

    /// <summary>Permiso de lectura de la página. El redirect del layout es defensa en profundidad.</summary>
    protected bool CanAccess { get; private set; }
    protected bool CanCreate { get; private set; }
    protected bool CanEdit { get; private set; }
    protected bool CanDelete { get; private set; }

    protected async Task LoadPermissionsAsync(string pageUrl)
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (AdminClaims.IsAdmin(user))
        {
            CanAccess = CanCreate = CanEdit = CanDelete = true;
            return;
        }

        CanAccess = PermissionService.CanAccess(user, pageUrl);
        CanCreate = PermissionService.CanPerform(user, pageUrl, "create");
        CanEdit   = PermissionService.CanPerform(user, pageUrl, "edit");
        CanDelete = PermissionService.CanPerform(user, pageUrl, "delete");
    }
}
