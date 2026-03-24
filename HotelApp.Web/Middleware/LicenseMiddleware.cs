using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace HotelApp.Web.Middleware;

/// <summary>
/// Enforces the application license on every incoming request.
///
/// Validation flow (once per calendar day — result cached in IMemoryCache until midnight):
///   1. Load active OTP-verified license from the LOCAL database.
///      → No license found → redirect to /License/Register
///   2. Fetch matching record from REMOTE Central_Lic_DB (ClientCode + LicenseKey).
///      → If remote unreachable → fail-open (allow, log warning).
///   3. Collect all errors:
///      - IsActive = 0            → "Client deactivated"
///      - ExpiryDate in the past  → "Software Expired on <date> ..."
///      - Live hardware ≠ remote  → "Hardware Data Mismatch ..."
///   4. Any error → render /License/Invalid?errors=... (full-page modal).
///      All errors shown together in a single modal.
///   5. All ok → update local LastLoginDate + fire remote update; store
///      today's date in IMemoryCache so subsequent requests skip validation.
/// </summary>
public class LicenseMiddleware
{
    private static readonly string[] BypassPrefixes =
    [
        "/License",
        "/favicon",
        "/css",
        "/js",
        "/lib",
        "/images",
        "/uploads"
    ];

    private readonly RequestDelegate      _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache         _cache;
    private readonly bool                 _bypassHardwareCheck;
    private readonly IPublicIpService     _publicIpService;
    private readonly ILogger<LicenseMiddleware> _logger;

    // Cache key prefix — one entry per ClientCode
    private const string CacheKeyPrefix = "LicValidated_";

    public LicenseMiddleware(
        RequestDelegate      next,
        IServiceScopeFactory scopeFactory,
        IMemoryCache         cache,
        IConfiguration       configuration,
        IPublicIpService     publicIpService,
        ILogger<LicenseMiddleware> logger)
    {
        _next                = next;
        _scopeFactory        = scopeFactory;
        _cache               = cache;
        _bypassHardwareCheck = configuration.GetValue<bool>("Licensing:BypassHardwareCheck");
        _publicIpService     = publicIpService;
        _logger              = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsBypassPath(path)) { await _next(context); return; }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var licenseRepo = scope.ServiceProvider.GetRequiredService<ILicenseRepository>();
        var remoteRepo  = scope.ServiceProvider.GetRequiredService<IRemoteLicenseRepository>();
        var hwService   = scope.ServiceProvider.GetRequiredService<IHardwareInfoService>();

        // Ensure local tables exist (idempotent, fast after first run)
        await licenseRepo.EnsureTablesExistAsync();

        // Ensure remote LicenseValidationHistory table exists (runs once per lifetime)
        await remoteRepo.EnsureHistoryTableAsync();

        // ── 1. Local active license — prefer record matching current server URL ──
        var appUrlEarly = $"{context.Request.Scheme}://{context.Request.Host}";
        var license = await licenseRepo.GetActiveLicenseAsync(appUrlEarly);
        if (license == null)
        {
            context.Response.Redirect("/License/Register");
            return;
        }

        // ── 2. Check daily cache — skip full validation if already passed today ──
        var cacheKey = CacheKeyPrefix + license.ClientCode;
        if (_cache.TryGetValue(cacheKey, out DateOnly cachedDate) && cachedDate == DateOnly.FromDateTime(DateTime.Today))
        {
            await _next(context);
            return;
        }

        // ── 2b. DB-backed fallback — handles app-pool recycles mid-day ──────────
        // IMemoryCache is process-scoped; an IIS/Kestrel recycle wipes it.
        // Check the LicenseValidationHistory table: if a successful validation was
        // already logged today (any time after midnight), skip remote validation again
        // and repopulate the in-memory cache for the remainder of the day.
        if (await licenseRepo.HasValidationTodayAsync(license.ClientCode!))
        {
            _logger.LogInformation("[LicenseMiddleware] Daily validation already in DB history for {ClientCode} — skipping remote call (cache repopulated).", license.ClientCode);
            _cache.Set(cacheKey, DateOnly.FromDateTime(DateTime.Today),
                new MemoryCacheEntryOptions { AbsoluteExpiration = DateTime.Today.AddDays(1) });
            await _next(context);
            return;
        }

        // ── 3. Full daily validation (first hit after midnight) ───────────────
        await PerformDailyValidationAsync(context, license, licenseRepo, remoteRepo, hwService, cacheKey);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task PerformDailyValidationAsync(
        HttpContext context,
        ClientAppLicense localLicense,
        ILicenseRepository licenseRepo,
        IRemoteLicenseRepository remoteRepo,
        IHardwareInfoService hwService,
        string cacheKey)
    {
        var errors = new List<string>();

        // ── Dev bypass: skip all remote validation when BypassHardwareCheck is on ──
        if (_bypassHardwareCheck)
        {
            _logger.LogInformation("[LicenseMiddleware] Dev bypass active — skipping remote validation.");
            var midnight2 = DateTime.Today.AddDays(1);
            _cache.Set(cacheKey, DateOnly.FromDateTime(DateTime.Today),
                new MemoryCacheEntryOptions { AbsoluteExpiration = midnight2 });
            await _next(context);
            return;
        }

        // ── Fetch remote record (ClientCode + LicenseKey + AppUrl must all match) ──
        var appUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        ClientAppLicense? remote = null;
        bool remoteReachable = true;
        try
        {
            remote = await remoteRepo.GetLicenseForValidationAsync(
                localLicense.ClientCode!, localLicense.LicenseKey!, appUrl);
        }
        catch
        {
            remoteReachable = false;
        }

        if (remoteReachable && remote == null)
        {
            if (!string.Equals(localLicense.AppUrl, appUrl, StringComparison.OrdinalIgnoreCase))
            {
                // AppUrl mismatch — could be a new server OR developer running locally against prod DB.
                // Fetch the remote record WITHOUT AppUrl filter to distinguish the two cases:
                //   • If not found on remote at all → truly new/unregistered server → Register
                //   • If found on remote (different URL) → dev/multi-URL scenario →
                //     still validate IsActive + Expiry, but skip hardware (different machine)
                var remoteNoUrl = await remoteRepo.GetLicenseWithoutUrlAsync(
                    localLicense.ClientCode!, localLicense.LicenseKey!);
                if (remoteNoUrl == null)
                {
                    context.Response.Redirect("/License/Register");
                    return;
                }
                // Use the no-url record for IsActive + Expiry validation only
                remote = remoteNoUrl;
            }
            else
            {
                // AppUrl matches but not found in remote — treat as deactivated/not found
                errors.Add("License record not found in the central server. Contact Vendor Emeditech Plus LLP.");
            }
        }

        if (remote != null)
        {
            // ── Rule 4: IsActive check ───────────────────────────────────────
            if (!remote.IsActive)
                errors.Add("Client deactivated. Contact Vendor Emeditech Plus LLP.");

            // ── Rule 3: Expiry check ─────────────────────────────────────────
            if (remote.ExpiryDate.HasValue && remote.ExpiryDate.Value.Date < DateTime.Today)
                errors.Add($"Software Expired on {remote.ExpiryDate.Value:dd-MMM-yyyy}. " +
                           "Contact Vendor for Renewal.");

            // ── Rule 2: Live hardware vs REMOTE stored values (separate alert per field) ──
            // Skip hardware check if AppUrl differs (dev/multi-URL scenario — different machine)
            bool appUrlMatches = string.Equals(localLicense.AppUrl, appUrl, StringComparison.OrdinalIgnoreCase);
            if (_bypassHardwareCheck || !appUrlMatches)
            {
                // Dev bypass — skip hardware comparison
            }
            else if (appUrlMatches)
            {
                var hw = hwService.GetHardwareInfo();
                var macMatch = string.Equals(hw.MacId,             remote.ServerMacID,       StringComparison.OrdinalIgnoreCase);
                var hddMatch = string.Equals(hw.HardDiskSerial,    remote.HardDiskNumber,    StringComparison.OrdinalIgnoreCase);
                var mbMatch  = string.Equals(hw.MotherboardSerial, remote.MotherboardNumber, StringComparison.OrdinalIgnoreCase);

                if (!macMatch) errors.Add("MAC Address Mismatch. Contact Vendor Emeditech Plus LLP.");
                if (!hddMatch) errors.Add("Hard Disk Serial Mismatch. Contact Vendor Emeditech Plus LLP.");
                if (!mbMatch)  errors.Add("Motherboard ID Mismatch. Contact Vendor Emeditech Plus LLP.");
            }
        }

        // ── Build validation history entry ───────────────────────────────────
        var hw2        = hwService.GetHardwareInfo();
        var publicIp   = await _publicIpService.ResolveAsync(context);
        // appUrl is already computed above for the remote lookup
        var deviceInfo = $"Host={Environment.MachineName};" +
                         $"OS={RuntimeInformation.OSDescription};" +
                         $"MAC={hw2.MacId};HardDisk={hw2.HardDiskSerial};MB={hw2.MotherboardSerial}";

        bool isValid = errors.Count == 0;

        var historyEntry = new LicenseValidationHistory
        {
            ClientCode      = localLicense.ClientCode,
            LicenseKey      = localLicense.LicenseKey,
            IsValid         = isValid,
            FailureReason   = isValid ? null : string.Join(" | ", errors),
            PublicIPAddress = publicIp,
            DeviceInfo      = deviceInfo,
            AppUrl          = appUrl,
            ProductType     = localLicense.ProductType ?? "eLuxstay"
        };

        // Log locally first (authoritative)
        await licenseRepo.LogValidationAsync(historyEntry);

        // Log to remote and update LastLoginDate — awaited directly so failures
        // are captured in the handler's scope and logged properly.
        await remoteRepo.LogValidationAsync(historyEntry);
        if (isValid)
            await remoteRepo.UpdateLastLoginDateAsync(localLicense.ClientCode!);

        if (!isValid)
        {
            // Rule 5: all error messages go to a single modal page via query string
            var errorParam = UrlEncoder.Default.Encode(string.Join("||", errors));
            context.Response.Redirect($"/License/Invalid?errors={errorParam}");
            return;
        }

        // ── Validation passed: update local LastLoginDate + sync remote data + set cache ──
        await licenseRepo.UpdateLastLoginDateAsync(localLicense.ClientCode!);
        if (remote != null)
            await licenseRepo.SyncFromRemoteAsync(localLicense.ClientCode!, remote);

        // Cache valid-today marker; expires precisely at next midnight
        var midnight = DateTime.Today.AddDays(1);
        _cache.Set(cacheKey, DateOnly.FromDateTime(DateTime.Today),
            new MemoryCacheEntryOptions { AbsoluteExpiration = midnight });

        await _next(context);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static bool IsBypassPath(string path)
    {
        foreach (var prefix in BypassPrefixes)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
