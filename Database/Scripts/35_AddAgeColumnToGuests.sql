-- =============================================
-- Add Age Column to Guests Table
-- Created: 2025-12-13
-- Description: Persist Age as an optional field for guest records.
-- Notes: BookingGuests already has Age and DateOfBirth; Guests already has DateOfBirth.
-- =============================================

USE HMS_dev;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Age')
BEGIN
    ALTER TABLE [dbo].[Guests]
    ADD [Age] INT NULL;

    PRINT 'Age column added to Guests table';
END
ELSE
BEGIN
    PRINT 'Age column already exists in Guests table';
END
GO

PRINT 'Age column migration completed successfully';
GO
