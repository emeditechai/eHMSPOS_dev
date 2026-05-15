-- ============================================================
-- Script 157: Add RefundNumber to BanquetCancellations
--             and update sp_GetBanquetCollectionRegister to
--             include cancellation refunds via BanquetBookingPayments
-- ============================================================
SET NOCOUNT ON;
GO

-- 1. Add RefundNumber column to BanquetCancellations (idempotent)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.BanquetCancellations')
      AND name = 'RefundNumber'
)
BEGIN
    ALTER TABLE dbo.BanquetCancellations
        ADD RefundNumber NVARCHAR(50) NULL;
    PRINT 'Added RefundNumber column to BanquetCancellations';
END
ELSE
    PRINT 'RefundNumber already exists; skipping.';
GO

-- 2. Recreate sp_GetBanquetCollectionRegister to include refund payments
--    (refunds inserted into BanquetBookingPayments with IsRefund=1 are
--     already captured automatically — this script ensures the SP
--     counts them correctly in TotalReceipts and Daily Receipts)
IF OBJECT_ID('dbo.sp_GetBanquetCollectionRegister', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBanquetCollectionRegister;
GO

CREATE PROCEDURE dbo.sp_GetBanquetCollectionRegister
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Summary
    SELECT
        COUNT(*)                                                        AS TotalReceipts,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END)       AS TotalCollected,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END)       AS TotalRefunded,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.DiscountAmount ELSE 0 END) AS TotalDiscount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;

    -- Daily Totals
    SELECT
        CAST(bp.PaidOn AS DATE)                                              AS CollectionDate,
        COUNT(*)                                                             AS ReceiptCount,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END)            AS CollectedAmount,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END)            AS RefundedAmount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY CAST(bp.PaidOn AS DATE);

    -- Detail rows
    SELECT
        CAST(bp.PaidOn AS DATE)        AS CollectionDate,
        bp.ReceiptNumber,
        bb.BanquetBookingNumber,
        bb.GuestName                   AS ClientName,
        CASE WHEN bb.CustomerType = 'B2B'
             THEN ISNULL(bb.CompanyName, bb.GuestName)
             ELSE bb.GuestName
        END                            AS BilledTo,
        bv.VenueName,
        bb.EventDate,
        et.EventTypeName,
        bp.PaymentMethod,
        bp.Amount,
        CASE WHEN bp.IsRefund = 0 THEN bp.DiscountAmount ELSE 0 END AS DiscountAmount,
        bp.IsAdvancePayment,
        bp.IsRefund,
        u.FirstName + ' ' + u.LastName AS CollectedBy
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb    ON bb.Id   = bp.BanquetBookingId
    INNER JOIN dbo.BanquetVenues bv      ON bv.Id   = bb.VenueId
    INNER JOIN dbo.BanquetEventTypes et  ON et.Id   = bb.EventTypeId
    LEFT  JOIN dbo.Users u               ON u.Id    = bp.CreatedBy
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    ORDER BY bp.PaidOn, bp.ReceiptNumber;
END
GO

PRINT 'Script 157 completed successfully.';
GO
