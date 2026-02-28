-- =============================================
-- Migration 110: Fix Guest Details Report — filter by CheckInDate, keep JOIN fix,
--                include Cancelled bookings
--
-- Background:
--   Migration 109 switched to CreatedDate (booking-creation date) which
--   only returns bookings made on the selected days, not bookings whose
--   STAY falls within the selected period — causing most records to be missed.
--
--   This migration reverts the date filter back to CheckInDate and also
--   removes the Cancelled exclusion so cancelled booking guests appear too.
--   A CancelledBookings count is added to the summary and per-guest rows.
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetGuestDetailsReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetGuestDetailsReport;
GO

CREATE PROCEDURE sp_GetGuestDetailsReport
    @BranchID INT,
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF (@FromDate IS NULL) SET @FromDate = CAST(GETDATE() AS DATE);
    IF (@ToDate IS NULL)   SET @ToDate   = CAST(GETDATE() AS DATE);

    IF (@ToDate < @FromDate)
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate = @Tmp;
    END

    IF OBJECT_ID('tempdb..#B') IS NOT NULL DROP TABLE #B;
    CREATE TABLE #B (
        GuestKey      NVARCHAR(200)  NOT NULL,
        GuestName     NVARCHAR(250)  NOT NULL,
        Phone         NVARCHAR(50)   NULL,
        Email         NVARCHAR(200)  NULL,
        BookingId     INT            NOT NULL,
        Nights        INT            NOT NULL,
        Revenue       DECIMAL(18,2)  NOT NULL,
        Balance       DECIMAL(18,2)  NOT NULL,
        CheckInDate   DATE           NOT NULL,
        CheckOutDate  DATE           NULL,
        IsCancelled   BIT            NOT NULL DEFAULT 0
    );

    -- -----------------------------------------------------------------------
    -- Load ALL bookings (including Cancelled) for the branch whose
    -- CHECK-IN DATE falls in the selected range.
    -- -----------------------------------------------------------------------
    INSERT INTO #B (GuestKey, GuestName, Phone, Email, BookingId, Nights, Revenue, Balance, CheckInDate, CheckOutDate, IsCancelled)
    SELECT
        COALESCE(
            NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)), ''),
            NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)), ''),
            CAST(b.Id AS NVARCHAR(50))
        ) AS GuestKey,
        LTRIM(RTRIM(
            COALESCE(NULLIF(b.PrimaryGuestFirstName, ''), '')
            + CASE WHEN NULLIF(b.PrimaryGuestLastName, '') IS NULL THEN '' ELSE ' ' + b.PrimaryGuestLastName END
        )) AS GuestName,
        NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)), '')  AS Phone,
        NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)), '')  AS Email,
        b.Id                                            AS BookingId,
        ISNULL(b.Nights, DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate)) AS Nights,
        CAST(ISNULL(b.TotalAmount,   0) AS DECIMAL(18,2)) AS Revenue,
        CAST(ISNULL(b.BalanceAmount, 0) AS DECIMAL(18,2)) AS Balance,
        CAST(b.CheckInDate AS DATE)                     AS CheckInDate,
        TRY_CAST(b.CheckOutDate AS DATE)                AS CheckOutDate,
        CASE WHEN ISNULL(b.Status, '') IN ('Cancelled', 'Canceled') THEN 1 ELSE 0 END AS IsCancelled
    FROM dbo.Bookings b
    WHERE b.BranchID = @BranchID
      AND CAST(b.CheckInDate AS DATE) BETWEEN @FromDate AND @ToDate
      AND (
               NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)),     '') IS NOT NULL
            OR NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)),     '') IS NOT NULL
            OR NULLIF(LTRIM(RTRIM(b.PrimaryGuestFirstName)), '') IS NOT NULL
          );

    -- -----------------------------------------------------------------------
    -- A) Summary KPIs
    -- -----------------------------------------------------------------------
    SELECT
        COUNT(DISTINCT GuestKey)                          AS TotalGuests,
        COUNT(DISTINCT BookingId)                         AS TotalBookings,
        SUM(CASE WHEN IsCancelled = 1 THEN 1 ELSE 0 END) AS CancelledBookings,
        ISNULL(SUM(CASE WHEN IsCancelled = 0 THEN Nights  ELSE 0 END), 0) AS TotalNights,
        ISNULL(SUM(CASE WHEN IsCancelled = 0 THEN Revenue ELSE 0 END), 0) AS TotalRevenue,
        ISNULL(SUM(CASE WHEN IsCancelled = 0 THEN Balance ELSE 0 END), 0) AS TotalBalance
    FROM #B;

    -- -----------------------------------------------------------------------
    -- B) Guest rows
    --    Branch filter is part of the JOIN (not WHERE) so that bookings
    --    without a Guests-master record are NEVER dropped.
    -- -----------------------------------------------------------------------
    SELECT
        x.GuestName,
        x.Phone,
        x.Email,
        g.Address,
        g.City,
        g.State,
        g.Country,
        COUNT(DISTINCT x.BookingId)                               AS BookingCount,
        SUM(CASE WHEN x.IsCancelled = 1 THEN 1 ELSE 0 END)       AS CancelledCount,
        ISNULL(SUM(CASE WHEN x.IsCancelled = 0 THEN x.Nights  ELSE 0 END), 0) AS TotalNights,
        ISNULL(SUM(CASE WHEN x.IsCancelled = 0 THEN x.Revenue ELSE 0 END), 0) AS TotalRevenue,
        ISNULL(SUM(CASE WHEN x.IsCancelled = 0 THEN x.Balance ELSE 0 END), 0) AS TotalBalance,
        MAX(x.CheckInDate)                                        AS LastStayDate
    FROM #B x
    LEFT JOIN dbo.Guests g
        ON  (g.BranchID = @BranchID OR g.BranchID IS NULL)
        AND (
                (NULLIF(LTRIM(RTRIM(g.Phone)), '') IS NOT NULL
                 AND NULLIF(LTRIM(RTRIM(x.Phone)), '') = NULLIF(LTRIM(RTRIM(g.Phone)), ''))
             OR
                (NULLIF(LTRIM(RTRIM(g.Email)), '') IS NOT NULL
                 AND NULLIF(LTRIM(RTRIM(x.Email)), '') = NULLIF(LTRIM(RTRIM(g.Email)), ''))
            )
    GROUP BY
        x.GuestName,
        x.Phone,
        x.Email,
        g.Address,
        g.City,
        g.State,
        g.Country
    ORDER BY TotalRevenue DESC, BookingCount DESC, x.GuestName;

    DROP TABLE #B;
END;
GO

PRINT 'Stored Procedure sp_GetGuestDetailsReport updated — all bookings incl. Cancelled, filter by CheckInDate, JOIN bug fixed (Migration 110)';
GO
