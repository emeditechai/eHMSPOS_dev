using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

/// <summary>
/// Manages all read/write operations against the remote Central_Lic_DB.
/// Connection credentials are intentionally kept here in code (not in
/// appsettings.json) to prevent accidental exposure via config files.
/// </summary>
public class RemoteLicenseRepository : IRemoteLicenseRepository
{
    // ⚠ Central license server — do not expose in appsettings / environment variables
    private const string RemoteConnStr =
        "Server=198.38.81.123;Database=Central_Lic_DB;User Id=sa;Password=asdf@1234;TrustServerCertificate=True;";

    // Ensures the remote LicenseValidationHistory table is created only once per
    // application lifetime, not on every request.
    private static int _historyTableEnsured = 0;

    private readonly ILogger<RemoteLicenseRepository> _logger;

    public RemoteLicenseRepository(ILogger<RemoteLicenseRepository> logger)
    {
        _logger = logger;
    }

    public async Task EnsureHistoryTableAsync()
    {
        // Run once per app lifetime — cheap after first call
        if (Interlocked.CompareExchange(ref _historyTableEnsured, 1, 0) != 0)
            return;

        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.objects
                    WHERE  object_id = OBJECT_ID(N'[dbo].[LicenseValidationHistory]')
                      AND  type = 'U'
                )
                BEGIN
                    CREATE TABLE [dbo].[LicenseValidationHistory] (
                        [Id]              BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ClientCode]      VARCHAR(50)     NULL,
                        [LicenseKey]      NVARCHAR(100)   NULL,
                        [IsValid]         BIT             NOT NULL DEFAULT 0,
                        [FailureReason]   VARCHAR(500)    NULL,
                        [PublicIPAddress] VARCHAR(100)    NULL,
                        [DeviceInfo]      NVARCHAR(1000)  NULL,
                        [CreatedAt]       DATETIME        NOT NULL DEFAULT GETDATE(),
                        [AppUrl]          VARCHAR(500)    NULL,
                        [ProductType]     VARCHAR(100)    NULL
                    );
                END");

            _logger.LogInformation("Remote LicenseValidationHistory table verified/created.");
        }
        catch (Exception ex)
        {
            // Reset flag so the next request retries
            Interlocked.Exchange(ref _historyTableEnsured, 0);
            _logger.LogError(ex, "Failed to ensure remote LicenseValidationHistory table.");
        }
    }

    public async Task<bool> SaveLicenseAsync(ClientAppLicense lic)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            // Core INSERT — does NOT include ConnectionString so it works regardless
            // of whether the remote table has that column yet.
            const string sql = @"
                INSERT INTO ClientAppLicense
                    (ClientCode, ClientName, ContactNumber, LicenseKey,
                     HardDiskNumber, ServerMacID, MotherboardNumber,
                     Startdate, ExpiryDate, IsActive, CreatedAt, OTP_Verified,
                     PublicIPAddress, EmailID, AMC_Expireddate, appurl, ProductType)
                VALUES
                    (@ClientCode, @ClientName, @ContactNumber, @LicenseKey,
                     @HardDiskNumber, @ServerMacID, @MotherboardNumber,
                     @Startdate, @ExpiryDate, 1, GETDATE(), 1,
                     @PublicIPAddress, @EmailID, @AMC_Expireddate, @AppUrl, @ProductType)";

            var inserted = await conn.ExecuteAsync(sql, lic) > 0;

            // Best-effort: store ConnectionString if the column exists on the remote table.
            if (inserted && !string.IsNullOrWhiteSpace(lic.ConnectionString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        "UPDATE ClientAppLicense SET ConnectionString = @cs WHERE ClientCode = @cc",
                        new { cs = lic.ConnectionString, cc = lic.ClientCode });
                }
                catch
                {
                    // Column may not exist on the remote schema yet — non-critical.
                }
            }

            return inserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save license to remote DB for {ClientCode}.", lic.ClientCode);
            return false;
        }
    }

    public async Task LogValidationAsync(LicenseValidationHistory history)
    {
        try
        {
            // Always ensure the history table exists before inserting.
            // EnsureHistoryTableAsync uses a static flag so DDL only runs once
            // per lifetime; subsequent calls are O(1). This guards against the
            // race where LogValidationAsync is called before EnsureHistoryTableAsync
            // has completed (or if it failed on the very first request).
            await EnsureHistoryTableAsync();

            await using var conn = new SqlConnection(RemoteConnStr);

            await conn.ExecuteAsync(@"
                INSERT INTO LicenseValidationHistory
                    (ClientCode, LicenseKey, IsValid, FailureReason,
                     PublicIPAddress, DeviceInfo, CreatedAt, AppUrl, ProductType)
                VALUES
                    (@ClientCode, @LicenseKey, @IsValid, @FailureReason,
                     @PublicIPAddress, @DeviceInfo, GETDATE(), @AppUrl, @ProductType)",
                history);
        }
        catch (Exception ex)
        {
            // Log as ERROR (not warning) so this failure is always visible in logs.
            _logger.LogError(ex, "Remote LicenseValidationHistory insert failed for {ClientCode}. IsValid={IsValid}, Reason={Reason}.",
                history.ClientCode, history.IsValid, history.FailureReason);
        }
    }

    public async Task UpdateLastLoginDateAsync(string clientCode)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            await conn.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET    LastLoginDate = GETDATE()
                WHERE  ClientCode   = @ClientCode",
                new { ClientCode = clientCode });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote LastLoginDate update failed (non-blocking) for {ClientCode}.", clientCode);
        }
    }

    public async Task<int> GetNextSequenceAsync(string fyPrefix)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            var maxCode = await conn.ExecuteScalarAsync<string?>(@"
                SELECT TOP 1 ClientCode
                FROM   ClientAppLicense
                WHERE  ClientCode LIKE @Pattern
                ORDER BY Id DESC",
                new { Pattern = fyPrefix + "%" });

            if (maxCode != null && maxCode.Length > fyPrefix.Length)
            {
                var suffix = maxCode[fyPrefix.Length..];
                if (int.TryParse(suffix, out var seq))
                    return seq + 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query remote sequence for prefix {Prefix}.", fyPrefix);
        }

        return 1;
    }

    public async Task<bool> IsLicenseActiveAsync(string licenseKey)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            var count = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM   ClientAppLicense
                WHERE  LicenseKey = @LicenseKey
                  AND  IsActive   = 1
                  AND  OTP_Verified = 1
                  AND  (ExpiryDate IS NULL OR ExpiryDate >= GETDATE())",
                new { LicenseKey = licenseKey });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote license active-check failed for key.");
            return true; // fail-open on remote connectivity; local check is authoritative
        }
    }

    public async Task<ClientAppLicense?> GetLicenseForValidationAsync(string clientCode, string licenseKey, string appUrl)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);

            // AppUrl is included in the WHERE clause so that a license registered
            // for one server URL cannot validate on a different URL.
            return await conn.QueryFirstOrDefaultAsync<ClientAppLicense>(@"
                SELECT ClientCode, LicenseKey, HardDiskNumber, ServerMacID,
                       MotherboardNumber, ExpiryDate, IsActive
                FROM   ClientAppLicense
                WHERE  ClientCode        = @ClientCode
                  AND  LicenseKey        = @LicenseKey
                  AND  LOWER(AppUrl)     = LOWER(@AppUrl)",
                new { ClientCode = clientCode, LicenseKey = licenseKey, AppUrl = appUrl });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote GetLicenseForValidation failed for {ClientCode}.", clientCode);
            return null; // caller handles null as cannot-connect
        }
    }

    public async Task<ClientAppLicense?> GetLicenseByKeyAndUrlAsync(string licenseKey, string appUrl)
    {
        // No try/catch — caller must distinguish "not found" (null) from "exception" (throw)
        await using var conn = new SqlConnection(RemoteConnStr);
        return await conn.QueryFirstOrDefaultAsync<ClientAppLicense>(@"
            SELECT ClientCode, ClientName, EmailID, LicenseKey, IsActive, ExpiryDate
            FROM   ClientAppLicense
            WHERE  LicenseKey    = @LicenseKey
              AND  LOWER(AppUrl) = LOWER(@AppUrl)
              AND  OTP_Verified  = 1",
            new { LicenseKey = licenseKey, AppUrl = appUrl });
    }

    public async Task UpdateHardwareAsync(string clientCode, string macId, string hddSerial, string mbSerial)
    {
        try
        {
            await using var conn = new SqlConnection(RemoteConnStr);
            await conn.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET    ServerMacID       = @MacId,
                       HardDiskNumber    = @HddSerial,
                       MotherboardNumber = @MbSerial
                WHERE  ClientCode = @ClientCode",
                new { ClientCode = clientCode, MacId = macId, HddSerial = hddSerial, MbSerial = mbSerial });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remote UpdateHardware failed for {ClientCode}.", clientCode);
            throw;
        }
    }
}
