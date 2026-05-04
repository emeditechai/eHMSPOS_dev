-- =============================================================================
-- Script  : 146_FixBalanceAmountExcludeNonStayPayments.sql
-- Purpose : BalanceAmount on Bookings tracked only Stay charges (BillingHead='S').
--           Payments for Other Charges ('O') and Room Services were incorrectly
--           subtracted from BalanceAmount, causing it to go negative for fully-paid
--           bookings. This script recalculates BalanceAmount using Stay payments only
--           for any booking that has non-Stay payments.
-- =============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

UPDATE b
SET b.BalanceAmount = b.TotalAmount
    + ISNULL((
        SELECT SUM(CASE WHEN ISNULL(bp.IsRoundOffApplied, 0) = 1 THEN ISNULL(bp.RoundOffAmount, 0) ELSE 0 END)
        FROM BookingPayments bp
        WHERE bp.BookingId = b.Id
          AND (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
    ), 0)
    - ISNULL((
        SELECT SUM(bp.Amount + ISNULL(bp.DiscountAmount, 0))
        FROM BookingPayments bp
        WHERE bp.BookingId = b.Id
          AND (bp.BillingHead = 'S' OR bp.BillingHead IS NULL)
    ), 0)
FROM dbo.Bookings b
WHERE EXISTS (
    SELECT 1
    FROM BookingPayments bp
    WHERE bp.BookingId = b.Id
      AND bp.BillingHead IS NOT NULL
      AND bp.BillingHead <> 'S'
);

-- Report affected rows
SELECT
    b.BookingNumber,
    b.PrimaryGuestFirstName + ' ' + b.PrimaryGuestLastName AS GuestName,
    b.TotalAmount,
    b.BalanceAmount AS CorrectedBalanceAmount,
    b.PaymentStatus
FROM dbo.Bookings b
WHERE EXISTS (
    SELECT 1
    FROM BookingPayments bp
    WHERE bp.BookingId = b.Id
      AND bp.BillingHead IS NOT NULL
      AND bp.BillingHead <> 'S'
)
ORDER BY b.Id;
