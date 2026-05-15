-- ============================================================
-- Script 158: Add GstSlabId column to BanquetVenues
-- Links venue master GST configuration to the GstSlabs master
-- ============================================================
SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BanquetVenues' AND COLUMN_NAME = 'GstSlabId'
)
BEGIN
    ALTER TABLE dbo.BanquetVenues
        ADD GstSlabId INT NULL;

    ALTER TABLE dbo.BanquetVenues
        ADD CONSTRAINT FK_BanquetVenues_GstSlabId
        FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);

    PRINT 'Script 158: Added GstSlabId column and FK to BanquetVenues.';
END
ELSE
BEGIN
    PRINT 'Script 158: GstSlabId already exists on BanquetVenues; skipping.';
END
GO
