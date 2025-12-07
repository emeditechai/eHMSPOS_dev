-- Script 27: Add Max_RoomAvailability column to RoomTypes table
-- Purpose: Set maximum room availability count per room type for availability calculations
-- Date: 2025-12-07

USE HMS_dev;
GO

-- Add Max_RoomAvailability column to RoomTypes if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[RoomTypes]') 
               AND name = 'Max_RoomAvailability')
BEGIN
    ALTER TABLE RoomTypes
    ADD Max_RoomAvailability INT NULL;
    
    PRINT 'Max_RoomAvailability column added to RoomTypes table';
END
ELSE
BEGIN
    PRINT 'Max_RoomAvailability column already exists in RoomTypes table';
END
GO

-- Update Max_RoomAvailability with current total room counts by room type as default
UPDATE rt
SET rt.Max_RoomAvailability = (
    SELECT COUNT(r.Id)
    FROM Rooms r
    WHERE r.RoomTypeId = rt.Id
        AND r.IsActive = 1
)
FROM RoomTypes rt
WHERE rt.Max_RoomAvailability IS NULL;

PRINT 'Max_RoomAvailability values initialized with current room counts';
GO
