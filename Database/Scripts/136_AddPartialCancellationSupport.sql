-- =============================================
-- Migration 136: Add partial room-type cancellation support
-- Adds IsCancelled tracking to B2BBookingRoomLines
-- Adds partial-cancellation columns to BookingCancellations
-- Created: 2026-04-18
-- =============================================

-- 1) B2BBookingRoomLines: track per-line cancellation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'IsCancelled')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD IsCancelled BIT NOT NULL CONSTRAINT DF_B2BBookingRoomLines_IsCancelled DEFAULT (0);
    PRINT 'Added IsCancelled to B2BBookingRoomLines';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'CancelledDate')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD CancelledDate DATETIME2 NULL;
    PRINT 'Added CancelledDate to B2BBookingRoomLines';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'CancelledBy')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD CancelledBy INT NULL;
    PRINT 'Added CancelledBy to B2BBookingRoomLines';
END
GO

-- 2) BookingCancellations: track partial cancellation info
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BookingCancellations') AND name = 'IsPartial')
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD IsPartial BIT NOT NULL CONSTRAINT DF_BookingCancellations_IsPartial DEFAULT (0);
    PRINT 'Added IsPartial to BookingCancellations';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BookingCancellations') AND name = 'CancelledRoomLineIds')
BEGIN
    ALTER TABLE dbo.BookingCancellations
        ADD CancelledRoomLineIds NVARCHAR(200) NULL;
    PRINT 'Added CancelledRoomLineIds to BookingCancellations';
END
GO

PRINT 'Migration 136 complete — partial cancellation support added.';
GO
