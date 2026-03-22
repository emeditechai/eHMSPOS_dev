-- ============================================================
-- 113_CreateLicenseTables.sql
-- Creates local licensing tables in HMS_Dev that mirror the
-- structure of the remote Central_Lic_DB licensing tables.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[ClientAppLicense]') AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[ClientAppLicense] (
        [Id]                BIGINT          IDENTITY(1,1)   NOT NULL,
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
        [ProductType]       VARCHAR(100)    NULL,
        CONSTRAINT [PK_ClientAppLicense] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Table ClientAppLicense created.';
END
ELSE
BEGIN
    PRINT 'Table ClientAppLicense already exists.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[LicenseValidationHistory]') AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[LicenseValidationHistory] (
        [Id]                BIGINT          IDENTITY(1,1)   NOT NULL,
        [ClientCode]        VARCHAR(50)     NULL,
        [LicenseKey]        NVARCHAR(100)   NULL,
        [IsValid]           BIT             NOT NULL DEFAULT 0,
        [FailureReason]     VARCHAR(500)    NULL,
        [PublicIPAddress]   VARCHAR(100)    NULL,
        [DeviceInfo]        NVARCHAR(1000)  NULL,
        [CreatedAt]         DATETIME        NOT NULL DEFAULT GETDATE(),
        [AppUrl]            VARCHAR(500)    NULL,
        [ProductType]       VARCHAR(100)    NULL,
        CONSTRAINT [PK_LicenseValidationHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Table LicenseValidationHistory created.';
END
ELSE
BEGIN
    PRINT 'Table LicenseValidationHistory already exists.';
END
GO
