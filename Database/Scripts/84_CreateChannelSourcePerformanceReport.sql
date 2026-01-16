-- =============================================
-- Channel/Source Performance Report (Business Analytics)
-- Description:
--   Adds sp_GetChannelSourcePerformanceReport
--   Result sets:
--     A) Summary KPIs
--     B) Channel+Source rows
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetChannelSourcePerformanceReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetChannelSourcePerformanceReport;
GO

CREATE PROCEDURE sp_GetChannelSourcePerformanceReport
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

    IF OBJECT_ID('tempdb..#X') IS NOT NULL DROP TABLE #X;
    CREATE TABLE #X (
        BookingId INT NOT NULL,
        Channel NVARCHAR(100) NOT NULL,
        Source NVARCHAR(100) NOT NULL,
        RoomRevenue DECIMAL(18,2) NOT NULL,
        RoomTax DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #X(BookingId, Channel, Source, RoomRevenue, RoomTax)
    SELECT
        brn.BookingId,
        ISNULL(NULLIF(LTRIM(RTRIM(b.Channel)), ''), 'Unknown') AS Channel,
        ISNULL(NULLIF(LTRIM(RTRIM(b.Source)), ''), 'Unknown') AS Source,
        CAST(ISNULL(brn.RateAmount, 0) AS DECIMAL(18,2)) AS RoomRevenue,
        CAST(ISNULL(brn.TaxAmount, 0) AS DECIMAL(18,2)) AS RoomTax
    FROM dbo.BookingRoomNights brn
    INNER JOIN dbo.Bookings b ON b.Id = brn.BookingId
    WHERE b.BranchID = @BranchID
      AND brn.StayDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled';

    DECLARE @TotalSoldNights INT = (SELECT COUNT(1) FROM #X);
    DECLARE @TotalRoomRevenue DECIMAL(18,2) = (SELECT ISNULL(SUM(RoomRevenue), 0) FROM #X);

    -- A) Summary
    SELECT
        (SELECT COUNT(DISTINCT BookingId) FROM #X) AS TotalBookings,
        @TotalSoldNights AS SoldRoomNights,
        @AvailableRoomNights AS AvailableRoomNights,
        CAST(
            CASE WHEN @AvailableRoomNights <= 0 THEN 0
                 ELSE ROUND((@TotalSoldNights * 100.0) / @AvailableRoomNights, 2)
            END
        AS DECIMAL(10,2)) AS OccupancyPercent,
        @TotalRoomRevenue AS RoomRevenue,
        (SELECT ISNULL(SUM(RoomTax), 0) FROM #X) AS RoomTax,
        CAST(CASE WHEN @TotalSoldNights <= 0 THEN 0 ELSE ROUND(@TotalRoomRevenue / @TotalSoldNights, 2) END AS DECIMAL(18,2)) AS Adr,
        CAST(CASE WHEN @AvailableRoomNights <= 0 THEN 0 ELSE ROUND(@TotalRoomRevenue / @AvailableRoomNights, 2) END AS DECIMAL(18,2)) AS RevPar;

    -- B) Channel + Source rows
    SELECT
        Channel,
        Source,
        COUNT(DISTINCT BookingId) AS BookingCount,
        COUNT(1) AS SoldRoomNights,
        ISNULL(SUM(RoomRevenue), 0) AS RoomRevenue,
        ISNULL(SUM(RoomTax), 0) AS RoomTax,
        CAST(CASE WHEN COUNT(1) = 0 THEN 0 ELSE ROUND(ISNULL(SUM(RoomRevenue), 0) / COUNT(1), 2) END AS DECIMAL(18,2)) AS Adr,
        CAST(
            CASE WHEN @TotalRoomRevenue <= 0 THEN 0
                 ELSE ROUND((ISNULL(SUM(RoomRevenue), 0) * 100.0) / @TotalRoomRevenue, 2)
            END
        AS DECIMAL(10,2)) AS RevenueSharePercent
    FROM #X
    GROUP BY Channel, Source
    ORDER BY RoomRevenue DESC, Channel, Source;

    DROP TABLE #X;
END;
GO

PRINT 'Stored Procedure sp_GetChannelSourcePerformanceReport created successfully';
GO
