-- ============================================================
-- Script 160: Update sp_GetBanquetGSTRegister to filter
--             by payment received date instead of event date.
-- This ensures bookings appear in the GST register for the
-- period in which payment was actually received.
-- ============================================================

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

    -- Helper: first payment date received in the requested range for each booking
    -- (shown as PaymentDate in the result set)

    -- Venue line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        (
            SELECT MIN(CAST(bp.PaidOn AS DATE))
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        ) AS PaymentDate,
        CASE WHEN bb.CustomerType = 'B2B'
             THEN ISNULL(bb.CompanyName, bb.GuestName)
             ELSE bb.GuestName END                                          AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Venue Hire'          AS LineType,
        bv.SACCode,
        bb.VenueBaseAmount    AS TaxableValue,
        bb.VenueGSTAmount     AS TotalGST,
        bb.VenueCGSTAmount    AS CGST,
        bb.VenueSGSTAmount    AS SGST,
        bb.VenueIGSTAmount    AS IGST,
        CASE WHEN bb.IsInterState = 1
             THEN bb.VenueIGSTAmount
             ELSE bb.VenueCGSTAmount + bb.VenueSGSTAmount END               AS TaxCharged
    FROM  dbo.BanquetBookings bb
    INNER JOIN dbo.BanquetVenues bv ON bv.Id = bb.VenueId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND EXISTS (
            SELECT 1
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
          )

    UNION ALL

    -- Package line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        (
            SELECT MIN(CAST(bp.PaidOn AS DATE))
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        ) AS PaymentDate,
        CASE WHEN bb.CustomerType = 'B2B'
             THEN ISNULL(bb.CompanyName, bb.GuestName)
             ELSE bb.GuestName END                                          AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Package (' + pl.PackageName + ')'                                  AS LineType,
        pl.SACCode,
        pl.BaseAmount    AS TaxableValue,
        pl.GSTAmount     AS TotalGST,
        pl.CGSTAmount    AS CGST,
        pl.SGSTAmount    AS SGST,
        pl.IGSTAmount    AS IGST,
        CASE WHEN bb.IsInterState = 1
             THEN pl.IGSTAmount
             ELSE pl.CGSTAmount + pl.SGSTAmount END                         AS TaxCharged
    FROM  dbo.BanquetBookingPackageLines pl
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = pl.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND EXISTS (
            SELECT 1
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
          )

    UNION ALL

    -- Addon line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        (
            SELECT MIN(CAST(bp.PaidOn AS DATE))
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        ) AS PaymentDate,
        CASE WHEN bb.CustomerType = 'B2B'
             THEN ISNULL(bb.CompanyName, bb.GuestName)
             ELSE bb.GuestName END                                          AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Addon (' + al.ServiceName + ')'                                    AS LineType,
        al.SACCode,
        al.BaseAmount    AS TaxableValue,
        al.GSTAmount     AS TotalGST,
        al.CGSTAmount    AS CGST,
        al.SGSTAmount    AS SGST,
        al.IGSTAmount    AS IGST,
        CASE WHEN bb.IsInterState = 1
             THEN al.IGSTAmount
             ELSE al.CGSTAmount + al.SGSTAmount END                         AS TaxCharged
    FROM  dbo.BanquetBookingAddonLines al
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = al.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND EXISTS (
            SELECT 1
            FROM   dbo.BanquetBookingPayments bp
            WHERE  bp.BanquetBookingId = bb.Id
              AND  bp.IsRefund = 0
              AND  bp.Status IN ('Captured', 'Success')
              AND  CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
          )

    ORDER BY PaymentDate, bb.BanquetBookingNumber;
END
GO
