-- Migration 145: IRP Token table + IRN response columns on B2BEInvoiceJsonLogs
-- + update usp_GetB2BEInvoiceDashboard to include IRN fields
-- Applied: 2026-05-04

-- ── 1. EInvoiceIrpTokens table ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('dbo.EInvoiceIrpTokens') AND type = 'U')
BEGIN
    CREATE TABLE dbo.EInvoiceIrpTokens (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        BranchID     INT           NOT NULL,
        SessionUserId INT           NULL,
        AccessToken  NVARCHAR(MAX) NOT NULL,
        ExpiresAt    DATETIME2     NOT NULL,
        CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy    INT           NULL
    );
    PRINT 'Created EInvoiceIrpTokens table.';
END
ELSE PRINT 'EInvoiceIrpTokens table already exists.';

-- ── 2. Add IRN columns to B2BEInvoiceJsonLogs ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'Irn')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD Irn NVARCHAR(100) NULL;
    PRINT 'Added Irn column.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'AckNo')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD AckNo NVARCHAR(50) NULL;
    PRINT 'Added AckNo column.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'AckDt')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD AckDt NVARCHAR(50) NULL;
    PRINT 'Added AckDt column.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'SignedQRCode')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD SignedQRCode NVARCHAR(MAX) NULL;
    PRINT 'Added SignedQRCode column.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'IrnRequestJson')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD IrnRequestJson NVARCHAR(MAX) NULL;
    PRINT 'Added IrnRequestJson column.';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('B2BEInvoiceJsonLogs') AND name = 'IrnResponseJson')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs ADD IrnResponseJson NVARCHAR(MAX) NULL;
    PRINT 'Added IrnResponseJson column.';
END

-- ── 3. Recreate usp_GetB2BEInvoiceDashboard with IRN fields ──────────────────
IF OBJECT_ID('dbo.usp_GetB2BEInvoiceDashboard', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetB2BEInvoiceDashboard;
GO

CREATE PROCEDURE dbo.usp_GetB2BEInvoiceDashboard
    @BranchID       INT,
    @FromDate       DATETIME  = NULL,
    @ToDate         DATETIME  = NULL,
    @GenerationType NVARCHAR(10)  = NULL,
    @BookingNoSearch NVARCHAR(50) = NULL,
    @PushStatus     NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        l.Id,
        l.BookingId,
        l.BookingNo,
        l.InvoiceNumber,
        l.Version,
        l.GenerationType,
        l.BranchID,
        l.CreatedDate,
        l.CreatedBy,
        -- Guest name
        RTRIM(ISNULL(b.PrimaryGuestFirstName,'') + ' ' + ISNULL(b.PrimaryGuestLastName,'')) AS GuestName,
        -- B2B Client
        ISNULL(bc.ClientName, b.B2BClientName) AS B2BClientName,
        ISNULL(bc.GstNo, b.CompanyGstNo)       AS CompanyGstNo,
        -- Dates
        b.CheckInDate,
        b.CheckOutDate,
        b.ActualCheckOutDate,
        -- Financials
        b.TotalAmount  AS GrandTotal,
        b.BaseAmount,
        b.TaxAmount,
        -- Status
        b.Status       AS BookingStatus,
        l.JsonPayload,
        -- Portal push / IRN fields
        l.PushStatus,
        l.PushedAt,
        l.PushResponse,
        l.Irn,
        l.AckNo,
        l.AckDt,
        l.SignedQRCode
    FROM dbo.B2BEInvoiceJsonLogs l
    INNER JOIN dbo.Bookings b
        ON b.Id = l.BookingId
    LEFT JOIN dbo.B2BClients bc
        ON bc.Id = b.B2BClientId
    WHERE
        l.BranchID = @BranchID
        AND (@FromDate       IS NULL OR CAST(b.ActualCheckOutDate AS DATE) >= CAST(@FromDate AS DATE))
        AND (@ToDate         IS NULL OR CAST(b.ActualCheckOutDate AS DATE) <= CAST(@ToDate  AS DATE))
        AND (@GenerationType IS NULL OR l.GenerationType = @GenerationType)
        AND (@BookingNoSearch IS NULL OR l.BookingNo LIKE '%' + @BookingNoSearch + '%')
        AND (@PushStatus     IS NULL OR l.PushStatus = @PushStatus)
    ORDER BY l.CreatedDate DESC;
END
GO

PRINT 'Migration 145 complete.';
