-- Creates per-branch UPI settings for dynamic QR payments

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UpiSettings')
BEGIN
    CREATE TABLE dbo.UpiSettings
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BranchID INT NOT NULL,
        UpiVpa NVARCHAR(100) NULL,
        PayeeName NVARCHAR(100) NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_UpiSettings_IsEnabled DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_UpiSettings_CreatedDate DEFAULT (GETDATE()),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL CONSTRAINT DF_UpiSettings_LastModifiedDate DEFAULT (GETDATE()),
        LastModifiedBy INT NULL
    );

    -- One row per branch
    CREATE UNIQUE INDEX UX_UpiSettings_BranchID ON dbo.UpiSettings(BranchID);
END
