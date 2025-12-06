-- =============================================
-- Add Gender Column to Guest Tables
-- Created: 2025-12-06
-- Description: Add Gender field to Guests and BookingGuests tables
-- =============================================

USE HMS_dev;
GO

-- Add Gender column to Guests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[Guests]
    ADD [Gender] NVARCHAR(20) NULL;
    
    PRINT 'Gender column added to Guests table';
END
ELSE
BEGIN
    PRINT 'Gender column already exists in Guests table';
END
GO

-- Add Gender column to BookingGuests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[BookingGuests]
    ADD [Gender] NVARCHAR(20) NULL;
    
    PRINT 'Gender column added to BookingGuests table';
END
ELSE
BEGIN
    PRINT 'Gender column already exists in BookingGuests table';
END
GO

PRINT 'Gender column migration completed successfully';
GO
