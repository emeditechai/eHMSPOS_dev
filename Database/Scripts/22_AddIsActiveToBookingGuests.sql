-- Add IsActive column to BookingGuests table for soft delete
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'IsActive')
BEGIN
    ALTER TABLE BookingGuests ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsActive column to BookingGuests table';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists in BookingGuests table';
END
GO

-- Add ModifiedDate and ModifiedBy columns for tracking updates
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'ModifiedDate')
BEGIN
    ALTER TABLE BookingGuests ADD 
        ModifiedDate DATETIME2 NULL,
        ModifiedBy INT NULL;
    PRINT 'Added ModifiedDate and ModifiedBy columns to BookingGuests table';
END
ELSE
BEGIN
    PRINT 'ModifiedDate and ModifiedBy columns already exist in BookingGuests table';
END
GO
