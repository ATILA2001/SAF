using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using SAF.Security;
using SAF.Services.Abstractions;
using System.Security.Claims;

namespace SAF.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject] private IPermissionService PermissionService { get; set; } = null!;
    [Inject] private IPermissionVersionService PermissionVersionService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private ClaimsPrincipal? _user;
    private bool sidebarExpanded = true;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _user = authState.User;

        Navigation.LocationChanged += OnLocationChanged;

        EnsureCurrentPageAccessible();
        // Fuera del camino crítico: no bloquea el primer render (el chequeo es fail-open;
        // si la versión está vencida, redirige a Login cuando Auth.Web responde).
        _ = CheckPermissionVersionAsync();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        EnsureCurrentPageAccessible();
        await CheckPermissionVersionAsync();
    }

    private async Task CheckPermissionVersionAsync()
    {
        if (_user?.Identity?.IsAuthenticated != true) return;

        var isCurrent = await PermissionVersionService.IsVersionCurrentAsync(_user);
        if (!isCurrent)
            Navigation.NavigateTo("/Account/Login", forceLoad: true);
    }

    private void EnsureCurrentPageAccessible()
    {
        if (_user?.Identity?.IsAuthenticated != true) return;

        var path = GetCurrentPath();
        if (IsPublicPath(path)) return;
        if (AdminClaims.IsAdmin(_user)) return;

        if (!PermissionService.CanAccess(_user, path))
            Navigation.NavigateTo("/Account/AccessDenied", replace: true);
    }

    public bool CanShowPage(string path)
    {
        if (_user?.Identity?.IsAuthenticated != true) return false;
        if (AdminClaims.IsAdmin(_user)) return true;
        return PermissionService.CanAccess(_user, path);
    }

    private string GetCurrentPath()
    {
        var relative = Navigation.ToBaseRelativePath(Navigation.Uri);
        return "/" + relative.Split('?')[0].TrimStart('/');
    }

    private static bool IsPublicPath(string path)
        => path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/Error", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/home", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
        => Navigation.LocationChanged -= OnLocationChanged;
}
