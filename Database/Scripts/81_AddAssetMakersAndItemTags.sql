SET NOCOUNT ON;

/*
Asset Management - Maker master + optional tagging fields for items
Adds:
- dbo.AssetMakers (per-branch)
- dbo.AssetItems: MakerId (nullable), Barcode (nullable), AssetTag (nullable)
- Uniqueness per branch for Barcode / AssetTag when provided
*/

IF OBJECT_ID('dbo.AssetMakers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetMakers
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetMakers PRIMARY KEY,
        BranchID INT NOT NULL,
        [Name] NVARCHAR(80) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AssetMakers_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetMakers_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_AssetMakers_BranchID_Name UNIQUE (BranchID, [Name])
    );

    CREATE INDEX IX_AssetMakers_BranchID_IsActive
        ON dbo.AssetMakers (BranchID, IsActive)
        INCLUDE ([Name]);
END

-- Add columns to AssetItems if missing
IF COL_LENGTH('dbo.AssetItems', 'MakerId') IS NULL
BEGIN
    ALTER TABLE dbo.AssetItems
        ADD MakerId INT NULL;
END

IF COL_LENGTH('dbo.AssetItems', 'Barcode') IS NULL
BEGIN
    ALTER TABLE dbo.AssetItems
        ADD Barcode NVARCHAR(60) NULL;
END

IF COL_LENGTH('dbo.AssetItems', 'AssetTag') IS NULL
BEGIN
    ALTER TABLE dbo.AssetItems
        ADD AssetTag NVARCHAR(60) NULL;
END

-- Normalize empty strings to NULL before applying unique indexes
UPDATE dbo.AssetItems
SET Barcode = NULL
WHERE Barcode IS NOT NULL AND LTRIM(RTRIM(Barcode)) = '';

UPDATE dbo.AssetItems
SET AssetTag = NULL
WHERE AssetTag IS NOT NULL AND LTRIM(RTRIM(AssetTag)) = '';

-- Add FK for MakerId (idempotent)
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_AssetItems_Maker'
)
BEGIN
    ALTER TABLE dbo.AssetItems
        ADD CONSTRAINT FK_AssetItems_Maker
            FOREIGN KEY (MakerId) REFERENCES dbo.AssetMakers(Id);
END

-- Unique Barcode per Branch when provided
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AssetItems_BranchID_Barcode'
      AND object_id = OBJECT_ID('dbo.AssetItems')
)
BEGIN
    CREATE UNIQUE INDEX UX_AssetItems_BranchID_Barcode
        ON dbo.AssetItems (BranchID, Barcode)
        WHERE Barcode IS NOT NULL;
END

-- Unique AssetTag per Branch when provided
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_AssetItems_BranchID_AssetTag'
      AND object_id = OBJECT_ID('dbo.AssetItems')
)
BEGIN
    CREATE UNIQUE INDEX UX_AssetItems_BranchID_AssetTag
        ON dbo.AssetItems (BranchID, AssetTag)
        WHERE AssetTag IS NOT NULL;
END
