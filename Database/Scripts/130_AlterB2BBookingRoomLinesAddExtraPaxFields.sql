-- =============================================
-- Add Extra Pax and Discount columns to B2BBookingRoomLines
-- These fields store the breakdown needed for receipt/details visibility
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'ExtraPaxCount')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD ExtraPaxCount INT NOT NULL DEFAULT 0;
    PRINT 'Added ExtraPaxCount column';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'ExtraPaxRatePerNight')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD ExtraPaxRatePerNight DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT 'Added ExtraPaxRatePerNight column';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'DiscountPercentage')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD DiscountPercentage DECIMAL(5,2) NOT NULL DEFAULT 0;
    PRINT 'Added DiscountPercentage column';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'DiscountAmount')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines
        ADD DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT 'Added DiscountAmount column';
END
GO
