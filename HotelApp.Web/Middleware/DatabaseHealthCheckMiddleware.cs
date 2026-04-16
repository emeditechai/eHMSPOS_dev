using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace HotelApp.Web.Middleware;

/// <summary>
/// Checks database connectivity on each dynamic request.
/// If the database is unreachable, serves a static maintenance page (HTTP 503).
/// Health status is cached for 15 seconds to avoid excessive connection attempts.
/// </summary>
public class DatabaseHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseHealthCheckMiddleware> _logger;
    private const string CacheKey = "DbHealthCheck_IsHealthy";
    private const int CacheDurationSeconds = 15;
    private const int ConnectTimeoutSeconds = 3;

    private static readonly string[] BypassPrefixes =
    {
        "/css", "/js", "/lib", "/images", "/uploads",
        "/favicon", "/maintenance.html"
    };

    public DatabaseHealthCheckMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<DatabaseHealthCheckMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var connStr = configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("DefaultConnection not configured.");

        // Override connection timeout to keep health checks fast
        var builder = new SqlConnectionStringBuilder(connStr)
        {
            ConnectTimeout = ConnectTimeoutSeconds
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Let static files pass through without a DB check
        foreach (var prefix in BypassPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        var isHealthy = await CheckHealthAsync(cache);

        if (!isHealthy)
        {
            _logger.LogWarning("Database is unreachable. Serving maintenance page for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers["Retry-After"] = "30";

            var maintenancePath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "maintenance.html");

            if (File.Exists(maintenancePath))
            {
                await context.Response.SendFileAsync(maintenancePath);
            }
            else
            {
                await context.Response.WriteAsync(
                    "<h1>Service Temporarily Unavailable</h1><p>Please try again later.</p>");
            }
            return;
        }

        await _next(context);
    }

    private async Task<bool> CheckHealthAsync(IMemoryCache cache)
    {
        if (cache.TryGetValue(CacheKey, out bool cachedStatus))
        {
            return cachedStatus;
        }

        bool healthy;
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.CommandTimeout = ConnectTimeoutSeconds;
            await cmd.ExecuteScalarAsync();
            healthy = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            healthy = false;
        }

        cache.Set(CacheKey, healthy, TimeSpan.FromSeconds(CacheDurationSeconds));
        return healthy;
    }
}
