-- Migration 141: Add E-Invoicing Configuration columns to HotelSettings
-- =============================================

-- 1. Add EInvoiceMode column (MANUAL or AUTO)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceMode')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceMode NVARCHAR(10) NOT NULL DEFAULT 'MANUAL';
    PRINT 'Added EInvoiceMode to HotelSettings.';
END
GO

-- 2. Add E-Invoicing API configuration columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceApiBaseUrl')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceApiBaseUrl NVARCHAR(500) NULL;
    PRINT 'Added EInvoiceApiBaseUrl to HotelSettings.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceAuthUrl')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceAuthUrl NVARCHAR(500) NULL;
    PRINT 'Added EInvoiceAuthUrl to HotelSettings.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceIrnEndpoint')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceIrnEndpoint NVARCHAR(500) NULL;
    PRINT 'Added EInvoiceIrnEndpoint to HotelSettings.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceClientId')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceClientId NVARCHAR(200) NULL;
    PRINT 'Added EInvoiceClientId to HotelSettings.';
END
GO

-- Encrypted at application layer
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceClientSecret')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceClientSecret NVARCHAR(1000) NULL;
    PRINT 'Added EInvoiceClientSecret to HotelSettings.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoiceUsername')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoiceUsername NVARCHAR(200) NULL;
    PRINT 'Added EInvoiceUsername to HotelSettings.';
END
GO

-- Encrypted at application layer
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'EInvoicePassword')
BEGIN
    ALTER TABLE HotelSettings ADD EInvoicePassword NVARCHAR(1000) NULL;
    PRINT 'Added EInvoicePassword to HotelSettings.';
END
GO

-- 3. sp_GetHotelSettingsByBranch uses SELECT * so no change required.

-- 4. Update sp_UpsertHotelSettings to include e-invoice fields
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'sp_UpsertHotelSettings') AND type = 'P')
BEGIN
    DROP PROCEDURE sp_UpsertHotelSettings;
END
GO

CREATE PROCEDURE sp_UpsertHotelSettings
    @BranchID INT,
    @HotelName NVARCHAR(200),
    @Address NVARCHAR(500),
    @ContactNumber1 NVARCHAR(20),
    @ContactNumber2 NVARCHAR(20) = NULL,
    @EmailAddress NVARCHAR(200),
    @Website NVARCHAR(300) = NULL,
    @GSTCode NVARCHAR(50) = NULL,
    @LogoPath NVARCHAR(500) = NULL,
    @CheckInTime TIME,
    @CheckOutTime TIME,
    @ByPassActualDayRate BIT = 0,
    @DiscountApprovalRequired BIT = 0,
    @MinimumBookingAmountRequired BIT = 0,
    @MinimumBookingAmount DECIMAL(18,2) = NULL,
    @NoShowGraceHours INT = 6,
    @CancellationRefundApprovalThreshold DECIMAL(18,2) = NULL,
    @EnableCancellationPolicy BIT = 1,
    @PoliceStation NVARCHAR(200) = NULL,
    -- E-Invoice parameters
    @EInvoiceMode NVARCHAR(10) = 'MANUAL',
    @EInvoiceApiBaseUrl NVARCHAR(500) = NULL,
    @EInvoiceAuthUrl NVARCHAR(500) = NULL,
    @EInvoiceIrnEndpoint NVARCHAR(500) = NULL,
    @EInvoiceClientId NVARCHAR(200) = NULL,
    @EInvoiceClientSecret NVARCHAR(1000) = NULL,   -- NULL = keep existing encrypted value
    @EInvoiceUsername NVARCHAR(200) = NULL,
    @EInvoicePassword NVARCHAR(1000) = NULL,       -- NULL = keep existing encrypted value
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM HotelSettings WHERE BranchID = @BranchID)
    BEGIN
        UPDATE HotelSettings SET
            HotelName = @HotelName,
            Address = @Address,
            ContactNumber1 = @ContactNumber1,
            ContactNumber2 = @ContactNumber2,
            EmailAddress = @EmailAddress,
            Website = @Website,
            GSTCode = @GSTCode,
            LogoPath = COALESCE(@LogoPath, LogoPath),
            CheckInTime = @CheckInTime,
            CheckOutTime = @CheckOutTime,
            ByPassActualDayRate = @ByPassActualDayRate,
            DiscountApprovalRequired = @DiscountApprovalRequired,
            MinimumBookingAmountRequired = @MinimumBookingAmountRequired,
            MinimumBookingAmount = @MinimumBookingAmount,
            NoShowGraceHours = @NoShowGraceHours,
            CancellationRefundApprovalThreshold = @CancellationRefundApprovalThreshold,
            EnableCancellationPolicy = @EnableCancellationPolicy,
            PoliceStation = @PoliceStation,
            EInvoiceMode = @EInvoiceMode,
            EInvoiceApiBaseUrl = @EInvoiceApiBaseUrl,
            EInvoiceAuthUrl = @EInvoiceAuthUrl,
            EInvoiceIrnEndpoint = @EInvoiceIrnEndpoint,
            EInvoiceClientId = @EInvoiceClientId,
            -- Retain existing encrypted value if NULL is passed (user left field blank)
            EInvoiceClientSecret = COALESCE(@EInvoiceClientSecret, EInvoiceClientSecret),
            EInvoiceUsername = @EInvoiceUsername,
            EInvoicePassword = COALESCE(@EInvoicePassword, EInvoicePassword),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @ModifiedBy
        WHERE BranchID = @BranchID;
    END
    ELSE
    BEGIN
        INSERT INTO HotelSettings (
            BranchID, HotelName, Address, ContactNumber1, ContactNumber2,
            EmailAddress, Website, GSTCode, LogoPath, CheckInTime, CheckOutTime,
            ByPassActualDayRate, DiscountApprovalRequired,
            MinimumBookingAmountRequired, MinimumBookingAmount,
            NoShowGraceHours, CancellationRefundApprovalThreshold,
            EnableCancellationPolicy, PoliceStation,
            EInvoiceMode, EInvoiceApiBaseUrl, EInvoiceAuthUrl, EInvoiceIrnEndpoint,
            EInvoiceClientId, EInvoiceClientSecret, EInvoiceUsername, EInvoicePassword,
            IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            @ByPassActualDayRate, @DiscountApprovalRequired,
            @MinimumBookingAmountRequired, @MinimumBookingAmount,
            @NoShowGraceHours, @CancellationRefundApprovalThreshold,
            @EnableCancellationPolicy, @PoliceStation,
            @EInvoiceMode, @EInvoiceApiBaseUrl, @EInvoiceAuthUrl, @EInvoiceIrnEndpoint,
            @EInvoiceClientId, @EInvoiceClientSecret, @EInvoiceUsername, @EInvoicePassword,
            1, GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );
    END
END
GO

PRINT 'Recreated sp_UpsertHotelSettings with E-Invoice configuration support.';
GO

PRINT 'Migration 141 complete.';
GO
