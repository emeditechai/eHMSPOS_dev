-- Migration 138: Add PoliceStation column to HotelSettings
-- =============================================

-- 1. Add PoliceStation column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HotelSettings') AND name = 'PoliceStation')
BEGIN
    ALTER TABLE HotelSettings ADD PoliceStation NVARCHAR(200) NULL;
    PRINT 'Added PoliceStation to HotelSettings.';
END
GO

-- 2. Update sp_GetHotelSettingsByBranch to include PoliceStation
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'sp_GetHotelSettingsByBranch') AND type = 'P')
BEGIN
    DROP PROCEDURE sp_GetHotelSettingsByBranch;
END
GO

CREATE PROCEDURE sp_GetHotelSettingsByBranch
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM HotelSettings WHERE BranchID = @BranchID AND IsActive = 1;
END
GO

PRINT 'Recreated sp_GetHotelSettingsByBranch with PoliceStation support.';
GO

-- 3. Update sp_UpsertHotelSettings to include PoliceStation
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
            IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            @ByPassActualDayRate, @DiscountApprovalRequired,
            @MinimumBookingAmountRequired, @MinimumBookingAmount,
            @NoShowGraceHours, @CancellationRefundApprovalThreshold,
            @EnableCancellationPolicy, @PoliceStation,
            1, GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );
    END
END
GO

PRINT 'Recreated sp_UpsertHotelSettings with PoliceStation support.';
GO

PRINT 'Migration 138 complete.';
