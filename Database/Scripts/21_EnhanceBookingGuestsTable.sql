-- Add additional fields to BookingGuests table for comprehensive guest information
-- This allows storing detailed guest data directly in the booking context

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BookingGuests') AND name = 'RelationshipToPrimary')
BEGIN
    ALTER TABLE BookingGuests ADD 
        RelationshipToPrimary NVARCHAR(50) NULL,
        Age INT NULL,
        DateOfBirth DATE NULL,
        IdentityType NVARCHAR(50) NULL,
        IdentityNumber NVARCHAR(100) NULL,
        DocumentPath NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedBy INT NULL;
    
    PRINT 'Additional fields added to BookingGuests table successfully.';
END
ELSE
BEGIN
    PRINT 'BookingGuests table already has the enhanced fields.';
END
GO
