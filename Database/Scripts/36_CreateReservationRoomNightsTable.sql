-- =============================================
-- Create Table: ReservationRoomNights
-- Purpose:
--   Stores night-wise plan pricing BEFORE room assignment.
--   Used for printing plan details in receipt when no rooms are assigned.
-- Notes:
--   Clone of BookingRoomNights but without RoomId.
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReservationRoomNights]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ReservationRoomNights] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReservationRoomNights PRIMARY KEY,
        [BookingId] INT NOT NULL,
        [StayDate] DATE NOT NULL,
        [RateAmount] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_RateAmount DEFAULT(0),
        [ActualBaseRate] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_ActualBaseRate DEFAULT(0),
        [DiscountAmount] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_DiscountAmount DEFAULT(0),
        [TaxAmount] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_TaxAmount DEFAULT(0),
        [CGSTAmount] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_CGSTAmount DEFAULT(0),
        [SGSTAmount] DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReservationRoomNights_SGSTAmount DEFAULT(0),
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT DF_ReservationRoomNights_Status DEFAULT('Reserved'),
        [CreatedDate] DATETIME2 NOT NULL CONSTRAINT DF_ReservationRoomNights_CreatedDate DEFAULT(GETDATE()),

        CONSTRAINT FK_ReservationRoomNights_Bookings
            FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX IX_ReservationRoomNights_BookingId ON [dbo].[ReservationRoomNights]([BookingId]);
    CREATE UNIQUE INDEX UX_ReservationRoomNights_BookingId_StayDate
        ON [dbo].[ReservationRoomNights]([BookingId], [StayDate]);
END
