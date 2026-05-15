-- ============================================================
-- Script 160: Add GstSlabId column to BanquetAddonServices
-- Links addon service master GST configuration to GstSlabs master
-- ============================================================
SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BanquetAddonServices' AND COLUMN_NAME = 'GstSlabId'
)
BEGIN
    ALTER TABLE dbo.BanquetAddonServices
        ADD GstSlabId INT NULL;

    ALTER TABLE dbo.BanquetAddonServices
        ADD CONSTRAINT FK_BanquetAddonServices_GstSlabId
        FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);

    PRINT 'Script 160: Added GstSlabId column and FK to BanquetAddonServices.';
END
ELSE
BEGIN
    PRINT 'Script 160: GstSlabId already exists on BanquetAddonServices; skipping.';
END
GO
