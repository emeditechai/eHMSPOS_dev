-- Add ActualCheckOutDate column to Bookings table
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Bookings' 
    AND COLUMN_NAME = 'ActualCheckOutDate'
)
BEGIN
    ALTER TABLE Bookings
    ADD ActualCheckOutDate DATETIME2 NULL;
    
    PRINT 'ActualCheckOutDate column added to Bookings table';
END
ELSE
BEGIN
    PRINT 'ActualCheckOutDate column already exists';
END
GO
