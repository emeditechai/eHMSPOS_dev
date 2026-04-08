-- Migration 126: Create B2BBookingRoomLines table
-- Stores per-room-type line items for a B2B booking (supporting multi-room-type selections)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.B2BBookingRoomLines') AND type = 'U')
BEGIN
    CREATE TABLE dbo.B2BBookingRoomLines (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        BookingId     INT NOT NULL REFERENCES dbo.Bookings(Id) ON DELETE CASCADE,
        RoomTypeId    INT NOT NULL,
        RoomTypeName  NVARCHAR(100) NOT NULL,
        RequiredRooms INT NOT NULL DEFAULT 1,
        RatePerNight  DECIMAL(18,2) NOT NULL DEFAULT 0,
        Nights        INT NOT NULL DEFAULT 1,
        BaseAmount    DECIMAL(18,2) NOT NULL DEFAULT 0,
        TaxAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
        GrandTotal    DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreatedDate   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_B2BBookingRoomLines_BookingId ON dbo.B2BBookingRoomLines (BookingId);
END
GO
