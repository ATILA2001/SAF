using System.Security.Claims;
using System.Text.Json;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

public sealed class PermissionService : IPermissionService
{
    private const string PermsJsonClaimType = "perms_json";

    public bool CanAccess(ClaimsPrincipal user, string pageUrl)
        => GetPage(user, pageUrl) is not null;

    public bool CanPerform(ClaimsPrincipal user, string pageUrl, string action)
    {
        var page = GetPage(user, pageUrl);
        return page is not null &&
               page.Value.Actions.Any(a =>
                   string.Equals(a, action, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> GetAllowedPages(ClaimsPrincipal user)
        => ParsePayload(user)
            .Select(p => p.Url)
            .ToList()
            .AsReadOnly();

    private static PageEntry? GetPage(ClaimsPrincipal user, string pageUrl)
    {
        foreach (var p in ParsePayload(user))
            if (string.Equals(p.Url, pageUrl, StringComparison.OrdinalIgnoreCase))
                return p;
        return null;
    }

    private static IReadOnlyList<PageEntry> ParsePayload(ClaimsPrincipal user)
    {
        var json = user.FindFirstValue(PermsJsonClaimType);
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<PageEntry>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("pages", out var pagesEl))
                return Array.Empty<PageEntry>();

            var result = new List<PageEntry>();
            foreach (var page in pagesEl.EnumerateArray())
            {
                var url = page.TryGetProperty("url", out var urlEl)
                    ? urlEl.GetString() ?? string.Empty
                    : string.Empty;

                var actions = page.TryGetProperty("actions", out var actEl)
                    ? actEl.EnumerateArray()
                              .Select(a => a.GetString() ?? string.Empty)
                              .Where(a => a.Length > 0)
                              .ToArray()
                    : Array.Empty<string>();

                if (!string.IsNullOrWhiteSpace(url))
                    result.Add(new PageEntry(url, actions));
            }
            return result;
        }
        catch (JsonException)
        {
            return Array.Empty<PageEntry>();
        }
    }

    private readonly record struct PageEntry(string Url, string[] Actions);
}
