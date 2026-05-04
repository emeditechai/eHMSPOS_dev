-- Migration 144: Add portal push tracking columns to B2BEInvoiceJsonLogs
-- Supports future provision to push e-invoice JSON to Govt/GST portal.
-- PushStatus: NULL = Not Pushed, 'PENDING' = Queued, 'PUSHED' = Success, 'FAILED' = Error
-- PushedAt  : UTC timestamp of the last push attempt
-- PushResponse: API response / error message from the portal (NVARCHAR(MAX))

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BEInvoiceJsonLogs') AND name = 'PushStatus')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs
        ADD PushStatus NVARCHAR(20) NULL;          -- NULL | 'PENDING' | 'PUSHED' | 'FAILED'
    PRINT 'Added PushStatus column to dbo.B2BEInvoiceJsonLogs.';
END
ELSE
    PRINT 'PushStatus column already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BEInvoiceJsonLogs') AND name = 'PushedAt')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs
        ADD PushedAt DATETIME2 NULL;
    PRINT 'Added PushedAt column to dbo.B2BEInvoiceJsonLogs.';
END
ELSE
    PRINT 'PushedAt column already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.B2BEInvoiceJsonLogs') AND name = 'PushResponse')
BEGIN
    ALTER TABLE dbo.B2BEInvoiceJsonLogs
        ADD PushResponse NVARCHAR(MAX) NULL;
    PRINT 'Added PushResponse column to dbo.B2BEInvoiceJsonLogs.';
END
ELSE
    PRINT 'PushResponse column already exists.';
GO

-- Recreate the dashboard SP to include the new columns
IF OBJECT_ID('dbo.usp_GetB2BEInvoiceDashboard', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetB2BEInvoiceDashboard;
GO

CREATE PROCEDURE dbo.usp_GetB2BEInvoiceDashboard
    @BranchID        INT,
    @FromDate        DATE         = NULL,
    @ToDate          DATE         = NULL,
    @GenerationType  NVARCHAR(10) = NULL,   -- 'MANUAL' | 'AUTO' | NULL (all)
    @BookingNoSearch NVARCHAR(50) = NULL,   -- partial match
    @PushStatus      NVARCHAR(20) = NULL    -- 'PUSHED' | 'FAILED' | 'NOT_PUSHED' | NULL (all)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        el.Id,
        el.BookingId,
        el.BookingNo,
        el.InvoiceNumber,
        el.Version,
        el.GenerationType,
        el.BranchID,
        el.CreatedDate,
        el.CreatedBy,
        el.PushStatus,
        el.PushedAt,
        el.PushResponse,

        -- Booking fields
        RTRIM(ISNULL(b.PrimaryGuestFirstName,'') + ' ' + ISNULL(b.PrimaryGuestLastName,'')) AS GuestName,
        b.B2BClientName,
        b.CompanyGstNo,
        b.CheckInDate,
        b.CheckOutDate,
        b.ActualCheckOutDate,
        b.TotalAmount   AS GrandTotal,
        b.BaseAmount,
        b.TaxAmount,
        b.Status        AS BookingStatus,

        -- JSON payload (for modal viewer)
        el.JsonPayload
    FROM dbo.B2BEInvoiceJsonLogs el
    INNER JOIN dbo.Bookings b ON b.Id = el.BookingId
    WHERE
        el.BranchID = @BranchID
        AND (
            @FromDate IS NULL
            OR CAST(ISNULL(b.ActualCheckOutDate, b.CheckOutDate) AS DATE) >= @FromDate
        )
        AND (
            @ToDate IS NULL
            OR CAST(ISNULL(b.ActualCheckOutDate, b.CheckOutDate) AS DATE) <= @ToDate
        )
        AND (
            @GenerationType IS NULL OR @GenerationType = ''
            OR el.GenerationType = @GenerationType
        )
        AND (
            @BookingNoSearch IS NULL OR @BookingNoSearch = ''
            OR el.BookingNo LIKE '%' + @BookingNoSearch + '%'
        )
        AND (
            @PushStatus IS NULL OR @PushStatus = ''
            OR (@PushStatus = 'NOT_PUSHED' AND el.PushStatus IS NULL)
            OR el.PushStatus = @PushStatus
        )
    ORDER BY el.CreatedDate DESC;
END
GO

PRINT 'Migration 144 complete.';
GO
