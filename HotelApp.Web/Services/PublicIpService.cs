using Microsoft.Extensions.Caching.Memory;

namespace HotelApp.Web.Services;

/// <summary>
/// Resolves the real public IP of the current request.
/// When the TCP / proxy-header IP is a loopback address (::1, 127.x.x.x),
/// the service fetches the server's own outbound internet IP from a public echo
/// API and caches it for 6 hours to avoid repeated external calls.
/// </summary>
public class PublicIpService : IPublicIpService
{
    // Cache key for the server's own outbound public IP
    private const string CacheKey = "ServerOutboundPublicIp";

    // Two independent services tried in order — first success wins
    private static readonly string[] IpEchoUrls =
    [
        "https://api.ipify.org?format=text",
        "https://checkip.amazonaws.com/"
    ];

    private readonly IHttpClientFactory             _httpClientFactory;
    private readonly IMemoryCache                   _cache;
    private readonly ILogger<PublicIpService>       _logger;

    public PublicIpService(
        IHttpClientFactory       httpClientFactory,
        IMemoryCache             cache,
        ILogger<PublicIpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache             = cache;
        _logger            = logger;
    }

    public async Task<string> ResolveAsync(HttpContext context)
    {
        // Step 1: try reverse-proxy headers then TCP address
        var ip = ReadFromHeaders(context);

        // Step 2: if not a loopback address, use it directly
        if (!IsLoopback(ip))
            return ip;

        // Step 3: loopback — server is being accessed from the same machine (no
        // proxy in front, or proxy strips headers). Fetch the server's real
        // outbound public IP instead.
        return await GetServerPublicIpAsync();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static string ReadFromHeaders(HttpContext context)
    {
        // X-Forwarded-For: may be a comma-separated chain; first entry is origin
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var firstIp = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (!string.IsNullOrWhiteSpace(firstIp))
                return firstIp;
        }

        // X-Real-IP: single-value header set by some reverse proxies
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp;

        // Fallback: direct TCP address, mapped to IPv4 so ::ffff:x.x.x.x → x.x.x.x
        var addr = context.Connection.RemoteIpAddress;
        return addr?.IsIPv4MappedToIPv6 == true
            ? addr.MapToIPv4().ToString()
            : addr?.ToString() ?? string.Empty;
    }

    private static bool IsLoopback(string ip) =>
        string.IsNullOrWhiteSpace(ip)                           ||
        ip == "::1"                                             ||
        ip.StartsWith("127.", StringComparison.Ordinal)         ||
        ip == "0.0.0.1";

    private async Task<string> GetServerPublicIpAsync()
    {
        // Return cached value if available (avoids outbound call on every request)
        if (_cache.TryGetValue(CacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var client = _httpClientFactory.CreateClient("PublicIpEcho");

        foreach (var url in IpEchoUrls)
        {
            try
            {
                using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var       result = (await client.GetStringAsync(url, cts.Token)).Trim();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    _cache.Set(CacheKey, result,
                        new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
                        });
                    _logger.LogInformation("Server outbound public IP resolved: {Ip}", result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Public IP echo call failed for {Url} — trying next.", url);
            }
        }

        _logger.LogWarning("All public IP echo services failed; recording 127.0.0.1.");
        return "127.0.0.1";
    }
}
