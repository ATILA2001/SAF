#nullable enable
using System.Security.Claims;

namespace SAF.Security;

/// <summary>
/// Helpers for detecting admin/superuser principals from claim sets.
/// Mirrors Auth.Web's role assignment: any of the role names below grants
/// unrestricted access, bypassing the per-page perms_json check.
/// </summary>
public static class AdminClaims
{
    public static readonly IReadOnlyList<string> AdminRoleNames =
        ["Admin", "Administrador", "Administradores"];

    /// <summary>Returns true when the principal holds any admin role claim.</summary>
    public static bool IsAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;

        foreach (var roleName in AdminRoleNames)
        {
            // Standard .NET role claim
            if (user.IsInRole(roleName)) return true;
        }

        // JWT-style "role" / "roles" claims and URI-based role claims
        foreach (var claim in user.Claims)
        {
            if ((claim.Type == "role" || claim.Type == "roles" ||
                 claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase)) &&
                AdminRoleNames.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
