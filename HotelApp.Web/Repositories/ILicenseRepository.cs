using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface ILicenseRepository
{
    /// <summary>Returns the active, OTP-verified license for this installation, or null.</summary>
    Task<ClientAppLicense?> GetActiveLicenseAsync(string? appUrl = null);

    /// <summary>Persists a new license record to the local database.</summary>
    Task<bool> SaveLicenseAsync(ClientAppLicense license);

    /// <summary>
    /// Returns true if a successful (IsValid=1) validation entry already exists
    /// for today so the middleware can skip redundant hardware checks.
    /// </summary>
    Task<bool> HasValidationTodayAsync(string clientCode);

    /// <summary>Appends a row to the local LicenseValidationHistory table.</summary>
    Task LogValidationAsync(LicenseValidationHistory history);

    /// <summary>Returns the next integer sequence for a given FY prefix (e.g. "Cl-2526").</summary>
    Task<int> GetNextSequenceAsync(string fyPrefix);

    /// <summary>Creates the licensing tables if they do not already exist.</summary>
    Task EnsureTablesExistAsync();

    /// <summary>Updates LastLoginDate to GETDATE() for the given ClientCode in the local DB.</summary>
    Task UpdateLastLoginDateAsync(string clientCode);

    /// <summary>
    /// Syncs key fields from the remote record into the local ClientAppLicense row
    /// so that the local copy always reflects the authoritative central-server state.
    /// Fields synced: ClientName, ContactNumber, LicenseKey, HardDiskNumber,
    /// ServerMacID, MotherboardNumber, ExpiryDate, IsActive, AMC_Expireddate.
    /// </summary>
    Task SyncFromRemoteAsync(string clientCode, ClientAppLicense remote);

    /// <summary>Updates ServerMacID, HardDiskNumber, MotherboardNumber in the local table.</summary>
    Task UpdateHardwareAsync(string clientCode, string macId, string hddSerial, string mbSerial);

    /// <summary>Updates LicenseKey in the local table for the given ClientCode.</summary>
    Task UpdateLicenseKeyAsync(string clientCode, string newLicenseKey);

    /// <summary>Returns the active alert message if alerts are enabled and current date/time is within the alert window.</summary>
    Task<string?> GetActiveAlertMessageAsync();
}
