-- =============================================
-- Add BookingNumber column to Bookings table
-- Created: 2025-11-20
-- =============================================

USE HMS_dev;
GO

-- Check if column exists, if not add it
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') 
    AND name = 'BookingNumber'
)
BEGIN
    ALTER TABLE [dbo].[Bookings]
    ADD [BookingNumber] NVARCHAR(25) NULL;
    
    PRINT 'BookingNumber column added successfully';
END
ELSE
BEGIN
    PRINT 'BookingNumber column already exists';
END
GO

-- Update existing rows with generated booking numbers
UPDATE [dbo].[Bookings]
SET [BookingNumber] = 'BK-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '-' + CAST(Id AS NVARCHAR(10))
WHERE [BookingNumber] IS NULL;
GO

-- Make the column NOT NULL after populating existing rows
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') 
    AND name = 'BookingNumber'
    AND is_nullable = 1
)
BEGIN
    ALTER TABLE [dbo].[Bookings]
    ALTER COLUMN [BookingNumber] NVARCHAR(25) NOT NULL;
    
    PRINT 'BookingNumber column set to NOT NULL';
END
GO

-- Add unique constraint if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') 
    AND name = 'UQ_Bookings_BookingNumber'
)
BEGIN
    ALTER TABLE [dbo].[Bookings]
    ADD CONSTRAINT UQ_Bookings_BookingNumber UNIQUE ([BookingNumber]);
    
    PRINT 'Unique constraint added to BookingNumber';
END
ELSE
BEGIN
    PRINT 'Unique constraint already exists on BookingNumber';
END
GO

PRINT 'Bookings table BookingNumber column migration completed successfully';
GO
