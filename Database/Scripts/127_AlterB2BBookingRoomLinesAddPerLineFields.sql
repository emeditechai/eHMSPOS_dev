-- Migration 127: Add per-room-type check-in/out, occupancy, and meal plan to B2BBookingRoomLines
-- Each room-type line in a B2B booking can now have its own dates, adults, children, and meal plan.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'CheckInDate')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines ADD CheckInDate DATE NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'CheckOutDate')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines ADD CheckOutDate DATE NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'Adults')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines ADD Adults INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'Children')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines ADD Children INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BBookingRoomLines') AND name = 'MealPlan')
BEGIN
    ALTER TABLE dbo.B2BBookingRoomLines ADD MealPlan NVARCHAR(20) NULL;
END
GO
