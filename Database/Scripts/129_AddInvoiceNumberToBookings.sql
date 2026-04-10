-- Migration 129: Add InvoiceNumber to Bookings and create InvoiceSequence table
-- Pattern: INV/{FY e.g. 2025-26}/{5-digit seq}
-- Example: INV/2025-26/00001

-- 1. Add InvoiceNumber column to Bookings
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Bookings' AND COLUMN_NAME = 'InvoiceNumber'
)
BEGIN
    ALTER TABLE Bookings ADD InvoiceNumber NVARCHAR(30) NULL;
END
GO

-- 2. Create unique index on InvoiceNumber (only non-null values)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'UX_Bookings_InvoiceNumber' AND object_id = OBJECT_ID('Bookings')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Bookings_InvoiceNumber 
    ON Bookings (InvoiceNumber) 
    WHERE InvoiceNumber IS NOT NULL;
END
GO

-- 3. Create InvoiceSequence table to track per-FY, per-branch sequences
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InvoiceSequence')
BEGIN
    CREATE TABLE InvoiceSequence (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FinancialYear NVARCHAR(10) NOT NULL,  -- e.g. '2025-26'
        BranchID INT NOT NULL,
        LastSequence INT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_InvoiceSequence_FY_Branch UNIQUE (FinancialYear, BranchID)
    );
END
GO
