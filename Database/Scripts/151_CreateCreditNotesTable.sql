-- ============================================================
-- Script 151 : Credit Notes table + sequence
-- Generated   : 09 May 2026
-- Purpose     : Store credit notes issued after refund processing.
--               Number format : CR-FYFY-NNNNN  (e.g. CR-2627-00001)
-- ============================================================

-- ── Sequence table (shared across branches, global serial) ──
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CreditNoteSequence')
BEGIN
    CREATE TABLE CreditNoteSequence (
        Id           INT           IDENTITY(1,1) PRIMARY KEY,
        FinancialYear NVARCHAR(10) NOT NULL,   -- e.g. '2627'
        BranchID      INT          NOT NULL DEFAULT 0,
        LastSequence  INT          NOT NULL DEFAULT 0,
        CONSTRAINT UQ_CreditNoteSequence UNIQUE (FinancialYear, BranchID)
    );
    PRINT 'CreditNoteSequence table created.';
END
ELSE
    PRINT 'CreditNoteSequence already exists — skipped.';
GO

-- ── Main credit note document table ──
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CreditNotes')
BEGIN
    CREATE TABLE CreditNotes (
        Id                    INT           IDENTITY(1,1) PRIMARY KEY,
        CreditNoteNumber      NVARCHAR(30)  NOT NULL,
        BookingId             INT           NOT NULL,
        BookingNumber         NVARCHAR(50)  NOT NULL,
        CancellationId        INT           NOT NULL,
        RefundPaymentId       INT           NULL,      -- FK to BookingPayments
        BranchID              INT           NOT NULL,
        CustomerType          NVARCHAR(10)  NOT NULL DEFAULT 'B2C',   -- B2C / B2B

        -- Guest / company info (denormalized for printing)
        GuestName             NVARCHAR(200) NOT NULL,
        GuestEmail            NVARCHAR(200) NULL,
        GuestPhone            NVARCHAR(50)  NULL,
        CompanyName           NVARCHAR(200) NULL,
        CompanyGstNo          NVARCHAR(50)  NULL,
        BillingAddress        NVARCHAR(500) NULL,

        -- Original booking context
        OriginalInvoiceNumber NVARCHAR(50)  NULL,
        CheckInDate           DATE          NOT NULL,
        CheckOutDate          DATE          NOT NULL,
        Nights                INT           NOT NULL DEFAULT 1,
        RoomType              NVARCHAR(100) NULL,

        -- Financial breakdown
        OriginalTotalAmount   DECIMAL(18,2) NOT NULL DEFAULT 0,
        RefundBaseAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        RefundCGSTAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        RefundSGSTAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
        RefundTotalAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,

        -- Cancellation info
        CancellationReason    NVARCHAR(500) NULL,
        CancellationDate      DATETIME2     NOT NULL,

        -- Refund payment
        RefundPaymentMethod   NVARCHAR(50)  NULL,
        RefundPaymentReference NVARCHAR(200) NULL,

        -- Metadata
        GeneratedAt           DATETIME2     NOT NULL DEFAULT GETDATE(),
        GeneratedBy           INT           NULL,
        IsActive              BIT           NOT NULL DEFAULT 1,

        CONSTRAINT UQ_CreditNoteNumber UNIQUE (CreditNoteNumber)
    );

    CREATE INDEX IX_CreditNotes_BookingId       ON CreditNotes (BookingId);
    CREATE INDEX IX_CreditNotes_CancellationId  ON CreditNotes (CancellationId);
    CREATE INDEX IX_CreditNotes_BranchID        ON CreditNotes (BranchID);

    PRINT 'CreditNotes table created.';
END
ELSE
    PRINT 'CreditNotes already exists — skipped.';
GO
