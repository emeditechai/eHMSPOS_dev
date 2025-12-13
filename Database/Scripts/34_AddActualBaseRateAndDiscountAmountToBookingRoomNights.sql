-- Add per-night pricing clarity fields to BookingRoomNights
-- ActualBaseRate: pre-discount nightly room charge (per room)
-- DiscountAmount: discount applied for that night (per room)

IF COL_LENGTH('dbo.BookingRoomNights', 'ActualBaseRate') IS NULL
BEGIN
    ALTER TABLE dbo.BookingRoomNights
    ADD ActualBaseRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingRoomNights_ActualBaseRate DEFAULT(0);
END

IF COL_LENGTH('dbo.BookingRoomNights', 'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.BookingRoomNights
    ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BookingRoomNights_DiscountAmount DEFAULT(0);
END

-- Backfill historical data where possible using RateAmount + RateMaster discount.
-- Note: RateAmount is treated as after-discount. We reconstruct original by dividing.
-- If RatePlanId missing or discount is 0, we treat RateAmount as ActualBaseRate.

;WITH DiscountCTE AS (
    SELECT
        brn.Id,
        b.RatePlanId,
        brn.RateAmount,
        TRY_CONVERT(DECIMAL(18,2), rm.ApplyDiscount) AS DiscountPercent
    FROM dbo.BookingRoomNights brn
    INNER JOIN dbo.Bookings b ON b.Id = brn.BookingId
    LEFT JOIN dbo.RateMaster rm ON rm.Id = b.RatePlanId
)
UPDATE brn
SET
    ActualBaseRate = CASE
        WHEN d.DiscountPercent IS NOT NULL AND d.DiscountPercent > 0 AND (1 - (d.DiscountPercent/100.0)) > 0
            THEN ROUND(d.RateAmount / (1 - (d.DiscountPercent/100.0)), 2)
        ELSE d.RateAmount
    END,
    DiscountAmount = CASE
        WHEN d.DiscountPercent IS NOT NULL AND d.DiscountPercent > 0 AND (1 - (d.DiscountPercent/100.0)) > 0
            THEN ROUND((d.RateAmount / (1 - (d.DiscountPercent/100.0))) - d.RateAmount, 2)
        ELSE 0
    END
FROM dbo.BookingRoomNights brn
INNER JOIN DiscountCTE d ON d.Id = brn.Id;
