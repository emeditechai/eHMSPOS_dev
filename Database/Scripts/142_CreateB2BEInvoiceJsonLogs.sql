-- Migration 142: Create B2BEInvoiceJsonLogs table for E-Invoice JSON storage
-- Stores the e-invoice JSON payload generated at B2B booking checkout
-- when EInvoiceMode = 'MANUAL' in Hotel Settings.

-- 1. Create the main log table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.B2BEInvoiceJsonLogs') AND type = 'U')
BEGIN
    CREATE TABLE dbo.B2BEInvoiceJsonLogs (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        BookingId        INT NOT NULL,
        BookingNo        NVARCHAR(50) NOT NULL,
        InvoiceNumber    NVARCHAR(50) NOT NULL,
        Version          NVARCHAR(20) NOT NULL,
        GenerationType   NVARCHAR(10) NOT NULL DEFAULT 'MANUAL',  -- 'MANUAL' | 'AUTO'
        JsonPayload      NVARCHAR(MAX) NOT NULL,
        BranchID         INT NOT NULL,
        CreatedDate      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        INT NULL,
        CONSTRAINT FK_B2BEInvoiceJsonLogs_Bookings
            FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id)
    );

    CREATE INDEX IX_B2BEInvoiceJsonLogs_BookingId ON dbo.B2BEInvoiceJsonLogs (BookingId);
    CREATE INDEX IX_B2BEInvoiceJsonLogs_BranchID  ON dbo.B2BEInvoiceJsonLogs (BranchID);

    PRINT 'Created dbo.B2BEInvoiceJsonLogs table.';
END
ELSE
BEGIN
    PRINT 'dbo.B2BEInvoiceJsonLogs table already exists.';
END
GO

-- 2. Create version-sequence table to generate unique Version numbers (e.g. 1.1, 1.2, ...)
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.EInvoiceVersionSequence') AND type = 'U')
BEGIN
    CREATE TABLE dbo.EInvoiceVersionSequence (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        LastSequence  INT NOT NULL DEFAULT 0
    );
    -- Seed the initial row
    INSERT INTO dbo.EInvoiceVersionSequence (LastSequence) VALUES (0);
    PRINT 'Created dbo.EInvoiceVersionSequence table and seeded initial row.';
END
ELSE
BEGIN
    PRINT 'dbo.EInvoiceVersionSequence table already exists.';
END
GO

-- 3. Add GenerationType column if the table already existed without it (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BEInvoiceJsonLogs') AND name = 'GenerationType')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs
        ADD GenerationType NVARCHAR(10) NOT NULL DEFAULT 'MANUAL';
    PRINT 'Added GenerationType column to dbo.B2BEInvoiceJsonLogs.';
END
GO

PRINT 'Migration 142 complete.';
GO
