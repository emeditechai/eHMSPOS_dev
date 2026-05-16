-- ============================================================
-- Script 162: Rebuild sp_GetBanquetCollectionRegister to group
--             head-wise payment rows by receipt (ReceiptGroupNumber).
--             One receipt = one row in the Collection Register,
--             regardless of how many billing heads were paid.
-- ============================================================
SET NOCOUNT ON;
GO

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

    -- ── Result 1: Summary ─────────────────────────────────────────────────
    -- Count distinct receipts (ReceiptGroupNumber collapses head-wise rows)
    SELECT
        COUNT(DISTINCT ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber)) AS TotalReceipts,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END)        AS TotalCollected,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END)        AS TotalRefunded,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.DiscountAmount ELSE 0 END) AS TotalDiscount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured', 'Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;

    -- ── Result 2: Daily Totals ────────────────────────────────────────────
    SELECT
        CAST(bp.PaidOn AS DATE)                                               AS CollectionDate,
        COUNT(DISTINCT ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber))       AS ReceiptCount,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END)              AS CollectedAmount,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END)              AS RefundedAmount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured', 'Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY CAST(bp.PaidOn AS DATE);

    -- ── Result 3: Detail rows (one row per receipt) ───────────────────────
    SELECT
        CAST(MIN(bp.PaidOn) AS DATE)                                      AS CollectionDate,
        ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber)                   AS ReceiptNumber,
        bb.BanquetBookingNumber,
        bb.GuestName                                                      AS ClientName,
        CASE WHEN bb.CustomerType = 'B2B'
             THEN ISNULL(bb.CompanyName, bb.GuestName)
             ELSE bb.GuestName
        END                                                               AS BilledTo,
        bv.VenueName,
        bb.EventDate,
        et.EventTypeName,
        MAX(bp.PaymentMethod)                                             AS PaymentMethod,
        SUM(bp.Amount)                                                    AS Amount,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.DiscountAmount ELSE 0 END) AS DiscountAmount,
        CAST(MAX(CAST(bp.IsAdvancePayment AS INT)) AS BIT)               AS IsAdvancePayment,
        CAST(MAX(CAST(bp.IsRefund        AS INT)) AS BIT)                AS IsRefund,
        MAX(ISNULL(u.FirstName + ' ' + u.LastName, ''))                  AS CollectedBy
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb    ON bb.Id  = bp.BanquetBookingId
    INNER JOIN dbo.BanquetVenues bv      ON bv.Id  = bb.VenueId
    INNER JOIN dbo.BanquetEventTypes et  ON et.Id  = bb.EventTypeId
    LEFT  JOIN dbo.Users u               ON u.Id   = bp.CreatedBy
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured', 'Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY
        ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber),
        bb.Id, bb.BanquetBookingNumber, bb.GuestName, bb.CompanyName, bb.CustomerType,
        bv.VenueName, bb.EventDate, et.EventTypeName
    ORDER BY MIN(bp.PaidOn), ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber);
END
GO

PRINT 'Script 162: sp_GetBanquetCollectionRegister updated with receipt-level grouping.';
GO
