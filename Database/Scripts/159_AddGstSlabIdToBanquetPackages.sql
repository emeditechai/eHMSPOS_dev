-- ============================================================
-- Script 159: Add GstSlabId column to BanquetPackages
-- Links package master GST configuration to the GstSlabs master
-- ============================================================
SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BanquetPackages' AND COLUMN_NAME = 'GstSlabId'
)
BEGIN
    ALTER TABLE dbo.BanquetPackages
        ADD GstSlabId INT NULL;

    ALTER TABLE dbo.BanquetPackages
        ADD CONSTRAINT FK_BanquetPackages_GstSlabId
        FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);

    PRINT 'Script 159: Added GstSlabId column and FK to BanquetPackages.';
END
ELSE
BEGIN
    PRINT 'Script 159: GstSlabId already exists on BanquetPackages; skipping.';
END
GO
