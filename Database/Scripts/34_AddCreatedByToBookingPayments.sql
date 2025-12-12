-- Migration: Add CreatedBy column to BookingPayments table (who created payment entry)
-- Date: 2025-12-12

USE HMS_dev;
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') 
      AND name = 'CreatedBy'
)
BEGIN
    ALTER TABLE [dbo].[BookingPayments]
    ADD [CreatedBy] INT NULL;

    PRINT 'Added CreatedBy column to BookingPayments';
END
ELSE
BEGIN
    PRINT 'CreatedBy column already exists in BookingPayments';
END
GO

-- Add FK to Users if not exists
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_BookingPayments_CreatedBy'
)
BEGIN
    ALTER TABLE [dbo].[BookingPayments]
    ADD CONSTRAINT FK_BookingPayments_CreatedBy FOREIGN KEY ([CreatedBy])
        REFERENCES [dbo].[Users]([Id]);

    PRINT 'Added FK_BookingPayments_CreatedBy';
END
ELSE
BEGIN
    PRINT 'FK_BookingPayments_CreatedBy already exists';
END
GO
