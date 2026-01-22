-- Add AssetTag column to AssetMovementLines (per-movement captured tag)
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[AssetMovementLines]')
      AND name = 'AssetTag'
)
BEGIN
    ALTER TABLE dbo.AssetMovementLines
        ADD AssetTag NVARCHAR(100) NULL;
END
