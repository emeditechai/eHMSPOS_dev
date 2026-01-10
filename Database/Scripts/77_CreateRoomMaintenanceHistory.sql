-- Create Room Maintenance History table
-- Stores maintenance reasons with server-side timestamp

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'dbo' AND t.name = 'RoomMaintenanceHistory'
)
BEGIN
    CREATE TABLE dbo.RoomMaintenanceHistory (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoomId INT NOT NULL,
        BranchID INT NULL,
        Reason NVARCHAR(500) NOT NULL,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RoomMaintenanceHistory_CreatedDate DEFAULT (GETDATE()),
        MarkAvailableDate DATETIME NULL
    );

    -- FK to Rooms
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_RoomMaintenanceHistory_Rooms_RoomId'
    )
    BEGIN
        ALTER TABLE dbo.RoomMaintenanceHistory
        ADD CONSTRAINT FK_RoomMaintenanceHistory_Rooms_RoomId
            FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id);
    END

    CREATE INDEX IX_RoomMaintenanceHistory_RoomId_CreatedDate
        ON dbo.RoomMaintenanceHistory (RoomId, CreatedDate DESC);
END
