using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IRemoteLicenseRepository
{
    /// <summary>Creates LicenseValidationHistory in the remote DB if absent. Safe to call once at startup.</summary>
    Task EnsureHistoryTableAsync();

    /// <summary>Saves the new client license to the remote Central_Lic_DB.</summary>
    Task<bool> SaveLicenseAsync(ClientAppLicense license);

    /// <summary>Logs a validation event to the remote LicenseValidationHistory.</summary>
    Task LogValidationAsync(LicenseValidationHistory history);

    /// <summary>Updates LastLoginDate for the client in central DB.</summary>
    Task UpdateLastLoginDateAsync(string clientCode);

    /// <summary>Returns the next available sequence number for the given FY prefix.</summary>
    Task<int> GetNextSequenceAsync(string fyPrefix);

    /// <summary>Checks whether a license key is still active and not expired.</summary>
    Task<bool> IsLicenseActiveAsync(string licenseKey);

    /// <summary>
    /// Fetches the full license record from the remote Central_Lic_DB for a given
    /// ClientCode + LicenseKey + AppUrl combination. Returns null if not found or on
    /// connectivity failure (caller treats null as a fail-open).
    /// AppUrl is included so that a license registered for one server URL cannot
    /// pass validation on a different URL.
    /// </summary>
    Task<ClientAppLicense?> GetLicenseForValidationAsync(string clientCode, string licenseKey, string appUrl);

    /// <summary>
    /// Fetches a license from the remote DB by LicenseKey + AppUrl (for hardware
    /// re-registration). Returns null if not found. Throws on connectivity failure
    /// so callers can distinguish "not found" from "server unreachable".
    /// </summary>
    Task<ClientAppLicense?> GetLicenseByKeyAndUrlAsync(string licenseKey, string appUrl);

    /// <summary>Updates ServerMacID, HardDiskNumber, MotherboardNumber in the remote table.</summary>
    Task UpdateHardwareAsync(string clientCode, string macId, string hddSerial, string mbSerial);
}
