-- Add MarkAvailableDate column to RoomMaintenanceHistory
-- Used when a room is taken back to Available from Maintenance

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'dbo' AND t.name = 'RoomMaintenanceHistory'
)
BEGIN
    IF COL_LENGTH('dbo.RoomMaintenanceHistory', 'MarkAvailableDate') IS NULL
    BEGIN
        ALTER TABLE dbo.RoomMaintenanceHistory
        ADD MarkAvailableDate DATETIME NULL;
    END
END
