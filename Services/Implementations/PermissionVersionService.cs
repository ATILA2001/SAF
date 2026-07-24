using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SAF.Services.Abstractions;

namespace SAF.Services.Implementations;

public sealed class PermissionVersionService : IPermissionVersionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionVersionService> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(2);
    private const string PermsVersionClaimType = "perms_version";

    internal static string CookieCacheKey(string userId) => $"cookie_hdr:{userId}";

    // La marca "ya chequeado hace <5 min" vive en IMemoryCache (singleton), no en un
    // campo de instancia: este servicio es scoped (por circuito) y un campo hacía que
    // cada F5/nueva pestaña repitiera el HTTP a Auth.Web aunque recién se hubiera chequeado.
    private static string CheckedCacheKey(string userId) => $"perms_version_ok:{userId}";

    public PermissionVersionService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<PermissionVersionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsVersionCurrentAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("perms_version check: user has no NameIdentifier claim. Fail open.");
            return true;
        }

        if (_cache.TryGetValue(CheckedCacheKey(userId), out _))
            return true;

        var cookieVersion = ParseVersion(user);

        var rawCookieHeader = _cache.Get<string>(CookieCacheKey(userId));
        if (string.IsNullOrEmpty(rawCookieHeader))
        {
            _logger.LogWarning("perms_version check: no cookie header in cache for user {UserId}. Fail open.", userId);
            _cache.Set(CheckedCacheKey(userId), true, CacheTtl);
            return true;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AuthWeb");
            using var cts = new CancellationTokenSource(CallTimeout);

            var request = new HttpRequestMessage(HttpMethod.Get, "api/permissions/version");
            request.Headers.TryAddWithoutValidation("Cookie", rawCookieHeader);

            var response = await client.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("perms_version check returned {Status} for user {UserId}. Fail open.",
                    response.StatusCode, userId);
                _cache.Set(CheckedCacheKey(userId), true, CacheTtl);
                return true;
            }

            var dto = await response.Content
                .ReadFromJsonAsync<PermissionVersionDto>(cancellationToken: cts.Token);

            var serverVersion = dto?.Version ?? 0;

            if (serverVersion != cookieVersion)
            {
                _logger.LogInformation(
                    "perms_version mismatch for user {UserId}: cookie={Cookie} server={Server}. Re-login required.",
                    userId, cookieVersion, serverVersion);
                return false;
            }

            _cache.Set(CheckedCacheKey(userId), true, CacheTtl);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("perms_version check timed out for user {UserId}. Fail open.", userId);
            _cache.Set(CheckedCacheKey(userId), true, CacheTtl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "perms_version check failed for user {UserId}. Fail open.", userId);
            _cache.Set(CheckedCacheKey(userId), true, CacheTtl);
            return true;
        }
    }

    private static int ParseVersion(ClaimsPrincipal user)
    {
        var versionStr = user.FindFirstValue(PermsVersionClaimType);
        return int.TryParse(versionStr, out var v) ? v : 0;
    }

    private sealed record PermissionVersionDto(int Version);
}
