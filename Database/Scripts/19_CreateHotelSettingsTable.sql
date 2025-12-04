-- Create HotelSettings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HotelSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE HotelSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        HotelName NVARCHAR(200) NOT NULL,
        Address NVARCHAR(500) NOT NULL,
        ContactNumber1 NVARCHAR(20) NOT NULL,
        ContactNumber2 NVARCHAR(20) NULL,
        EmailAddress NVARCHAR(100) NOT NULL,
        Website NVARCHAR(200) NULL,
        GSTCode NVARCHAR(50) NULL,
        LogoPath NVARCHAR(500) NULL,
        CheckInTime TIME NOT NULL DEFAULT '14:00:00',
        CheckOutTime TIME NOT NULL DEFAULT '12:00:00',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_HotelSettings_Branch FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID),
        CONSTRAINT UQ_HotelSettings_Branch UNIQUE (BranchID)
    );
    
    PRINT 'Table HotelSettings created successfully';
END
ELSE
BEGIN
    PRINT 'Table HotelSettings already exists';
END
GO

-- Create stored procedure to get hotel settings by branch
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
        IsActive,
        CreatedDate,
        CreatedBy,
        LastModifiedDate,
        LastModifiedBy
    FROM HotelSettings
    WHERE BranchID = @BranchID AND IsActive = 1;
END
GO

-- Create stored procedure to upsert hotel settings
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
            CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );
        
        SELECT SCOPE_IDENTITY() AS Id;
    END
END
GO

PRINT 'Hotel Settings stored procedures created successfully';
GO
