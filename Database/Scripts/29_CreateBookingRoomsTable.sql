-- Create BookingRooms table to handle multiple room assignments per booking
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookingRooms')
BEGIN
    CREATE TABLE BookingRooms (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BookingId INT NOT NULL,
        RoomId INT NOT NULL,
        AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
        UnassignedDate DATETIME NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NULL,
        CONSTRAINT FK_BookingRooms_Booking FOREIGN KEY (BookingId) REFERENCES Bookings(Id),
        CONSTRAINT FK_BookingRooms_Room FOREIGN KEY (RoomId) REFERENCES Rooms(Id),
        CONSTRAINT UQ_BookingRooms_Active UNIQUE (RoomId, BookingId, IsActive)
    );

    CREATE INDEX IX_BookingRooms_BookingId ON BookingRooms(BookingId);
    CREATE INDEX IX_BookingRooms_RoomId ON BookingRooms(RoomId);
    CREATE INDEX IX_BookingRooms_IsActive ON BookingRooms(IsActive);

    PRINT 'BookingRooms table created successfully';
END
ELSE
BEGIN
    PRINT 'BookingRooms table already exists';
END
GO

-- Migrate existing room assignments from Bookings table
INSERT INTO BookingRooms (BookingId, RoomId, AssignedDate, IsActive, CreatedDate, CreatedBy)
SELECT 
    Id AS BookingId,
    RoomId,
    ISNULL(ActualCheckInDate, CheckInDate) AS AssignedDate,
    CASE WHEN Status IN ('Confirmed', 'CheckedIn') THEN 1 ELSE 0 END AS IsActive,
    CreatedDate,
    CreatedBy
FROM Bookings
WHERE RoomId IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM BookingRooms br WHERE br.BookingId = Bookings.Id AND br.RoomId = Bookings.RoomId
);

PRINT 'Existing room assignments migrated to BookingRooms table';
GO
