using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;

namespace HotelApp.Web.Controllers;

/// <summary>
/// Handles the application licensing flow:
/// - Registration page (first-run)
/// - OTP dispatch and verification
/// - License expired / hardware invalid information pages
/// </summary>
public class LicenseController : Controller
{
    private const string ProductType         = "eLuxstay";
    private const string PendingRegKey       = "PendingRegData";

    private readonly ILicenseRepository         _licenseRepo;
    private readonly IRemoteLicenseRepository   _remoteRepo;
    private readonly IHardwareInfoService       _hwService;
    private readonly ILicenseOtpService         _otpService;
    private readonly ILogger<LicenseController> _logger;
    private readonly IMemoryCache               _cache;
    private readonly IConfiguration             _config;
    private readonly IPublicIpService           _publicIpService;

    public LicenseController(
        ILicenseRepository         licenseRepo,
        IRemoteLicenseRepository   remoteRepo,
        IHardwareInfoService       hwService,
        ILicenseOtpService         otpService,
        ILogger<LicenseController> logger,
        IMemoryCache               cache,
        IConfiguration             config,
        IPublicIpService           publicIpService)
    {
        _licenseRepo     = licenseRepo;
        _remoteRepo      = remoteRepo;
        _hwService       = hwService;
        _otpService      = otpService;
        _logger          = logger;
        _cache           = cache;
        _config          = config;
        _publicIpService = publicIpService;
    }

    // ── GET /License/Register ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var existing = await _licenseRepo.GetActiveLicenseAsync();
        if (existing != null && existing.OTP_Verified)
        {
            // Only skip registration if this license belongs to the current server.
            // If the AppUrl differs this is a new server sharing the same DB — let
            // the user register fresh instead of looping back to Login.
            var currentAppUrl = $"{Request.Scheme}://{Request.Host}";
            if (string.Equals(existing.AppUrl, currentAppUrl, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Login", "Account");
        }

        return View();
    }

    // ── GET /License/GetHardwareInfo ──────────────────────────────────────────
    // AJAX — returns hardware fingerprint only; ClientCode/LicenseKey are not
    // generated until after OTP is verified successfully.

    [HttpGet]
    public IActionResult GetHardwareInfo()
    {
        try
        {
            var hw      = _hwService.GetHardwareInfo();
            var appUrl  = $"{Request.Scheme}://{Request.Host}";

            return Json(new
            {
                success           = true,
                macId             = hw.MacId,
                hardDiskSerial    = hw.HardDiskSerial,
                motherboardSerial = hw.MotherboardSerial,
                appUrl,
                startDate         = DateTime.Today.ToString("yyyy-MM-dd")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch hardware info.");
            return Json(new { success = false, message = "Unable to read hardware identifiers." });
        }
    }

    // ── POST /License/SendOtp ─────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp([FromForm] LicenseRegistrationInput input)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        // Capture hardware server-side — never trust client values
        var hw       = _hwService.GetHardwareInfo();
        var appUrl   = $"{Request.Scheme}://{Request.Host}";
        var publicIp = await _publicIpService.ResolveAsync(HttpContext);

        // Use a temporary token as the OTP-table identifier.
        // The real ClientCode + LicenseKey are generated only after OTP passes.
        var tempToken = Guid.NewGuid().ToString("N").ToUpperInvariant();

        // Persist registration data in session keyed by temp token
        var pending = new PendingRegistration
        {
            TempToken         = tempToken,
            ClientName        = input.ClientName.Trim(),
            ContactNumber     = input.ContactNumber?.Trim(),
            EmailID           = input.EmailID?.Trim(),
            ExpiryDate        = input.ExpiryDate,
            AMC_Expireddate   = input.AMC_Expireddate,
            MacId             = hw.MacId,
            HardDiskSerial    = hw.HardDiskSerial,
            MotherboardSerial = hw.MotherboardSerial,
            AppUrl            = appUrl,
            PublicIPAddress   = publicIp
        };

        HttpContext.Session.SetString(PendingRegKey, JsonSerializer.Serialize(pending));

        var (success, message) = await _otpService.SendOtpAsync(tempToken, input.EmailID ?? string.Empty);
        return Json(new { success, message });
    }

    // ── POST /License/VerifyOtp ───────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp([FromForm] string otp)
    {
        var pendingJson = HttpContext.Session.GetString(PendingRegKey);
        if (string.IsNullOrWhiteSpace(pendingJson))
            return Json(new { success = false, message = "Session expired. Please restart the registration." });

        PendingRegistration? pending;
        try   { pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson); }
        catch { pending = null; }

        if (pending?.TempToken == null)
            return Json(new { success = false, message = "Session data not found. Please restart the registration." });

        // Validate OTP using the temp token
        var (valid, message) = await _otpService.ValidateOtpAsync(pending.TempToken, otp?.Trim() ?? string.Empty);
        if (!valid)
            return Json(new { success = false, message });

        // ── OTP passed → now generate ClientCode + LicenseKey ────────────────
        var fyPrefix   = BuildClientCodePrefix();
        var localSeq   = await _licenseRepo.GetNextSequenceAsync(fyPrefix);
        var remoteSeq  = await _remoteRepo.GetNextSequenceAsync(fyPrefix);
        var seq        = Math.Max(localSeq, remoteSeq);
        var clientCode = $"{fyPrefix}{seq:D4}";
        var licenseKey = Guid.NewGuid().ToString().ToUpperInvariant();

        var license = new ClientAppLicense
        {
            ClientCode        = clientCode,
            ClientName        = pending.ClientName,
            ContactNumber     = pending.ContactNumber,
            EmailID           = pending.EmailID,
            LicenseKey        = licenseKey,
            ServerMacID       = pending.MacId,
            HardDiskNumber    = pending.HardDiskSerial,
            MotherboardNumber = pending.MotherboardSerial,
            Startdate         = DateTime.Today,
            ExpiryDate        = pending.ExpiryDate,
            AMC_Expireddate   = pending.AMC_Expireddate,
            AppUrl            = pending.AppUrl,
            ProductType       = ProductType,
            PublicIPAddress   = pending.PublicIPAddress,
            ConnectionString  = _config.GetConnectionString("DefaultConnection"),
            IsActive          = true,
            OTP_Verified      = true
        };

        // ── Save to REMOTE DB first (authoritative central registry) ─────────
        // If remote fails the exception propagates here with the real SQL message
        // so we return an error to the user — no local record is created,
        // preventing local/remote getting out of sync.
        try
        {
            await _remoteRepo.SaveLicenseAsync(license);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remote license save failed for {ClientCode}: {Message}", clientCode, ex.Message);
            return Json(new { success = false, message = $"Failed to register license on central server: {ex.Message}. Please try again or contact support." });
        }

        // ── Save to local DB ───────────────────────────────────────────────────
        var localSaved = await _licenseRepo.SaveLicenseAsync(license);
        if (!localSaved)
            return Json(new { success = false, message = "Failed to save license locally. Please contact support." });
       // Send welcome email to the registrant — best effort, does not block
       try
       {
           await _otpService.SendWelcomeEmailAsync(license);
       }
       catch (Exception ex)
       {
           _logger.LogWarning(ex, "Welcome email failed for {ClientCode} — non-critical.", clientCode);
       }
        // Clear pending session data
        HttpContext.Session.Remove(PendingRegKey);

        _logger.LogInformation("License registered successfully for client {ClientCode}.", clientCode);
        return Json(new { success = true, clientCode, licenseKey, redirectUrl = "/Account/Login" });
    }

    // ── POST /License/ResendOtp ───────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var pendingJson = HttpContext.Session.GetString(PendingRegKey);
        if (string.IsNullOrWhiteSpace(pendingJson))
            return Json(new { success = false, message = "Session expired. Please restart the registration." });

        PendingRegistration? pending;
        try   { pending = JsonSerializer.Deserialize<PendingRegistration>(pendingJson); }
        catch { pending = null; }

        if (pending?.TempToken == null)
            return Json(new { success = false, message = "Session data not found." });

        var (success, message) = await _otpService.SendOtpAsync(pending.TempToken, pending.EmailID ?? string.Empty);
        return Json(new { success, message });
    }

    // ── POST /License/InitHardwareRenewal ─────────────────────────────────────────
    // Step 1 of hardware re-registration:
    //   ● Looks up the remote record by LicenseKey + current AppUrl
    //   ● Sends OTP to the registered approver
    // On success the caller proceeds to Step 2 (ConfirmHardwareRenewal).

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitHardwareRenewal([FromBody] HardwareRenewalInitRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.LicenseKey))
            return Json(new { success = false, message = "License Key is required." });

        var appUrl = $"{Request.Scheme}://{Request.Host}";

        ClientAppLicense? remote;
        try
        {
            remote = await _remoteRepo.GetLicenseByKeyAndUrlAsync(req.LicenseKey.Trim(), appUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitHardwareRenewal: remote server unreachable.");
            return Json(new { success = false, message = "Could not reach the central license server. Please try again." });
        }

        if (remote == null)
            return Json(new { success = false, message = "License Key not found or does not match the current server URL. Contact Vendor Emeditech Plus LLP." });

        if (!remote.IsActive)
            return Json(new { success = false, message = "This license is deactivated. Contact Vendor Emeditech Plus LLP." });

        if (remote.ExpiryDate.HasValue && remote.ExpiryDate.Value.Date < DateTime.Today)
            return Json(new { success = false, message = $"License expired on {remote.ExpiryDate.Value:dd-MMM-yyyy}. Contact Vendor Emeditech Plus LLP." });

        var (ok, msg) = await _otpService.SendOtpAsync(remote.ClientCode!, remote.EmailID ?? string.Empty);
        if (!ok)
            return Json(new { success = false, message = msg });

        _logger.LogInformation("Hardware renewal OTP sent for {ClientCode}.", remote.ClientCode);
        return Json(new { success = true, message = "OTP sent to registered approver. Please enter it below." });
    }

    // ── POST /License/ConfirmHardwareRenewal ───────────────────────────────────
    // Step 2 of hardware re-registration:
    //   1. Re-fetches remote record (LicenseKey + AppUrl)
    //   2. Validates OTP
    //   3. Reads live hardware identifiers
    //   4. Updates hardware in REMOTE table
    //   5. Updates hardware + LicenseKey in LOCAL table (critical — fixes the
    //      mismatch that causes the "License record not found" loop)
    //   6. Inline re-validation against remote to confirm all checks pass
    //   7. Sets daily IMemoryCache so the redirect to Login passes through cleanly

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmHardwareRenewal([FromBody] HardwareRenewalConfirmRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.LicenseKey) || string.IsNullOrWhiteSpace(req.Otp))
            return Json(new { success = false, message = "License Key and OTP are required." });

        var appUrl     = $"{Request.Scheme}://{Request.Host}";
        var licenseKey = req.LicenseKey.Trim();

        // Re-fetch remote — re-validates that LicenseKey + AppUrl still match
        ClientAppLicense? remote;
        try
        {
            remote = await _remoteRepo.GetLicenseByKeyAndUrlAsync(licenseKey, appUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmHardwareRenewal: remote server unreachable.");
            return Json(new { success = false, message = "Could not reach the central license server. Please try again." });
        }

        if (remote?.ClientCode == null)
            return Json(new { success = false, message = "License Key not found. Please restart the process." });

        // Validate OTP
        var (valid, otpMsg) = await _otpService.ValidateOtpAsync(remote.ClientCode, req.Otp.Trim());
        if (!valid)
            return Json(new { success = false, message = otpMsg });

        // Read live hardware
        HardwareInfo hw;
        try   { hw = _hwService.GetHardwareInfo(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmHardwareRenewal: hardware read failed.");
            return Json(new { success = false, message = "Could not read hardware identifiers from this server. Please try again." });
        }

        // ── Update REMOTE hardware ──────────────────────────────────────────
        try
        {
            await _remoteRepo.UpdateHardwareAsync(remote.ClientCode, hw.MacId, hw.HardDiskSerial, hw.MotherboardSerial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmHardwareRenewal: remote hardware update failed for {ClientCode}.", remote.ClientCode);
            return Json(new { success = false, message = "Failed to update hardware on the central server. Please try again." });
        }

        // ── Update LOCAL hardware + LicenseKey ──────────────────────────────
        // Both must be updated so the middleware's next lookup
        // (GetLicenseForValidationAsync by ClientCode + LicenseKey) resolves correctly.
        try
        {
            await _licenseRepo.UpdateHardwareAsync(remote.ClientCode, hw.MacId, hw.HardDiskSerial, hw.MotherboardSerial);
            await _licenseRepo.UpdateLicenseKeyAsync(remote.ClientCode, licenseKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConfirmHardwareRenewal: local update partial for {ClientCode} — remote is authoritative.", remote.ClientCode);
            // Continue: remote is updated; cache will be cleared so middleware re-syncs on next pass
        }

        // ── Inline re-validation: confirm the updated remote record now passes all rules ──
        // Fetch the freshly-updated record (contains the new hardware values)
        ClientAppLicense? freshRemote = null;
        try
        {
            freshRemote = await _remoteRepo.GetLicenseForValidationAsync(remote.ClientCode, licenseKey, appUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ConfirmHardwareRenewal: post-update re-fetch failed — failing safely.");
            return Json(new { success = false, message = "Hardware updated but could not re-validate with central server. Please retry in a moment." });
        }

        if (freshRemote == null)
            return Json(new { success = false, message = "Hardware updated but record not found on re-validation. Please contact Vendor Emeditech Plus LLP." });

        var reErrors = new List<string>();

        if (!freshRemote.IsActive)
            reErrors.Add("Client deactivated. Contact Vendor Emeditech Plus LLP.");

        if (freshRemote.ExpiryDate.HasValue && freshRemote.ExpiryDate.Value.Date < DateTime.Today)
            reErrors.Add($"License expired on {freshRemote.ExpiryDate.Value:dd-MMM-yyyy}. Contact Vendor for Renewal.");

        if (!_config.GetValue<bool>("Licensing:BypassHardwareCheck"))
        {
            var macOk = string.Equals(hw.MacId,             freshRemote.ServerMacID,       StringComparison.OrdinalIgnoreCase);
            var hddOk = string.Equals(hw.HardDiskSerial,    freshRemote.HardDiskNumber,    StringComparison.OrdinalIgnoreCase);
            var mbOk  = string.Equals(hw.MotherboardSerial, freshRemote.MotherboardNumber, StringComparison.OrdinalIgnoreCase);

            if (!macOk) reErrors.Add("MAC Address still mismatched after update. Contact Vendor Emeditech Plus LLP.");
            if (!hddOk) reErrors.Add("Hard Disk Serial still mismatched after update. Contact Vendor Emeditech Plus LLP.");
            if (!mbOk)  reErrors.Add("Motherboard ID still mismatched after update. Contact Vendor Emeditech Plus LLP.");
        }

        if (reErrors.Count > 0)
            return Json(new { success = false, message = string.Join(" | ", reErrors) });

        // ── All passed: set cache so the redirect to Login goes through cleanly ──
        _cache.Remove($"LicValidated_{remote.ClientCode}");
        _cache.Set($"LicValidated_{remote.ClientCode}",
            DateOnly.FromDateTime(DateTime.Today),
            new MemoryCacheEntryOptions { AbsoluteExpiration = DateTime.Today.AddDays(1) });

        _logger.LogInformation(
            "Hardware re-registered and re-validated for {ClientCode}. MAC={Mac}, HDD={Hdd}, MB={Mb}.",
            remote.ClientCode, hw.MacId, hw.HardDiskSerial, hw.MotherboardSerial);

        // Notify client via email — best effort, does not block
        try
        {
            await _otpService.SendHardwareRenewalNotificationAsync(
                remote.ClientCode,
                remote.ClientName ?? remote.ClientCode,
                remote.EmailID   ?? string.Empty,
                appUrl,
                hw.MacId, hw.HardDiskSerial, hw.MotherboardSerial);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hardware renewal notification failed for {ClientCode} — non-critical.", remote.ClientCode);
        }

        return Json(new { success = true });
    }

    // ── POST /License/ReValidate ───────────────────────────────────────────────
    // AJAX endpoint called from the Login page "Re-validate License" link.
    // Validates against the remote server and logs the result to BOTH the local
    // and remote LicenseValidationHistory tables in every code path.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReValidate()
    {
        // ── Load local license (cannot log without it — return immediately) ──
        ClientAppLicense? localLicense;
        try
        {
            localLicense = await _licenseRepo.GetActiveLicenseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReValidate: failed to load local license.");
            return Json(new { success = false, errors = new[] { "Could not read local license. Please try again." } });
        }

        if (localLicense == null)
            return Json(new { success = false, errors = new[] { "No active license found on this server." } });

        // Clear daily cache so any subsequent middleware check re-validates too
        if (localLicense.ClientCode != null)
            _cache.Remove($"LicValidated_{localLicense.ClientCode}");

        var appUrl   = $"{Request.Scheme}://{Request.Host}";
        var publicIp = await _publicIpService.ResolveAsync(HttpContext);

        // ── Read live hardware early — always needed for DeviceInfo ──────────
        HardwareInfo? hw = null;
        string deviceInfo;
        try
        {
            hw = _hwService.GetHardwareInfo();
            deviceInfo = $"Host={Environment.MachineName};" +
                         $"MAC={hw.MacId};HardDisk={hw.HardDiskSerial};MB={hw.MotherboardSerial}";
        }
        catch
        {
            deviceInfo = $"Host={Environment.MachineName};HardwareReadFailed=true";
        }

        // ── Fetch remote record ──────────────────────────────────────────────
        ClientAppLicense? remote = null;
        bool remoteReachable = true;
        try
        {
            remote = await _remoteRepo.GetLicenseForValidationAsync(
                localLicense.ClientCode!, localLicense.LicenseKey!, appUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReValidate: remote server unreachable.");
            remoteReachable = false;
        }

        // ── If remote unreachable: fail-open, log locally only ───────────────
        if (!remoteReachable)
        {
            var unreachableEntry = new LicenseValidationHistory
            {
                ClientCode      = localLicense.ClientCode,
                LicenseKey      = localLicense.LicenseKey,
                IsValid         = true,
                FailureReason   = null,
                PublicIPAddress = publicIp,
                DeviceInfo      = deviceInfo + ";RemoteUnreachable=true",
                AppUrl          = appUrl,
                ProductType     = localLicense.ProductType ?? "eLuxstay"
            };
            await _licenseRepo.LogValidationAsync(unreachableEntry);
            // Remote is down — cannot log there, proceed with last known state
            return Json(new { success = true, warning = "Remote license server is currently unreachable. Proceeding with last known validation." });
        }

        // ── Collect validation errors ────────────────────────────────────────
        var errors = new List<string>();

        if (remote == null)
            errors.Add("License record not found in the central server. Contact Vendor Emeditech Plus LLP.");

        if (remote != null)
        {
            if (!remote.IsActive)
                errors.Add("Client deactivated. Contact Vendor Emeditech Plus LLP.");

            if (remote.ExpiryDate.HasValue && remote.ExpiryDate.Value.Date < DateTime.Today)
                errors.Add($"Software Expired on {remote.ExpiryDate.Value:dd-MMM-yyyy}. Contact Vendor for Renewal.");

            if (!_config.GetValue<bool>("Licensing:BypassHardwareCheck"))
            {
                if (hw == null)
                {
                    errors.Add("Could not read hardware identifiers from this server. Contact Vendor Emeditech Plus LLP.");
                }
                else
                {
                    var macMatch = string.Equals(hw.MacId,             remote.ServerMacID,       StringComparison.OrdinalIgnoreCase);
                    var hddMatch = string.Equals(hw.HardDiskSerial,    remote.HardDiskNumber,    StringComparison.OrdinalIgnoreCase);
                    var mbMatch  = string.Equals(hw.MotherboardSerial, remote.MotherboardNumber, StringComparison.OrdinalIgnoreCase);

                    if (!macMatch) errors.Add("MAC Address Mismatch. Contact Vendor Emeditech Plus LLP.");
                    if (!hddMatch) errors.Add("Hard Disk Serial Mismatch. Contact Vendor Emeditech Plus LLP.");
                    if (!mbMatch)  errors.Add("Motherboard ID Mismatch. Contact Vendor Emeditech Plus LLP.");
                }
            }
        }

        bool isValid = errors.Count == 0;

        // ── Log to LOCAL and REMOTE history tables ───────────────────────────
        // This happens in every path (success AND failure) so both tables always
        // reflect every manual re-validation attempt.
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

        await _licenseRepo.LogValidationAsync(historyEntry);   // local
        await _remoteRepo.LogValidationAsync(historyEntry);    // remote (EnsureHistoryTableAsync called inside)

        if (!isValid)
            return Json(new { success = false, errors });

        // ── All passed: sync, update dates, set cache ────────────────────────
        await _licenseRepo.SyncFromRemoteAsync(localLicense.ClientCode!, remote!);
        await _licenseRepo.UpdateLastLoginDateAsync(localLicense.ClientCode!);
        await _remoteRepo.UpdateLastLoginDateAsync(localLicense.ClientCode!);

        var midnight = DateTime.Today.AddDays(1);
        _cache.Set($"LicValidated_{localLicense.ClientCode}",
            DateOnly.FromDateTime(DateTime.Today),
            new MemoryCacheEntryOptions { AbsoluteExpiration = midnight });

        _logger.LogInformation("License manually re-validated OK for {ClientCode}.", localLicense.ClientCode);
        return Json(new { success = true });
    }

    // ── GET /License/ClearCache ───────────────────────────────────────────────
    // Non-JS fallback: clears cache so LicenseMiddleware re-validates on the
    // next request (which will also sync remote → local on success).

    [HttpGet]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            var license = await _licenseRepo.GetActiveLicenseAsync();
            if (license?.ClientCode != null)
                _cache.Remove($"LicValidated_{license.ClientCode}");
            _logger.LogInformation("License cache cleared (no-JS fallback) for {ClientCode}.", license?.ClientCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing license cache.");
        }
        return Redirect("/Account/Login");
    }

    // ── GET /License/Expired ──────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Expired() => View();

    // ── GET /License/Invalid ──────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Invalid() => View();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the ClientCode prefix using the Indian Financial Year convention.
    /// April–March cycle: March 2026 → FY 2025-26 → prefix "Cl-2526"
    /// </summary>
    private static string BuildClientCodePrefix()
    {
        var now     = DateTime.Today;
        int fyStart = now.Month >= 4 ? now.Year : now.Year - 1;
        int fyEnd   = fyStart + 1;
        return $"Cl-{fyStart % 100:D2}{fyEnd % 100:D2}";
    }
}

// ─── Input models for hardware re-registration ────────────────────────────────

public class HardwareRenewalInitRequest
{
    public string? LicenseKey { get; set; }
}

public class HardwareRenewalConfirmRequest
{
    public string? LicenseKey { get; set; }
    public string? Otp        { get; set; }
}

// ─── Input model for the registration form ────────────────────────────────────

public class LicenseRegistrationInput
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Client Name is required.")]
    public string ClientName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Contact Number is required.")]
    public string ContactNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email ID is required.")]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string EmailID { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Expiry Date is required.")]
    public DateTime ExpiryDate { get; set; }

    public DateTime? AMC_Expireddate { get; set; }
}

// ─── Lightweight session DTO — stores pre-OTP registration data ───────────────

internal sealed class PendingRegistration
{
    public string    TempToken         { get; set; } = string.Empty;
    public string    ClientName        { get; set; } = string.Empty;
    public string?   ContactNumber     { get; set; }
    public string?   EmailID           { get; set; }
    public DateTime  ExpiryDate        { get; set; }
    public DateTime? AMC_Expireddate   { get; set; }
    public string    MacId             { get; set; } = string.Empty;
    public string    HardDiskSerial    { get; set; } = string.Empty;
    public string    MotherboardSerial { get; set; } = string.Empty;
    public string    AppUrl            { get; set; } = string.Empty;
    public string?   PublicIPAddress   { get; set; }
}
