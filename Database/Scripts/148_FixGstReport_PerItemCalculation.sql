SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- Script  : 148_FixGstReport_PerItemCalculation.sql
-- Purpose : sp_GetGstReport was using a proportional back-calculation:
--             TaxableValue = Payment × (BookingBaseAmount / BookingGross)
--           This gives wrong values when OC/RS items have mixed GST rates,
--           e.g., a ₹1,000 + 12% GST = ₹1,120 payment was showing
--           ₹1,018.18 taxable instead of ₹1,000 because the rate was
--           blended with other OC items that had different GST rates.
--
--   Fix:
--   • Stay Charges (BillingHead='S'/NULL): keep per-payment rows,
--     proportional to booking GST amounts (correct — full night
--     payments have exact proportions).
--   • Other Charges (BillingHead='O'): show one row per
--     BookingOtherCharges item using ACTUAL amounts.
--     Filter by oc.ChargeDate in the report date range.
--   • Room Services (BillingHead='R'): show one row per RoomServices
--     item using ACTUAL amounts.
--     Filter by rs.CreatedAt date in the report date range.
--
--   Summary totals updated to match (actual amounts, not proportional).
-- ============================================================

USE HMS_dev;
GO

IF OBJECT_ID('dbo.sp_GetGstReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGstReport;
GO

CREATE PROCEDURE dbo.sp_GetGstReport
(
    @BranchID INT,
    @FromDate  DATE = NULL,
    @ToDate    DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = CAST(GETDATE() AS DATE);
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    IF @ToDate < @FromDate
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate   = @Tmp;
    END;

    -- ── SUMMARY ROW ──────────────────────────────────────────────────────────
    SELECT
        -- Total unique bookings with any payment in range
        ISNULL((
            SELECT COUNT(DISTINCT b.Id)
            FROM   dbo.BookingPayments bp
            INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
            WHERE  b.BranchID = @BranchID
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
              AND  bp.Status IN ('Completed','Captured','Success')
              AND  ISNULL(b.Status,'') <> 'Cancelled'
        ), 0) AS TotalBookings,

        -- Actual cash collected (all billing heads)
        ROUND(ISNULL((
            SELECT SUM(bp.Amount)
            FROM   dbo.BookingPayments bp
            INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
            WHERE  b.BranchID = @BranchID
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
              AND  bp.Status IN ('Completed','Captured','Success')
              AND  ISNULL(b.Status,'') <> 'Cancelled'
        ), 0), 2) AS TotalPaidAmount,

        -- CGST: Stay (proportional) + OC (actual) + RS (actual)
        ROUND(
            ISNULL((
                SELECT SUM(bp.Amount * ISNULL(b.CGSTAmount,0) / NULLIF(b.TotalAmount,0))
                FROM   dbo.BookingPayments bp
                INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
                  AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  bp.Status IN ('Completed','Captured','Success')
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(oc.CGSTAmount)
                FROM   dbo.BookingOtherCharges oc
                INNER JOIN dbo.Bookings b ON b.Id = oc.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  oc.ChargeDate BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(rs.CGSTAmount)
                FROM   dbo.RoomServices rs
                INNER JOIN dbo.Bookings b ON b.Id = rs.BookingID
                WHERE  rs.BranchID = @BranchID
                  AND  CAST(rs.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
        , 2) AS TotalCGST,

        -- SGST: Stay (proportional) + OC (actual) + RS (actual)
        ROUND(
            ISNULL((
                SELECT SUM(bp.Amount * ISNULL(b.SGSTAmount,0) / NULLIF(b.TotalAmount,0))
                FROM   dbo.BookingPayments bp
                INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
                  AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  bp.Status IN ('Completed','Captured','Success')
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(oc.SGSTAmount)
                FROM   dbo.BookingOtherCharges oc
                INNER JOIN dbo.Bookings b ON b.Id = oc.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  oc.ChargeDate BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(rs.SGSTAmount)
                FROM   dbo.RoomServices rs
                INNER JOIN dbo.Bookings b ON b.Id = rs.BookingID
                WHERE  rs.BranchID = @BranchID
                  AND  CAST(rs.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
        , 2) AS TotalSGST,

        -- Total GST: Stay (proportional) + OC (actual) + RS (actual)
        ROUND(
            ISNULL((
                SELECT SUM(bp.Amount * ISNULL(b.TaxAmount,0) / NULLIF(b.TotalAmount,0))
                FROM   dbo.BookingPayments bp
                INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
                  AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  bp.Status IN ('Completed','Captured','Success')
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(oc.GSTAmount)
                FROM   dbo.BookingOtherCharges oc
                INNER JOIN dbo.Bookings b ON b.Id = oc.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  oc.ChargeDate BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(rs.GSTAmount)
                FROM   dbo.RoomServices rs
                INNER JOIN dbo.Bookings b ON b.Id = rs.BookingID
                WHERE  rs.BranchID = @BranchID
                  AND  CAST(rs.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
        , 2) AS TotalGST,

        -- Total Taxable Value: Stay (proportional) + OC (actual base) + RS (actual NetAmount)
        ROUND(
            ISNULL((
                SELECT SUM(bp.Amount - bp.Amount * ISNULL(b.TaxAmount,0) / NULLIF(b.TotalAmount,0))
                FROM   dbo.BookingPayments bp
                INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
                  AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  bp.Status IN ('Completed','Captured','Success')
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(oc.Rate * CASE WHEN ISNULL(oc.Qty,0) <= 0 THEN 1 ELSE oc.Qty END)
                FROM   dbo.BookingOtherCharges oc
                INNER JOIN dbo.Bookings b ON b.Id = oc.BookingId
                WHERE  b.BranchID = @BranchID
                  AND  oc.ChargeDate BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
            + ISNULL((
                SELECT SUM(rs.NetAmount)
                FROM   dbo.RoomServices rs
                INNER JOIN dbo.Bookings b ON b.Id = rs.BookingID
                WHERE  rs.BranchID = @BranchID
                  AND  CAST(rs.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
                  AND  ISNULL(b.Status,'') <> 'Cancelled'
            ), 0)
        , 2) AS TotalTaxableValue;

    -- ── DETAILS: per-item rows ordered by date desc ───────────────────────────
    SELECT
        PaymentDate,
        PaidOn,
        BookingNumber,
        GuestName,
        RoomType,
        BillingHead,
        CGSTAmount,
        SGSTAmount,
        GSTAmount,
        TaxableValue,
        BookingStatus  AS [Status],
        PaymentStatus,
        PaidAmount,
        CreatedBy
    FROM
    (
        -- ── PART 1: Stay Charges (per payment, proportional to booking amounts) ──
        SELECT
            CAST(bp.PaidOn AS DATE)                                                       AS PaymentDate,
            bp.PaidOn,
            b.BookingNumber,
            CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)                  AS GuestName,
            rt.TypeName                                                                    AS RoomType,
            'Stay Charges'                                                                 AS BillingHead,
            ROUND(bp.Amount * ISNULL(b.CGSTAmount,0) / NULLIF(b.TotalAmount,0), 2)       AS CGSTAmount,
            ROUND(bp.Amount * ISNULL(b.SGSTAmount,0) / NULLIF(b.TotalAmount,0), 2)       AS SGSTAmount,
            ROUND(bp.Amount * ISNULL(b.TaxAmount,0)  / NULLIF(b.TotalAmount,0), 2)       AS GSTAmount,
            ROUND(bp.Amount - bp.Amount * ISNULL(b.TaxAmount,0) / NULLIF(b.TotalAmount,0), 2) AS TaxableValue,
            b.Status                                                                       AS BookingStatus,
            bp.Status                                                                      AS PaymentStatus,
            bp.Amount                                                                      AS PaidAmount,
            CAST(ISNULL(u.FullName, CAST(bp.CreatedBy AS NVARCHAR(200))) AS NVARCHAR(200)) AS CreatedBy
        FROM   dbo.BookingPayments bp
        INNER JOIN dbo.Bookings    b  ON b.Id  = bp.BookingId
        LEFT  JOIN dbo.RoomTypes   rt ON rt.Id = b.RoomTypeId
        LEFT  JOIN dbo.Users        u  ON u.Id  = TRY_CAST(bp.CreatedBy AS INT)
        WHERE  b.BranchID = @BranchID
          AND  (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
          AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
          AND  bp.Status IN ('Completed','Captured','Success')
          AND  ISNULL(b.Status,'') <> 'Cancelled'

        UNION ALL

        -- ── PART 2: Other Charges — one row per OC item (ACTUAL amounts) ──────
        -- Filtered by ChargeDate so each item shows on the day it was charged.
        -- TaxableValue = Rate × Qty (actual base, not proportional).
        -- PaidAmount   = Rate × Qty + GSTAmount (actual gross of this item).
        SELECT
            oc.ChargeDate                                                                  AS PaymentDate,
            CAST(oc.ChargeDate AS DATETIME)                                                AS PaidOn,
            b.BookingNumber,
            CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)                  AS GuestName,
            rt.TypeName                                                                    AS RoomType,
            'Other Charges'                                                                AS BillingHead,
            oc.CGSTAmount,
            oc.SGSTAmount,
            oc.GSTAmount,
            ROUND(oc.Rate * CASE WHEN ISNULL(oc.Qty,0) <= 0 THEN 1 ELSE oc.Qty END, 2)  AS TaxableValue,
            b.Status                                                                       AS BookingStatus,
            'Captured'                                                                     AS PaymentStatus,
            ROUND(oc.Rate * CASE WHEN ISNULL(oc.Qty,0) <= 0 THEN 1 ELSE oc.Qty END
                  + oc.GSTAmount, 2)                                                       AS PaidAmount,
            CAST(NULL AS NVARCHAR(200))                                                    AS CreatedBy
        FROM   dbo.BookingOtherCharges oc
        INNER JOIN dbo.Bookings  b  ON b.Id  = oc.BookingId
        LEFT  JOIN dbo.RoomTypes rt ON rt.Id = b.RoomTypeId
        WHERE  b.BranchID = @BranchID
          AND  oc.ChargeDate BETWEEN @FromDate AND @ToDate
          AND  ISNULL(b.Status,'') <> 'Cancelled'

        UNION ALL

        -- ── PART 3: Room Services — one row per RS item (ACTUAL amounts) ──────
        SELECT
            CAST(rs.CreatedAt AS DATE)                                                     AS PaymentDate,
            rs.CreatedAt                                                                   AS PaidOn,
            b.BookingNumber,
            CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)                  AS GuestName,
            rt.TypeName                                                                    AS RoomType,
            'Room Services'                                                                AS BillingHead,
            rs.CGSTAmount,
            rs.SGSTAmount,
            rs.GSTAmount,
            rs.NetAmount                                                                   AS TaxableValue,
            b.Status                                                                       AS BookingStatus,
            CASE WHEN rs.IsSettled = 1 THEN 'Captured' ELSE 'Pending' END                 AS PaymentStatus,
            rs.ActualBillAmount                                                            AS PaidAmount,
            CAST(NULL AS NVARCHAR(200))                                                    AS CreatedBy
        FROM   dbo.RoomServices rs
        INNER JOIN dbo.Bookings  b  ON b.Id  = rs.BookingID
        LEFT  JOIN dbo.RoomTypes rt ON rt.Id = b.RoomTypeId
        WHERE  rs.BranchID = @BranchID
          AND  CAST(rs.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
          AND  ISNULL(b.Status,'') <> 'Cancelled'
    ) AS AllRows
    ORDER BY PaidOn DESC, BookingNumber;
END;
GO

PRINT 'sp_GetGstReport updated: per-item accurate GST calculation for OC and RS';
GO
