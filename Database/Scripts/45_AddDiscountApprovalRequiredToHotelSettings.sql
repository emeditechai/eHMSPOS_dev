-- Add DiscountApprovalRequired to HotelSettings
IF COL_LENGTH('dbo.HotelSettings', 'DiscountApprovalRequired') IS NULL
BEGIN
    ALTER TABLE dbo.HotelSettings
    ADD DiscountApprovalRequired BIT NOT NULL CONSTRAINT DF_HotelSettings_DiscountApprovalRequired DEFAULT (0);

    PRINT 'Column DiscountApprovalRequired added to HotelSettings';
END
ELSE
BEGIN
    PRINT 'Column DiscountApprovalRequired already exists in HotelSettings';
END
GO

-- Recreate stored procedure to get hotel settings by branch (include DiscountApprovalRequired)
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
        IsActive,
        CreatedDate,
        CreatedBy,
        LastModifiedDate,
        LastModifiedBy
    FROM HotelSettings
    WHERE BranchID = @BranchID AND IsActive = 1;
END
GO

-- Recreate stored procedure to upsert hotel settings (include DiscountApprovalRequired)
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
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    -- Check if settings exist for this branch
    SELECT @ExistingId = Id FROM HotelSettings WHERE BranchID = @BranchID AND IsActive = 1;

    IF @ExistingId IS NOT NULL
    BEGIN
        -- Update existing record
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
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @ModifiedBy
        WHERE Id = @ExistingId;

        SELECT @ExistingId AS Id;
    END
    ELSE
    BEGIN
        -- Insert new record
        INSERT INTO HotelSettings (
            BranchID, HotelName, Address, ContactNumber1, ContactNumber2,
            EmailAddress, Website, GSTCode, LogoPath, CheckInTime, CheckOutTime,
            ByPassActualDayRate,
            DiscountApprovalRequired,
            CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            @ByPassActualDayRate,
            @DiscountApprovalRequired,
            GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );

        SELECT SCOPE_IDENTITY() AS Id;
    END
END
GO

PRINT 'Hotel Settings stored procedures updated for DiscountApprovalRequired';
GO
