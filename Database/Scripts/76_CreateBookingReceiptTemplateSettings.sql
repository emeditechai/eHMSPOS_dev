-- Create BookingReceiptTemplateSettings table (per-branch receipt template selection)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookingReceiptTemplateSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE BookingReceiptTemplateSettings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        TemplateKey NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_BookingReceiptTemplateSettings_Branch FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID),
        CONSTRAINT UQ_BookingReceiptTemplateSettings_Branch UNIQUE (BranchID)
    );

    PRINT 'Table BookingReceiptTemplateSettings created successfully';
END
ELSE
BEGIN
    PRINT 'Table BookingReceiptTemplateSettings already exists';
END
GO

-- Get receipt template setting by branch
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetBookingReceiptTemplateByBranch]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_GetBookingReceiptTemplateByBranch];
GO

CREATE PROCEDURE [dbo].[sp_GetBookingReceiptTemplateByBranch]
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        Id,
        BranchID,
        TemplateKey,
        IsActive,
        CreatedDate,
        CreatedBy,
        LastModifiedDate,
        LastModifiedBy
    FROM BookingReceiptTemplateSettings
    WHERE BranchID = @BranchID AND IsActive = 1;
END
GO

-- Upsert receipt template setting by branch
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpsertBookingReceiptTemplateByBranch]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_UpsertBookingReceiptTemplateByBranch];
GO

CREATE PROCEDURE [dbo].[sp_UpsertBookingReceiptTemplateByBranch]
    @BranchID INT,
    @TemplateKey NVARCHAR(50),
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;
    SELECT @ExistingId = Id
    FROM BookingReceiptTemplateSettings
    WHERE BranchID = @BranchID AND IsActive = 1;

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE BookingReceiptTemplateSettings
        SET
            TemplateKey = @TemplateKey,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @ModifiedBy
        WHERE Id = @ExistingId;

        SELECT @ExistingId AS Id;
    END
    ELSE
    BEGIN
        INSERT INTO BookingReceiptTemplateSettings (
            BranchID,
            TemplateKey,
            IsActive,
            CreatedDate,
            CreatedBy,
            LastModifiedDate,
            LastModifiedBy
        )
        VALUES (
            @BranchID,
            @TemplateKey,
            1,
            GETDATE(),
            @ModifiedBy,
            GETDATE(),
            @ModifiedBy
        );

        SELECT SCOPE_IDENTITY() AS Id;
    END
END
GO

PRINT 'BookingReceiptTemplateSettings stored procedures created successfully';
GO
