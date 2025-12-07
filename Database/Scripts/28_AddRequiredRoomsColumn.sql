-- Migration: Add RequiredRooms column to Bookings table
-- Description: Adds a column to track how many rooms are required for a booking
-- Date: 2025-12-07

-- Add RequiredRooms column with default value of 1
ALTER TABLE Bookings
ADD RequiredRooms INT NOT NULL DEFAULT 1
    CONSTRAINT CK_Bookings_RequiredRooms CHECK (RequiredRooms >= 1 AND RequiredRooms <= 50);

PRINT 'Migration completed: RequiredRooms column added to Bookings table';
