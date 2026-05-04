-- Migration 143: Stored Procedure for B2B E-Invoice Dashboard
-- Returns e-invoice log rows joined with Booking details
-- Filtered by checkout date range, GenerationType, and BookingNo search.

-- ── Stored Procedure ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.usp_GetB2BEInvoiceDashboard', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetB2BEInvoiceDashboard;
GO

CREATE PROCEDURE dbo.usp_GetB2BEInvoiceDashboard
    @BranchID        INT,
    @FromDate        DATE        = NULL,
    @ToDate          DATE        = NULL,
    @GenerationType  NVARCHAR(10) = NULL,   -- 'MANUAL' | 'AUTO' | NULL (all)
    @BookingNoSearch NVARCHAR(50) = NULL    -- partial match
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

        -- Booking fields
        RTRIM(ISNULL(b.PrimaryGuestFirstName, '') + ' ' + ISNULL(b.PrimaryGuestLastName, '')) AS GuestName,
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
    ORDER BY el.CreatedDate DESC;
END
GO

PRINT 'Migration 143: usp_GetB2BEInvoiceDashboard created.';
GO

-- ── Nav Menu Seed ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS_EINVOICE_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'BOOKINGS_EINVOICE_DASHBOARD',
        'E-Invoice Logs',
        'fas fa-file-code',
        'Booking',
        'EInvoiceDashboard',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'BOOKINGS'),
        38,
        1
    );
    PRINT 'BOOKINGS_EINVOICE_DASHBOARD menu item inserted.';
END
ELSE
    PRINT 'BOOKINGS_EINVOICE_DASHBOARD menu item already exists.';
GO

PRINT 'Migration 143 complete.';
GO
