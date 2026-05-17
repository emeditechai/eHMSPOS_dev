-- ============================================================
-- Script 163: Rebuild sp_GetBanquetGSTRegister with correct
--             industry-standard payment-based GST logic.
--
-- KEY CHANGES:
--   1. Drive from BanquetBookingPayments (actual money received),
--      NOT from booking amounts.
--   2. Group by receipt (ReceiptGroupNumber ?? ReceiptNumber)
--      and BillingHead so each head appears once per receipt.
--   3. Compute GST proportionally on the actual net amount paid:
--        Proportion     = AmountPaid / HeadGSTInclusiveTotal
--        TaxableValue   = HeadBaseAmount   × Proportion
--        CGST / SGST / IGST               × Proportion
--   4. Filter by payment date (CAST(PaidOn AS DATE) BETWEEN
--      @FromDate AND @ToDate).
--   5. Handle old payments with BillingHead IS NULL by
--      distributing against full-booking totals.
-- ============================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.sp_GetBanquetGSTRegister', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBanquetGSTRegister;
GO

CREATE PROCEDURE dbo.sp_GetBanquetGSTRegister
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Step 1: Aggregate payments per (receipt group × billing head × booking) ──
    ;WITH PaymentBase AS (
        SELECT
            ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber)      AS ReceiptGroupKey,
            bp.BillingHead,
            bb.Id                                                AS BookingId,
            bb.BanquetBookingNumber,
            MIN(bp.PaidOn)                                       AS PaidOn,
            bb.EventDate,
            bb.CustomerType,
            bb.GuestName,
            bb.CompanyName,
            bb.IsInterState,
            bv.SACCode                                           AS VenueSAC,

            -- Amount actually received for this head (net of discount)
            SUM(bp.Amount)                                       AS AmountPaid,

            -- GST-inclusive total for the relevant head (denominator for proportion)
            CASE bp.BillingHead
                WHEN 'V' THEN bb.VenueBaseAmount   + bb.VenueGSTAmount
                WHEN 'P' THEN bb.PackageBaseAmount + bb.PackageGSTAmount
                WHEN 'A' THEN bb.AddonBaseAmount   + bb.AddonGSTAmount
                ELSE          bb.TotalAmount   -- NULL head → full booking
            END AS HeadGSTIncTotal,

            -- Base (taxable) amount for this head
            CASE bp.BillingHead
                WHEN 'V' THEN bb.VenueBaseAmount
                WHEN 'P' THEN bb.PackageBaseAmount
                WHEN 'A' THEN bb.AddonBaseAmount
                ELSE          bb.TotalBaseAmount
            END AS HeadBase,

            -- GST component amounts for this head
            CASE bp.BillingHead
                WHEN 'V' THEN bb.VenueCGSTAmount
                WHEN 'P' THEN bb.PackageCGSTAmount
                WHEN 'A' THEN bb.AddonCGSTAmount
                ELSE          bb.TotalCGSTAmount
            END AS HeadCGST,

            CASE bp.BillingHead
                WHEN 'V' THEN bb.VenueSGSTAmount
                WHEN 'P' THEN bb.PackageSGSTAmount
                WHEN 'A' THEN bb.AddonSGSTAmount
                ELSE          bb.TotalSGSTAmount
            END AS HeadSGST,

            CASE bp.BillingHead
                WHEN 'V' THEN bb.VenueIGSTAmount
                WHEN 'P' THEN bb.PackageIGSTAmount
                WHEN 'A' THEN bb.AddonIGSTAmount
                ELSE          bb.TotalIGSTAmount
            END AS HeadIGST

        FROM dbo.BanquetBookingPayments bp
        INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
        INNER JOIN dbo.BanquetVenues   bv ON bv.Id = bb.VenueId
        WHERE bb.BranchID = @BranchID
          AND bp.[Status]  IN ('Captured', 'Success')
          AND bp.IsRefund  = 0
          AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        GROUP BY
            ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber),
            bp.BillingHead,
            bb.Id, bb.BanquetBookingNumber, bb.EventDate,
            bb.CustomerType, bb.GuestName, bb.CompanyName, bb.IsInterState,
            bv.SACCode,
            bb.VenueBaseAmount,   bb.VenueGSTAmount,
            bb.VenueCGSTAmount,   bb.VenueSGSTAmount,   bb.VenueIGSTAmount,
            bb.PackageBaseAmount, bb.PackageGSTAmount,
            bb.PackageCGSTAmount, bb.PackageSGSTAmount, bb.PackageIGSTAmount,
            bb.AddonBaseAmount,   bb.AddonGSTAmount,
            bb.AddonCGSTAmount,   bb.AddonSGSTAmount,   bb.AddonIGSTAmount,
            bb.TotalAmount, bb.TotalBaseAmount,
            bb.TotalCGSTAmount,   bb.TotalSGSTAmount,   bb.TotalIGSTAmount
    )

    -- ── Step 2: Compute proportional GST on the actual payment ───────────────
    SELECT
        pb.ReceiptGroupKey                                              AS ReceiptNumber,
        pb.BanquetBookingNumber,
        CAST(pb.PaidOn AS DATE)                                        AS PaymentDate,
        pb.EventDate,
        CASE WHEN pb.CustomerType = 'B2B'
             THEN ISNULL(pb.CompanyName, pb.GuestName)
             ELSE pb.GuestName
        END                                                            AS ClientName,
        pb.CustomerType,
        CASE WHEN pb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,

        -- Billing head label
        CASE pb.BillingHead
            WHEN 'V' THEN 'Venue Hire'
            WHEN 'P' THEN 'Package Charges'
            WHEN 'A' THEN 'Addon Services'
            ELSE          'General Payment'
        END                                                            AS LineType,

        -- SAC code per head
        CASE pb.BillingHead
            WHEN 'V' THEN pb.VenueSAC
            WHEN 'P' THEN (SELECT TOP 1 SACCode
                           FROM dbo.BanquetBookingPackageLines
                           WHERE BanquetBookingId = pb.BookingId)
            WHEN 'A' THEN (SELECT CASE WHEN COUNT(DISTINCT SACCode) > 1
                                        THEN 'Multiple'
                                        ELSE MAX(SACCode) END
                           FROM dbo.BanquetBookingAddonLines
                           WHERE BanquetBookingId = pb.BookingId)
            ELSE NULL
        END                                                            AS SACCode,

        -- Proportional taxable value on actual payment received
        ROUND(pb.HeadBase * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2) AS TaxableValue,

        -- Proportional GST components
        ROUND(pb.HeadCGST * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2) AS CGST,
        ROUND(pb.HeadSGST * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2) AS SGST,
        ROUND(pb.HeadIGST * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2) AS IGST,
        ROUND((pb.HeadCGST + pb.HeadSGST + pb.HeadIGST)
              * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2)              AS TotalGST,
        ROUND((pb.HeadCGST + pb.HeadSGST + pb.HeadIGST)
              * pb.AmountPaid / NULLIF(pb.HeadGSTIncTotal, 0), 2)              AS TaxCharged

    FROM PaymentBase pb
    ORDER BY pb.PaidOn, pb.BanquetBookingNumber, pb.BillingHead;
END
GO

PRINT 'Script 163: sp_GetBanquetGSTRegister rebuilt with payment-based proportional GST logic.';
GO
