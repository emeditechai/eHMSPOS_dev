-- Migration: Add ReceiptGroupNumber column to BookingPayments table
-- Description: Stores a single displayed receipt number per payment transaction,
--              while keeping per-row ReceiptNumber unique (e.g., when payments are split by BillingHead).
-- Date: 2025-12-31

USE HMS_dev;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]')
      AND name = 'ReceiptGroupNumber'
)
BEGIN
    ALTER TABLE BookingPayments
    ADD ReceiptGroupNumber NVARCHAR(50) NULL;

    PRINT 'Added ReceiptGroupNumber column to BookingPayments table';
END
ELSE
BEGIN
    PRINT 'ReceiptGroupNumber column already exists in BookingPayments table';
END
GO

-- Backfill: for existing rows, default group = receipt
UPDATE BookingPayments
SET ReceiptGroupNumber = ReceiptNumber
WHERE ReceiptGroupNumber IS NULL
  AND ReceiptNumber IS NOT NULL;
GO

-- Helpful (non-unique) index for grouping/printing
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_BookingPayments_ReceiptGroupNumber'
      AND object_id = OBJECT_ID(N'[dbo].[BookingPayments]')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BookingPayments_ReceiptGroupNumber
    ON BookingPayments(ReceiptGroupNumber)
    WHERE ReceiptGroupNumber IS NOT NULL;

    PRINT 'Created index on ReceiptGroupNumber column';
END
ELSE
BEGIN
    PRINT 'Index on ReceiptGroupNumber already exists';
END
GO

PRINT 'Migration completed successfully';
GO
