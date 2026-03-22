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

    // Cache key prefix — one entry per ClientCode
    private const string CacheKeyPrefix = "LicValidated_";

    public LicenseMiddleware(
        RequestDelegate      next,
        IServiceScopeFactory scopeFactory,
        IMemoryCache         cache,
        IConfiguration       configuration,
        IPublicIpService     publicIpService)
    {
        _next                = next;
        _scopeFactory        = scopeFactory;
        _cache               = cache;
        _bypassHardwareCheck = configuration.GetValue<bool>("Licensing:BypassHardwareCheck");
        _publicIpService     = publicIpService;
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

        // ── 1. Local active license ───────────────────────────────────────────
        var license = await licenseRepo.GetActiveLicenseAsync();
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
            // If the stored AppUrl doesn't match the current server URL, this is a
            // different server sharing the same database (new deployment). Treat it
            // as a fresh installation and redirect to the Registration page instead
            // of showing a validation-failed error.
            if (!string.Equals(localLicense.AppUrl, appUrl, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/License/Register");
                return;
            }

            // Record exists locally but not in remote — treat as deactivated
            errors.Add("License record not found in the central server. Contact Vendor Emeditech Plus LLP.");
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
            if (_bypassHardwareCheck)
            {
                // Dev bypass — skip hardware comparison
            }
            else
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
