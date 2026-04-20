-- Migration 140: Add BranchID to GstSlabs for branch-wise isolation
-- ================================================================

-- 1) Add BranchID column (default 1 = Head Office for existing rows)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'GstSlabs' AND COLUMN_NAME = 'BranchID'
)
BEGIN
    ALTER TABLE dbo.GstSlabs ADD BranchID INT NOT NULL CONSTRAINT DF_GstSlabs_BranchID DEFAULT (1);

    ALTER TABLE dbo.GstSlabs ADD CONSTRAINT FK_GstSlabs_BranchMaster
        FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID);
END
GO

-- 2) Replace global UNIQUE(SlabCode) with branch-scoped UNIQUE(SlabCode, BranchID)
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_GstSlabs_SlabCode' AND object_id = OBJECT_ID('dbo.GstSlabs')
)
BEGIN
    ALTER TABLE dbo.GstSlabs DROP CONSTRAINT UQ_GstSlabs_SlabCode;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_GstSlabs_SlabCode_BranchID' AND object_id = OBJECT_ID('dbo.GstSlabs')
)
BEGIN
    ALTER TABLE dbo.GstSlabs ADD CONSTRAINT UQ_GstSlabs_SlabCode_BranchID UNIQUE (SlabCode, BranchID);
END
GO

-- 3) Add index for branch-scoped queries
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_GstSlabs_BranchID_Active' AND object_id = OBJECT_ID('dbo.GstSlabs')
)
BEGIN
    CREATE INDEX IX_GstSlabs_BranchID_Active ON dbo.GstSlabs(BranchID, IsActive, EffectiveFrom, EffectiveTo);
END
GO
