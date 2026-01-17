-- =============================================
-- Guest Details Report
-- Description:
--   Adds sp_GetGuestDetailsReport for guest-wise stay and revenue summary.
--   Date filter is applied on Bookings.CheckInDate.
-- Result sets:
--   A) Summary KPIs
--   B) Guest rows
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetGuestDetailsReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetGuestDetailsReport;
GO

CREATE PROCEDURE sp_GetGuestDetailsReport
    @BranchID INT,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF (@FromDate IS NULL) SET @FromDate = CAST(GETDATE() AS DATE);
    IF (@ToDate IS NULL) SET @ToDate = CAST(GETDATE() AS DATE);

    IF (@ToDate < @FromDate)
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate = @Tmp;
    END

    IF OBJECT_ID('tempdb..#B') IS NOT NULL DROP TABLE #B;
    CREATE TABLE #B (
        GuestKey NVARCHAR(200) NOT NULL,
        GuestName NVARCHAR(250) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(200) NULL,
        BookingId INT NOT NULL,
        Nights INT NOT NULL,
        Revenue DECIMAL(18,2) NOT NULL,
        Balance DECIMAL(18,2) NOT NULL,
        LastStayDate DATE NOT NULL
    );

    INSERT INTO #B(GuestKey, GuestName, Phone, Email, BookingId, Nights, Revenue, Balance, LastStayDate)
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)), ''), NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)), ''), CAST(b.Id AS NVARCHAR(50))) AS GuestKey,
        LTRIM(RTRIM(COALESCE(NULLIF(b.PrimaryGuestFirstName, ''), '') + CASE WHEN NULLIF(b.PrimaryGuestLastName, '') IS NULL THEN '' ELSE ' ' + b.PrimaryGuestLastName END)) AS GuestName,
        NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)), '') AS Phone,
        NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)), '') AS Email,
        b.Id AS BookingId,
        ISNULL(b.Nights, DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate)) AS Nights,
        CAST(ISNULL(b.TotalAmount, 0) AS DECIMAL(18,2)) AS Revenue,
        CAST(ISNULL(b.BalanceAmount, 0) AS DECIMAL(18,2)) AS Balance,
        b.CheckInDate AS LastStayDate
    FROM dbo.Bookings b
    WHERE b.BranchID = @BranchID
      AND b.CheckInDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled'
      AND (
            NULLIF(LTRIM(RTRIM(b.PrimaryGuestPhone)), '') IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(b.PrimaryGuestEmail)), '') IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(b.PrimaryGuestFirstName)), '') IS NOT NULL
      );

    -- A) Summary
    SELECT
        COUNT(DISTINCT GuestKey) AS TotalGuests,
        COUNT(DISTINCT BookingId) AS TotalBookings,
        ISNULL(SUM(Nights), 0) AS TotalNights,
        ISNULL(SUM(Revenue), 0) AS TotalRevenue,
        ISNULL(SUM(Balance), 0) AS TotalBalance
    FROM #B;

    -- B) Guest rows
    SELECT
        x.GuestName,
        x.Phone,
        x.Email,
        ISNULL(g.Address, NULL) AS Address,
        ISNULL(g.City, NULL) AS City,
        ISNULL(g.State, NULL) AS State,
        ISNULL(g.Country, NULL) AS Country,
        COUNT(DISTINCT x.BookingId) AS BookingCount,
        ISNULL(SUM(x.Nights), 0) AS TotalNights,
        ISNULL(SUM(x.Revenue), 0) AS TotalRevenue,
        ISNULL(SUM(x.Balance), 0) AS TotalBalance,
        MAX(x.LastStayDate) AS LastStayDate
    FROM #B x
    LEFT JOIN dbo.Guests g
      ON (NULLIF(LTRIM(RTRIM(g.Phone)), '') IS NOT NULL AND NULLIF(LTRIM(RTRIM(x.Phone)), '') = NULLIF(LTRIM(RTRIM(g.Phone)), ''))
      OR (NULLIF(LTRIM(RTRIM(g.Email)), '') IS NOT NULL AND NULLIF(LTRIM(RTRIM(x.Email)), '') = NULLIF(LTRIM(RTRIM(g.Email)), ''))
    WHERE (g.BranchID = @BranchID OR g.BranchID IS NULL)
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

PRINT 'Stored Procedure sp_GetGuestDetailsReport created successfully';
GO
