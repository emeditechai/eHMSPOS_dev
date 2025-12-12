-- Migration: Add ReceiptNumber column to BookingPayments table
-- Description: Adds ReceiptNumber field to track unique receipt numbers for each payment
-- Date: 2025-12-12

USE HMS_dev;
GO

-- Add ReceiptNumber column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') 
               AND name = 'ReceiptNumber')
BEGIN
    ALTER TABLE BookingPayments
    ADD ReceiptNumber NVARCHAR(50) NULL;
    
    PRINT 'Added ReceiptNumber column to BookingPayments table';
END
ELSE
BEGIN
    PRINT 'ReceiptNumber column already exists in BookingPayments table';
END
GO

-- Generate receipt numbers for existing payments without one
DECLARE @BranchID INT;
DECLARE @PaymentId INT;
DECLARE @PaidOn DATETIME;
DECLARE @ReceiptNumber NVARCHAR(50);

DECLARE payment_cursor CURSOR FOR
SELECT Id, PaidOn FROM BookingPayments WHERE ReceiptNumber IS NULL;

OPEN payment_cursor;

FETCH NEXT FROM payment_cursor INTO @PaymentId, @PaidOn;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Generate receipt number in format: RCP-YYYYMMDD-#### (based on payment ID)
    SET @ReceiptNumber = 'RCP-' + FORMAT(@PaidOn, 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(@PaymentId AS VARCHAR), 4);
    
    UPDATE BookingPayments
    SET ReceiptNumber = @ReceiptNumber
    WHERE Id = @PaymentId;
    
    FETCH NEXT FROM payment_cursor INTO @PaymentId, @PaidOn;
END

CLOSE payment_cursor;
DEALLOCATE payment_cursor;

PRINT 'Generated receipt numbers for existing payments';
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Create index on ReceiptNumber for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_BookingPayments_ReceiptNumber' 
               AND object_id = OBJECT_ID(N'[dbo].[BookingPayments]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_BookingPayments_ReceiptNumber
    ON BookingPayments(ReceiptNumber)
    WHERE ReceiptNumber IS NOT NULL;
    
    PRINT 'Created index on ReceiptNumber column';
END
ELSE
BEGIN
    PRINT 'Index on ReceiptNumber already exists';
END
GO

PRINT 'Migration completed successfully';
GO
