-- =============================================
-- Business Analytics Reports Stored Procedures
-- Description:
--   Adds "futuristic" business analytics reports under Reports navigation.
--   Includes:
--     1) sp_GetBusinessAnalyticsDashboard
--     2) sp_GetRoomTypePerformanceReport
--     3) sp_GetOutstandingBalanceReport
-- =============================================

USE HMS_dev;
GO

/* ============================================================
   1) Business Analytics Dashboard
   Result sets:
     A) Summary
     B) Daily Trend
     C) Payment Method Summary
============================================================ */
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
                BookingCount INT NOT NULL
        );

        INSERT INTO #Sold([Date], SoldRoomNights, BookingCount)
        SELECT
                brn.StayDate AS [Date],
                COUNT(1) AS SoldRoomNights,
                COUNT(DISTINCT brn.BookingId) AS BookingCount
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

    -- A) Summary
    SELECT
        @Days AS TotalDays,
        @RoomCount AS TotalRooms,
        (SELECT ISNULL(SUM(SoldRoomNights), 0) FROM #Sold) AS SoldRoomNights,
        @AvailableRoomNights AS AvailableRoomNights,
        CAST(
            CASE WHEN @AvailableRoomNights <= 0 THEN 0
                 ELSE ROUND(((SELECT ISNULL(SUM(SoldRoomNights), 0) FROM #Sold) * 100.0) / @AvailableRoomNights, 2)
            END
        AS DECIMAL(10,2)) AS OccupancyPercent,
        (SELECT COUNT(1) FROM #BookingsInRange) AS TotalBookings,
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
        ISNULL(c.ReceiptCount, 0) AS ReceiptCount,
        ISNULL(c.CollectedAmount, 0) AS CollectedAmount
    FROM #Dates d
    LEFT JOIN #Sold s ON s.[Date] = d.[Date]
    LEFT JOIN #Collected c ON c.[Date] = d.[Date]
    ORDER BY d.[Date]
    ;

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

PRINT 'Stored Procedure sp_GetBusinessAnalyticsDashboard created successfully';
GO


/* ============================================================
   2) Room Type Performance Report
   Result sets:
     A) Summary
     B) Room type rows
============================================================ */
IF OBJECT_ID('sp_GetRoomTypePerformanceReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetRoomTypePerformanceReport;
GO

CREATE PROCEDURE sp_GetRoomTypePerformanceReport
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

    IF OBJECT_ID('tempdb..#X') IS NOT NULL DROP TABLE #X;
    CREATE TABLE #X (
        RoomType NVARCHAR(200) NOT NULL,
        SoldNights INT NOT NULL,
        Revenue DECIMAL(18,2) NOT NULL
    );

    INSERT INTO #X(RoomType, SoldNights, Revenue)
    SELECT
        COALESCE(rt.TypeName, rt2.TypeName, 'Unknown') AS RoomType,
        1 AS SoldNights,
        CAST(ISNULL(brn.RateAmount, 0) + ISNULL(brn.TaxAmount, 0) AS DECIMAL(18,2)) AS Revenue
    FROM dbo.BookingRoomNights brn
    INNER JOIN dbo.Bookings b ON b.Id = brn.BookingId
    LEFT JOIN dbo.Rooms r ON r.Id = brn.RoomId
    LEFT JOIN dbo.RoomTypes rt ON rt.Id = r.RoomTypeId
    LEFT JOIN dbo.RoomTypes rt2 ON rt2.Id = b.RoomTypeId
    WHERE b.BranchID = @BranchID
      AND brn.StayDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled';

    -- A) Summary
    SELECT
        ISNULL(SUM(SoldNights), 0) AS SoldNights,
        ISNULL(SUM(Revenue), 0) AS Revenue
    FROM #X;

    -- B) Room type rows
    SELECT
        RoomType,
        COUNT(1) AS SoldNights,
        ISNULL(SUM(Revenue), 0) AS Revenue,
        CAST(CASE WHEN COUNT(1) = 0 THEN 0 ELSE ROUND(ISNULL(SUM(Revenue), 0) / COUNT(1), 2) END AS DECIMAL(18,2)) AS AvgNightRevenue
    FROM #X
    GROUP BY RoomType
    ORDER BY Revenue DESC, RoomType;

    DROP TABLE #X;

END;
GO

PRINT 'Stored Procedure sp_GetRoomTypePerformanceReport created successfully';
GO


/* ============================================================
   3) Outstanding Balance Report
   Result sets:
     A) Summary
     B) Details
============================================================ */
IF OBJECT_ID('sp_GetOutstandingBalanceReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetOutstandingBalanceReport;
GO

CREATE PROCEDURE sp_GetOutstandingBalanceReport
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
        Id INT NOT NULL,
        BookingNumber NVARCHAR(25) NULL,
        GuestName NVARCHAR(205) NULL,
        GuestPhone NVARCHAR(50) NULL,
        CheckInDate DATE NOT NULL,
        CheckOutDate DATE NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        DepositAmount DECIMAL(18,2) NOT NULL,
        BalanceAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(50) NULL,
        PaymentStatus NVARCHAR(50) NULL
    );

    INSERT INTO #B (
        Id,
        BookingNumber,
        GuestName,
        GuestPhone,
        CheckInDate,
        CheckOutDate,
        TotalAmount,
        DepositAmount,
        BalanceAmount,
        Status,
        PaymentStatus
    )
    SELECT
        b.Id,
        b.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
        b.PrimaryGuestPhone AS GuestPhone,
        b.CheckInDate,
        b.CheckOutDate,
        b.TotalAmount,
        b.DepositAmount,
        b.BalanceAmount,
        b.Status,
        b.PaymentStatus
    FROM dbo.Bookings b
    WHERE b.BranchID = @BranchID
      AND b.CheckInDate BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status, '') <> 'Cancelled'
      AND ISNULL(b.BalanceAmount, 0) > 0;

    -- A) Summary
    SELECT
        COUNT(1) AS TotalBookings,
        ISNULL(SUM(BalanceAmount), 0) AS TotalBalance
    FROM #B;

    -- B) Details
    SELECT
        BookingNumber,
        GuestName,
        GuestPhone,
        CheckInDate,
        CheckOutDate,
        TotalAmount,
        DepositAmount,
        BalanceAmount,
        Status,
        PaymentStatus
    FROM #B
    ORDER BY BalanceAmount DESC, CheckInDate DESC, BookingNumber DESC;

    DROP TABLE #B;

END;
GO

PRINT 'Stored Procedure sp_GetOutstandingBalanceReport created successfully';
GO
