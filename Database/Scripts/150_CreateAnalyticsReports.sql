-- ============================================================
-- 150_CreateAnalyticsReports.sql
-- Creates:
--   sp_GetOccupancyRevenueAnalytics  (charts data)
--   sp_GetDueAmountAlerts            (outstanding due bookings)
-- Seeds nav menu items under Reports for:
--   Occupancy & Revenue, Monthly Report, Due Alerts
-- ============================================================

-- ============================================================
-- SP 1: Occupancy & Revenue Analytics
-- ============================================================
IF OBJECT_ID('dbo.sp_GetOccupancyRevenueAnalytics', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOccupancyRevenueAnalytics;
GO

CREATE PROCEDURE dbo.sp_GetOccupancyRevenueAnalytics
    @BranchID   INT,
    @FromDate   DATE,
    @ToDate     DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Total rooms in branch (for occupancy %)
    DECLARE @TotalRooms INT;
    SELECT @TotalRooms = COUNT(*) FROM Rooms WHERE BranchID = @BranchID AND IsActive = 1;
    IF @TotalRooms = 0 SET @TotalRooms = 1; -- avoid divide by zero

    -- ── RS1: Summary KPIs ──────────────────────────────────
    SELECT
        @TotalRooms                                                         AS TotalRooms,
        COUNT(DISTINCT CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN b.Id END)                                                AS TotalBookings,
        ISNULL(SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN b.TotalAmount END), 0)                                  AS TotalRevenue,
        ISNULL(SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN DATEDIFF(DAY,
                    CASE WHEN b.CheckInDate  < @FromDate THEN @FromDate ELSE b.CheckInDate  END,
                    CASE WHEN b.CheckOutDate > @ToDate   THEN @ToDate   ELSE b.CheckOutDate END)
              END), 0)                                                      AS TotalOccupiedNights,
        COUNT(DISTINCT CASE WHEN b.Status = 'Cancelled' THEN b.Id END)    AS CancelledBookings,
        -- Average Daily Rate (revenue / occupied nights)
        CASE WHEN ISNULL(SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN DATEDIFF(DAY,
                    CASE WHEN b.CheckInDate  < @FromDate THEN @FromDate ELSE b.CheckInDate  END,
                    CASE WHEN b.CheckOutDate > @ToDate   THEN @ToDate   ELSE b.CheckOutDate END)
              END), 0) = 0 THEN 0
             ELSE ISNULL(SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
                  THEN b.TotalAmount END), 0)
                  / SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
                    THEN DATEDIFF(DAY,
                        CASE WHEN b.CheckInDate  < @FromDate THEN @FromDate ELSE b.CheckInDate  END,
                        CASE WHEN b.CheckOutDate > @ToDate   THEN @ToDate   ELSE b.CheckOutDate END)
                    END)
        END                                                                 AS AverageDailyRate,
        DATEDIFF(DAY, @FromDate, @ToDate) + 1                             AS TotalDays
    FROM Bookings b
    WHERE b.BranchID = @BranchID
      AND b.CheckInDate  <= @ToDate
      AND b.CheckOutDate >= @FromDate;

    -- ── RS2: Daily Occupancy & Revenue ─────────────────────
    -- Generate a date series, then join bookings
    WITH DateSeries AS (
        SELECT CAST(@FromDate AS DATE) AS [Date]
        UNION ALL
        SELECT DATEADD(DAY, 1, [Date]) FROM DateSeries WHERE [Date] < @ToDate
    )
    SELECT
        d.[Date],
        DATENAME(WEEKDAY, d.[Date])                                         AS DayName,
        COUNT(DISTINCT CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN b.Id END)                                                AS BookingsCount,
        ISNULL(SUM(CASE WHEN b.Status NOT IN ('Cancelled','Pending')
              THEN b.TotalAmount END), 0)                                  AS Revenue,
        -- Occupancy %: rooms occupied that night / total rooms * 100
        CAST(
            CAST(COUNT(DISTINCT CASE WHEN b.Status NOT IN ('Cancelled','Pending')
                 THEN b.RoomId END) AS FLOAT)
            / @TotalRooms * 100
        AS DECIMAL(5,1))                                                    AS OccupancyPct
    FROM DateSeries d
    LEFT JOIN Bookings b ON b.BranchID = @BranchID
        AND b.CheckInDate  <= d.[Date]
        AND b.CheckOutDate >  d.[Date]
    GROUP BY d.[Date]
    ORDER BY d.[Date]
    OPTION (MAXRECURSION 400);

    -- ── RS3: Room Type Revenue Breakdown ───────────────────
    SELECT
        ISNULL(rt.TypeName, 'Unknown')                                       AS RoomType,
        COUNT(DISTINCT b.Id)                                                AS Bookings,
        ISNULL(SUM(b.TotalAmount), 0)                                       AS Revenue,
        CAST(
            CAST(ISNULL(SUM(b.TotalAmount), 0) AS FLOAT)
            / NULLIF((SELECT SUM(TotalAmount) FROM Bookings
                      WHERE BranchID = @BranchID
                        AND Status NOT IN ('Cancelled','Pending')
                        AND CheckInDate  <= @ToDate
                        AND CheckOutDate >= @FromDate), 0) * 100
        AS DECIMAL(5,1))                                                    AS RevenuePct
    FROM Bookings b
    LEFT JOIN Rooms r   ON r.Id = b.RoomId
    LEFT JOIN RoomTypes rt ON rt.Id = r.RoomTypeId
    WHERE b.BranchID = @BranchID
      AND b.Status NOT IN ('Cancelled','Pending')
      AND b.CheckInDate  <= @ToDate
      AND b.CheckOutDate >= @FromDate
    GROUP BY rt.TypeName
    ORDER BY Revenue DESC;

END
GO

PRINT '150: sp_GetOccupancyRevenueAnalytics created.';
GO

-- ============================================================
-- SP 2: Due Amount Alerts
-- ============================================================
IF OBJECT_ID('dbo.sp_GetDueAmountAlerts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDueAmountAlerts;
GO

CREATE PROCEDURE dbo.sp_GetDueAmountAlerts
    @BranchID   INT,
    @MinDue     DECIMAL(18,2) = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1: Summary ───────────────────────────────────────
    SELECT
        COUNT(*)                        AS TotalBookingsWithDue,
        ISNULL(SUM(DueAmount), 0)       AS TotalDueAmount
    FROM (
        SELECT
            GREATEST(0,
                ISNULL(b.TotalAmount, 0)
                + ISNULL((SELECT SUM(ISNULL(boc.Rate,0) * ISNULL(boc.Qty,1) + ISNULL(boc.GSTAmount,0)) FROM BookingOtherCharges boc WHERE boc.BookingId = b.Id), 0)
                - ISNULL((SELECT SUM(bp.Amount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
                - ISNULL((SELECT SUM(bp.DiscountAmount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
                - ISNULL((SELECT SUM(bp.RoundOffAmount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
            ) AS DueAmount
        FROM Bookings b
        WHERE b.BranchID = @BranchID
          AND b.Status NOT IN ('Cancelled')
    ) x WHERE DueAmount >= @MinDue;

    -- ── RS2: Detail rows ───────────────────────────────────
    ;WITH DueCalc AS (
        SELECT
            b.Id,
            b.BookingNumber,
            ISNULL(bg.FullName,
                ISNULL(g.FirstName + ' ' + g.LastName, 'Guest'))    AS GuestName,
            ISNULL(bg.Phone, g.Phone)                               AS GuestPhone,
            ISNULL(bg.Email, g.Email)                               AS GuestEmail,
            r.RoomNumber,
            b.CheckInDate,
            b.CheckOutDate,
            b.Status,
            ISNULL(b.TotalAmount, 0)                                AS TotalBill,
            ISNULL((SELECT SUM(bp.Amount)
                    FROM BookingPayments bp
                    WHERE bp.BookingId = b.Id
                      AND bp.Status IN ('Completed','Captured','Success')
                      AND ISNULL(bp.IsRefund,0) = 0), 0)            AS TotalPaid,
            ISNULL((SELECT SUM(bp.DiscountAmount)
                    FROM BookingPayments bp
                    WHERE bp.BookingId = b.Id
                      AND bp.Status IN ('Completed','Captured','Success')
                      AND ISNULL(bp.IsRefund,0) = 0), 0)            AS TotalDiscount,
            GREATEST(0,
                ISNULL(b.TotalAmount, 0)
                + ISNULL((SELECT SUM(ISNULL(boc.Rate,0) * ISNULL(boc.Qty,1) + ISNULL(boc.GSTAmount,0)) FROM BookingOtherCharges boc WHERE boc.BookingId = b.Id), 0)
                - ISNULL((SELECT SUM(bp.Amount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
                - ISNULL((SELECT SUM(bp.DiscountAmount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
                - ISNULL((SELECT SUM(bp.RoundOffAmount)
                          FROM BookingPayments bp
                          WHERE bp.BookingId = b.Id
                            AND bp.Status IN ('Completed','Captured','Success')
                            AND ISNULL(bp.IsRefund,0) = 0), 0)
            )                                                       AS DueAmount,
            DATEDIFF(DAY, b.CheckOutDate, CAST(GETDATE() AS DATE)) AS DaysOverdue
        FROM Bookings b
        LEFT JOIN BookingGuests bg ON bg.BookingId = b.Id AND bg.IsActive = 1 AND bg.IsPrimary = 1
        LEFT JOIN Guests g  ON g.Id = bg.GuestId
        LEFT JOIN Rooms r   ON r.Id = b.RoomId
        WHERE b.BranchID = @BranchID
          AND b.Status NOT IN ('Cancelled')
    )
    SELECT
        Id          AS BookingId,
        BookingNumber, GuestName, GuestPhone, GuestEmail,
        RoomNumber, CheckInDate, CheckOutDate, Status,
        TotalBill, TotalPaid, TotalDiscount, DueAmount, DaysOverdue
    FROM DueCalc
    WHERE DueAmount >= @MinDue
    ORDER BY DueAmount DESC;

END
GO

PRINT '150: sp_GetDueAmountAlerts created.';
GO

-- ============================================================
-- Nav Menu Items (under Reports parent)
-- ============================================================

-- 1. Occupancy & Revenue Charts
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_OCCUPANCY_REVENUE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_OCCUPANCY_REVENUE', 'Occupancy & Revenue', 'fas fa-chart-bar', 'Reports', 'OccupancyRevenue',
            (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 30, 1);
END

-- 2. Monthly Report
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_MONTHLY')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_MONTHLY', 'Monthly Report', 'fas fa-calendar-alt', 'Reports', 'MonthlyReport',
            (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 31, 1);
END

-- 3. Due Amount Alerts
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_DUE_ALERTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_DUE_ALERTS', 'Due Amount Alerts', 'fas fa-exclamation-circle', 'Reports', 'DueAlerts',
            (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 32, 1);
END
GO

-- ============================================================
-- RoleNavMenuItems — Administrator & Manager for all 3
-- ============================================================
DECLARE @Roles TABLE (RoleId INT);
INSERT INTO @Roles SELECT Id FROM Roles WHERE Name IN ('Administrator','Manager');

DECLARE @MenuCodes TABLE (Code NVARCHAR(100));
INSERT INTO @MenuCodes VALUES
    ('REPORTS_OCCUPANCY_REVENUE'),
    ('REPORTS_MONTHLY'),
    ('REPORTS_DUE_ALERTS');

INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.RoleId, m.Id, 1
FROM @Roles r
CROSS JOIN @MenuCodes mc
INNER JOIN NavMenuItems m ON m.Code = mc.Code
WHERE NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems x
    WHERE x.RoleId = r.RoleId AND x.NavMenuItemId = m.Id
);
GO

PRINT '150: Nav menu items seeded for analytics reports.';
GO
