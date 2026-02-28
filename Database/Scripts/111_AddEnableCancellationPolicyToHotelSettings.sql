-- =============================================
-- Add EnableCancellationPolicy setting to HotelSettings
-- When YES: Cancellation Policy section shown + acceptance required on booking
-- When NO:  Cancellation Policy section shown but checkbox auto-checked/disabled
-- Created: 2026-02-28
-- =============================================

IF COL_LENGTH('dbo.HotelSettings', 'EnableCancellationPolicy') IS NULL
BEGIN
    ALTER TABLE dbo.HotelSettings
    ADD EnableCancellationPolicy BIT NOT NULL CONSTRAINT DF_HotelSettings_EnableCancellationPolicy DEFAULT (1);

    PRINT 'Column EnableCancellationPolicy added to HotelSettings (default YES)';
END
ELSE
BEGIN
    PRINT 'Column EnableCancellationPolicy already exists in HotelSettings';
END
GO

-- Recreate sp_GetHotelSettingsByBranch to include EnableCancellationPolicy
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetHotelSettingsByBranch]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_GetHotelSettingsByBranch];
GO

CREATE PROCEDURE [dbo].[sp_GetHotelSettingsByBranch]
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        BranchID,
        HotelName,
        Address,
        ContactNumber1,
        ContactNumber2,
        EmailAddress,
        Website,
        GSTCode,
        LogoPath,
        CheckInTime,
        CheckOutTime,
        ByPassActualDayRate,
        DiscountApprovalRequired,
        MinimumBookingAmountRequired,
        MinimumBookingAmount,
        NoShowGraceHours,
        CancellationRefundApprovalThreshold,
        EnableCancellationPolicy,
        IsActive,
        CreatedDate,
        CreatedBy,
        LastModifiedDate,
        LastModifiedBy
    FROM HotelSettings
    WHERE BranchID = @BranchID AND IsActive = 1;
END
GO

-- Recreate sp_UpsertHotelSettings to include EnableCancellationPolicy
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpsertHotelSettings]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_UpsertHotelSettings];
GO

CREATE PROCEDURE [dbo].[sp_UpsertHotelSettings]
    @Id INT = NULL,
    @BranchID INT,
    @HotelName NVARCHAR(200),
    @Address NVARCHAR(500),
    @ContactNumber1 NVARCHAR(20),
    @ContactNumber2 NVARCHAR(20) = NULL,
    @EmailAddress NVARCHAR(100),
    @Website NVARCHAR(200) = NULL,
    @GSTCode NVARCHAR(50) = NULL,
    @LogoPath NVARCHAR(500) = NULL,
    @CheckInTime TIME,
    @CheckOutTime TIME,
    @ByPassActualDayRate BIT,
    @DiscountApprovalRequired BIT,
    @MinimumBookingAmountRequired BIT,
    @MinimumBookingAmount DECIMAL(18,2) = NULL,
    @NoShowGraceHours INT,
    @CancellationRefundApprovalThreshold DECIMAL(18,2) = NULL,
    @EnableCancellationPolicy BIT = 1,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    SELECT @ExistingId = Id FROM HotelSettings WHERE BranchID = @BranchID AND IsActive = 1;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE HotelSettings
        SET
            HotelName = @HotelName,
            Address = @Address,
            ContactNumber1 = @ContactNumber1,
            ContactNumber2 = @ContactNumber2,
            EmailAddress = @EmailAddress,
            Website = @Website,
            GSTCode = @GSTCode,
            LogoPath = @LogoPath,
            CheckInTime = @CheckInTime,
            CheckOutTime = @CheckOutTime,
            ByPassActualDayRate = @ByPassActualDayRate,
            DiscountApprovalRequired = @DiscountApprovalRequired,
            MinimumBookingAmountRequired = @MinimumBookingAmountRequired,
            MinimumBookingAmount = @MinimumBookingAmount,
            NoShowGraceHours = @NoShowGraceHours,
            CancellationRefundApprovalThreshold = @CancellationRefundApprovalThreshold,
            EnableCancellationPolicy = @EnableCancellationPolicy,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @ModifiedBy
        WHERE Id = @ExistingId;

        SELECT @ExistingId AS Id;
    END
    ELSE
    BEGIN
        INSERT INTO HotelSettings (
            BranchID, HotelName, Address, ContactNumber1, ContactNumber2,
            EmailAddress, Website, GSTCode, LogoPath, CheckInTime, CheckOutTime,
            ByPassActualDayRate,
            DiscountApprovalRequired,
            MinimumBookingAmountRequired,
            MinimumBookingAmount,
            NoShowGraceHours,
            CancellationRefundApprovalThreshold,
            EnableCancellationPolicy,
            CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            @ByPassActualDayRate,
            @DiscountApprovalRequired,
            @MinimumBookingAmountRequired,
            @MinimumBookingAmount,
            @NoShowGraceHours,
            @CancellationRefundApprovalThreshold,
            @EnableCancellationPolicy,
            GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );

        SELECT SCOPE_IDENTITY() AS Id;
    END
END
GO

PRINT 'Hotel Settings updated with EnableCancellationPolicy column and stored procedures recreated.';
GO
