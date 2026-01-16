-- =============================================
-- Enhance Business Analytics Dashboard (ADR / RevPAR)
-- Description:
--   Recreates sp_GetBusinessAnalyticsDashboard to include:
--     - Room revenue + room tax totals
--     - ADR (Avg Daily Rate)
--     - RevPAR (Revenue per Available Room)
--   Keeps existing result sets (Summary, Daily Trend, Payment Method Summary).
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetBusinessAnalyticsDashboard', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetBusinessAnalyticsDashboard;
GO

CREATE PROCEDURE sp_GetBusinessAnalyticsDashboard
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

    DECLARE @RoomCount INT = (
        SELECT COUNT(1)
        FROM dbo.Rooms r
        WHERE r.BranchID = @BranchID
          AND ISNULL(r.IsActive, 1) = 1
    );

    DECLARE @Days INT = DATEDIFF(DAY, @FromDate, @ToDate) + 1;
    DECLARE @AvailableRoomNights INT = ISNULL(@RoomCount, 0) * ISNULL(@Days, 0);

    IF OBJECT_ID('tempdb..#Dates') IS NOT NULL DROP TABLE #Dates;
    CREATE TABLE #Dates ([Date] DATE NOT NULL PRIMARY KEY);

    DECLARE @d DATE = @FromDate;
    WHILE (@d <= @ToDate)
    BEGIN
        INSERT INTO #Dates([Date]) VALUES (@d);
        SET @d = DATEADD(DAY, 1, @d);
    END

    IF OBJECT_ID('tempdb..#Sold') IS NOT NULL DROP TABLE #Sold;
    CREATE TABLE #Sold (
        [Date] DATE NOT NULL PRIMARY KEY,
        SoldRoomNights INT NOT NULL,
        BookingCount INT NOT NULL,
        RoomRevenue DECIMAL(18,2) NOT NULL,
        RoomTax DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Sold([Date], SoldRoomNights, BookingCount, RoomRevenue, RoomTax)
    SELECT
        brn.StayDate AS [Date],
        COUNT(1) AS SoldRoomNights,
        COUNT(DISTINCT brn.BookingId) AS BookingCount,
        ISNULL(SUM(ISNULL(brn.RateAmount, 0)), 0) AS RoomRevenue,
        ISNULL(SUM(ISNULL(brn.TaxAmount, 0)), 0) AS RoomTax
    FROM dbo.BookingRoomNights brn
    INNER JOIN dbo.Bookings b ON b.Id = brn.BookingId
    WHERE b.BranchID = @BranchID
      AND brn.StayDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled'
    GROUP BY brn.StayDate;

    IF OBJECT_ID('tempdb..#Collected') IS NOT NULL DROP TABLE #Collected;
    CREATE TABLE #Collected (
        [Date] DATE NOT NULL PRIMARY KEY,
        ReceiptCount INT NOT NULL,
        CollectedAmount DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #Collected([Date], ReceiptCount, CollectedAmount)
    SELECT
        CAST(bp.PaidOn AS DATE) AS [Date],
        COUNT(1) AS ReceiptCount,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount ELSE 0 END), 0) AS CollectedAmount
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(bp.PaidOn AS DATE);

    IF OBJECT_ID('tempdb..#BookingsInRange') IS NOT NULL DROP TABLE #BookingsInRange;
    CREATE TABLE #BookingsInRange (Id INT NOT NULL PRIMARY KEY);

    INSERT INTO #BookingsInRange(Id)
    SELECT DISTINCT b.Id
    FROM dbo.BookingRoomNights brn
    INNER JOIN dbo.Bookings b ON b.Id = brn.BookingId
    WHERE b.BranchID = @BranchID
      AND brn.StayDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled';

    DECLARE @TotalSoldNights INT = (SELECT ISNULL(SUM(SoldRoomNights), 0) FROM #Sold);
    DECLARE @TotalRoomRevenue DECIMAL(18,2) = (SELECT ISNULL(SUM(RoomRevenue), 0) FROM #Sold);

    -- A) Summary
    SELECT
        @Days AS TotalDays,
        @RoomCount AS TotalRooms,
        @TotalSoldNights AS SoldRoomNights,
        @AvailableRoomNights AS AvailableRoomNights,
        CAST(
            CASE WHEN @AvailableRoomNights <= 0 THEN 0
                 ELSE ROUND((@TotalSoldNights * 100.0) / @AvailableRoomNights, 2)
            END
        AS DECIMAL(10,2)) AS OccupancyPercent,
        (SELECT COUNT(1) FROM #BookingsInRange) AS TotalBookings,
        @TotalRoomRevenue AS TotalRoomRevenue,
        (SELECT ISNULL(SUM(RoomTax), 0) FROM #Sold) AS TotalRoomTax,
        CAST(CASE WHEN @TotalSoldNights <= 0 THEN 0 ELSE ROUND(@TotalRoomRevenue / @TotalSoldNights, 2) END AS DECIMAL(18,2)) AS Adr,
        CAST(CASE WHEN @AvailableRoomNights <= 0 THEN 0 ELSE ROUND(@TotalRoomRevenue / @AvailableRoomNights, 2) END AS DECIMAL(18,2)) AS RevPar,
        (SELECT ISNULL(SUM(CollectedAmount), 0) FROM #Collected) AS TotalCollected,
        ISNULL(
            (
                SELECT
                    SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success')
                             THEN (bp.Amount * ISNULL(b.TaxAmount, 0) / NULLIF(b.TotalAmount, 0))
                             ELSE 0 END)
                FROM dbo.BookingPayments bp
                INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
                WHERE b.BranchID = @BranchID
                  AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
            ), 0
        ) AS TotalGST,
        ISNULL(
            (
                SELECT SUM(ISNULL(b.BalanceAmount, 0))
                FROM dbo.Bookings b
                INNER JOIN #BookingsInRange bir ON bir.Id = b.Id
            ), 0
        ) AS TotalBalance;

    -- B) Daily Trend
    SELECT
        d.[Date] AS ReportDate,
        ISNULL(s.BookingCount, 0) AS TotalBookings,
        ISNULL(s.SoldRoomNights, 0) AS SoldRoomNights,
        CAST(
            CASE WHEN @RoomCount <= 0 THEN 0
                 ELSE ROUND((ISNULL(s.SoldRoomNights, 0) * 100.0) / @RoomCount, 2)
            END
        AS DECIMAL(10,2)) AS OccupancyPercent,
        ISNULL(s.RoomRevenue, 0) AS RoomRevenue,
        CAST(CASE WHEN ISNULL(s.SoldRoomNights, 0) <= 0 THEN 0 ELSE ROUND(ISNULL(s.RoomRevenue, 0) / s.SoldRoomNights, 2) END AS DECIMAL(18,2)) AS Adr,
        CAST(CASE WHEN @RoomCount <= 0 THEN 0 ELSE ROUND(ISNULL(s.RoomRevenue, 0) / @RoomCount, 2) END AS DECIMAL(18,2)) AS RevPar,
        ISNULL(c.ReceiptCount, 0) AS ReceiptCount,
        ISNULL(c.CollectedAmount, 0) AS CollectedAmount
    FROM #Dates d
    LEFT JOIN #Sold s ON s.[Date] = d.[Date]
    LEFT JOIN #Collected c ON c.[Date] = d.[Date]
    ORDER BY d.[Date];

    -- C) Payment Method Summary
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(bp.PaymentMethod)), ''), 'Unknown') AS PaymentMethod,
        COUNT(1) AS ReceiptCount,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount ELSE 0 END), 0) AS CollectedAmount
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON b.Id = bp.BookingId
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(bp.PaymentMethod)), ''), 'Unknown')
    ORDER BY CollectedAmount DESC;

    DROP TABLE #Dates;
    DROP TABLE #Sold;
    DROP TABLE #Collected;
    DROP TABLE #BookingsInRange;
END;
GO

PRINT 'Stored Procedure sp_GetBusinessAnalyticsDashboard recreated successfully (ADR/RevPAR)';
GO
