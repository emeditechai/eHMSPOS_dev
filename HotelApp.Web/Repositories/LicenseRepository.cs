using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly IDbConnection _db;

    public LicenseRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task EnsureTablesExistAsync()
    {
        // ClientAppLicense
        const string createLicense = @"
            IF NOT EXISTS (
                SELECT 1 FROM sys.objects
                WHERE object_id = OBJECT_ID(N'[dbo].[ClientAppLicense]') AND type = 'U'
            )
            BEGIN
                CREATE TABLE [dbo].[ClientAppLicense] (
                    [Id]                BIGINT          IDENTITY(1,1)   NOT NULL PRIMARY KEY,
                    [ClientCode]        VARCHAR(50)     NULL,
                    [ClientName]        VARCHAR(200)    NULL,
                    [ContactNumber]     VARCHAR(20)     NULL,
                    [LicenseKey]        NVARCHAR(100)   NULL,
                    [HardDiskNumber]    VARCHAR(200)    NULL,
                    [ServerMacID]       NVARCHAR(200)   NULL,
                    [MotherboardNumber] NVARCHAR(200)   NULL,
                    [Startdate]         DATETIME        NULL,
                    [ExpiryDate]        DATETIME        NULL,
                    [IsActive]          BIT             NOT NULL DEFAULT 1,
                    [CreatedAt]         DATETIME        NOT NULL DEFAULT GETDATE(),
                    [OTP_Verified]      BIT             NOT NULL DEFAULT 0,
                    [PublicIPAddress]   VARCHAR(100)    NULL,
                    [LastLoginDate]     DATETIME        NULL,
                    [EmailID]           VARCHAR(200)    NULL,
                    [AMC_Expireddate]   DATETIME        NULL,
                    [AppUrl]            VARCHAR(500)    NULL,
                    [ProductType]       VARCHAR(100)    NULL
                );
            END";

        // LicenseValidationHistory
        const string createHistory = @"
            IF NOT EXISTS (
                SELECT 1 FROM sys.objects
                WHERE object_id = OBJECT_ID(N'[dbo].[LicenseValidationHistory]') AND type = 'U'
            )
            BEGIN
                CREATE TABLE [dbo].[LicenseValidationHistory] (
                    [Id]                BIGINT          IDENTITY(1,1)   NOT NULL PRIMARY KEY,
                    [ClientCode]        VARCHAR(50)     NULL,
                    [LicenseKey]        NVARCHAR(100)   NULL,
                    [IsValid]           BIT             NOT NULL DEFAULT 0,
                    [FailureReason]     VARCHAR(500)    NULL,
                    [PublicIPAddress]   VARCHAR(100)    NULL,
                    [DeviceInfo]        NVARCHAR(1000)  NULL,
                    [CreatedAt]         DATETIME        NOT NULL DEFAULT GETDATE(),
                    [AppUrl]            VARCHAR(500)    NULL,
                    [ProductType]       VARCHAR(100)    NULL
                );
            END";

        await _db.ExecuteAsync(createLicense);
        await _db.ExecuteAsync(createHistory);

        // Add any columns that may be missing from tables created by older migration scripts
        var alterColumns = new[]
        {
            "IF COL_LENGTH('dbo.ClientAppLicense','LastLoginDate') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [LastLoginDate] DATETIME NULL",
            "IF COL_LENGTH('dbo.ClientAppLicense','EmailID') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [EmailID] VARCHAR(200) NULL",
            "IF COL_LENGTH('dbo.ClientAppLicense','AMC_Expireddate') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [AMC_Expireddate] DATETIME NULL",
            "IF COL_LENGTH('dbo.ClientAppLicense','AppUrl') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [AppUrl] VARCHAR(500) NULL",
            "IF COL_LENGTH('dbo.ClientAppLicense','ProductType') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [ProductType] VARCHAR(100) NULL",
            "IF COL_LENGTH('dbo.ClientAppLicense','PublicIPAddress') IS NULL ALTER TABLE dbo.ClientAppLicense ADD [PublicIPAddress] VARCHAR(100) NULL",
        };
        foreach (var ddl in alterColumns)
        {
            try { await _db.ExecuteAsync(ddl); } catch { /* non-critical */ }
        }
    }

    public async Task<ClientAppLicense?> GetActiveLicenseAsync(string? appUrl = null)
    {
        try
        {
            // If a URL is provided, prefer the record registered for that URL.
            // This handles the case where the local DB has licenses for multiple
            // deployments (e.g. dev Mac + prod server share the same HMS_Dev DB).
            if (!string.IsNullOrEmpty(appUrl))
            {
                var byUrl = await _db.QueryFirstOrDefaultAsync<ClientAppLicense>(@"
                    SELECT TOP 1 *
                    FROM   ClientAppLicense
                    WHERE  IsActive     = 1
                      AND  OTP_Verified = 1
                      AND  LOWER(AppUrl) = LOWER(@AppUrl)
                    ORDER BY Id DESC",
                    new { AppUrl = appUrl });
                if (byUrl != null) return byUrl;
            }
            // Fall back ONLY to legacy records that predate AppUrl tracking (AppUrl is empty/null).
            // Records with a different populated AppUrl belong to another registered deployment —
            // returning them here would allow an unregistered URL to bypass the registration check.
            return await _db.QueryFirstOrDefaultAsync<ClientAppLicense>(@"
                SELECT TOP 1 *
                FROM   ClientAppLicense
                WHERE  IsActive     = 1
                  AND  OTP_Verified = 1
                  AND  (AppUrl IS NULL OR AppUrl = '')
                ORDER BY Id DESC");
        }
        catch
        {
            // Table may not exist on first run — return null so middleware redirects to Register.
            return null;
        }
    }

    public async Task<bool> SaveLicenseAsync(ClientAppLicense lic)
    {
        const string sql = @"
            INSERT INTO ClientAppLicense
                (ClientCode, ClientName, ContactNumber, LicenseKey,
                 HardDiskNumber, ServerMacID, MotherboardNumber,
                 Startdate, ExpiryDate, IsActive, CreatedAt, OTP_Verified,
                 PublicIPAddress, EmailID, AMC_Expireddate, AppUrl, ProductType)
            VALUES
                (@ClientCode, @ClientName, @ContactNumber, @LicenseKey,
                 @HardDiskNumber, @ServerMacID, @MotherboardNumber,
                 @Startdate, @ExpiryDate, 1, GETDATE(), 1,
                 @PublicIPAddress, @EmailID, @AMC_Expireddate, @AppUrl, @ProductType)";

        return await _db.ExecuteAsync(sql, lic) > 0;
    }

    public async Task<bool> HasValidationTodayAsync(string clientCode)
    {
        try
        {
            var count = await _db.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM   LicenseValidationHistory
                WHERE  ClientCode = @ClientCode
                  AND  IsValid    = 1
                  AND  CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)",
                new { ClientCode = clientCode });

            return count > 0;
        }
        catch { return false; }
    }

    public async Task LogValidationAsync(LicenseValidationHistory history)
    {
        try
        {
            await _db.ExecuteAsync(@"
                INSERT INTO LicenseValidationHistory
                    (ClientCode, LicenseKey, IsValid, FailureReason,
                     PublicIPAddress, DeviceInfo, CreatedAt, AppUrl, ProductType)
                VALUES
                    (@ClientCode, @LicenseKey, @IsValid, @FailureReason,
                     @PublicIPAddress, @DeviceInfo, GETDATE(), @AppUrl, @ProductType)",
                history);
        }
        catch { /* best-effort */ }
    }

    public async Task<int> GetNextSequenceAsync(string fyPrefix)
    {
        // Pattern: ClientCode = 'Cl-2526NNNN' so we look for max sequence under this prefix.
        try
        {
            var maxCode = await _db.ExecuteScalarAsync<string?>(@"
                SELECT TOP 1 ClientCode
                FROM  ClientAppLicense
                WHERE ClientCode LIKE @Pattern
                ORDER BY Id DESC",
                new { Pattern = fyPrefix + "%" });

            if (maxCode != null && maxCode.Length > fyPrefix.Length)
            {
                var suffix = maxCode[fyPrefix.Length..];
                if (int.TryParse(suffix, out var seq))
                    return seq + 1;
            }
        }
        catch { /* fall through */ }

        return 1;
    }

    public async Task UpdateLastLoginDateAsync(string clientCode)
    {
        try
        {
            await _db.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET    LastLoginDate = GETDATE()
                WHERE  ClientCode   = @ClientCode",
                new { ClientCode = clientCode });
        }
        catch { /* non-critical — best effort */ }
    }

    public async Task UpdateHardwareAsync(string clientCode, string macId, string hddSerial, string mbSerial)
    {
        try
        {
            await _db.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET    ServerMacID       = @MacId,
                       HardDiskNumber    = @HddSerial,
                       MotherboardNumber = @MbSerial
                WHERE  ClientCode = @ClientCode",
                new { ClientCode = clientCode, MacId = macId, HddSerial = hddSerial, MbSerial = mbSerial });
        }
        catch (Exception ex)
        {
            // Log and rethrow so caller can report failure to the user
            throw new InvalidOperationException($"Local hardware update failed for {clientCode}.", ex);
        }
    }

    public async Task UpdateLicenseKeyAsync(string clientCode, string newLicenseKey)
    {
        try
        {
            await _db.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET    LicenseKey = @LicenseKey
                WHERE  ClientCode = @ClientCode",
                new { ClientCode = clientCode, LicenseKey = newLicenseKey });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Local LicenseKey update failed for {clientCode}.", ex);
        }
    }

    public async Task SyncFromRemoteAsync(string clientCode, ClientAppLicense remote)
    {
        try
        {
            await _db.ExecuteAsync(@"
                UPDATE ClientAppLicense
                SET  ClientName       = @ClientName,
                     ContactNumber    = @ContactNumber,
                     LicenseKey       = @LicenseKey,
                     HardDiskNumber   = @HardDiskNumber,
                     ServerMacID      = @ServerMacID,
                     MotherboardNumber = @MotherboardNumber,
                     ExpiryDate       = @ExpiryDate,
                     IsActive         = @IsActive,
                     AMC_Expireddate  = @AMC_Expireddate
                WHERE ClientCode = @ClientCode",
                new
                {
                    ClientCode        = clientCode,
                    remote.ClientName,
                    remote.ContactNumber,
                    remote.LicenseKey,
                    remote.HardDiskNumber,
                    remote.ServerMacID,
                    remote.MotherboardNumber,
                    remote.ExpiryDate,
                    remote.IsActive,
                    remote.AMC_Expireddate
                });
        }
        catch { /* non-critical — best effort */ }
    }
}
