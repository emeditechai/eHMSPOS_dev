-- Migration: Create Stored Procedures for Dashboard
-- Description: Creates stored procedures to fetch dashboard statistics, revenue, room types, and recent bookings
-- Date: 2025-12-12

USE HMS_dev;
GO

-- =============================================
-- Stored Procedure: Get Dashboard Statistics
-- Description: Returns key metrics for the dashboard
-- =============================================
IF OBJECT_ID('sp_GetDashboardStatistics', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetDashboardStatistics;
GO

CREATE PROCEDURE sp_GetDashboardStatistics
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
    DECLARE @StartOfLastMonth DATE = DATEADD(MONTH, -1, @StartOfMonth);
    DECLARE @EndOfLastMonth DATE = DATEADD(DAY, -1, @StartOfMonth);
    DECLARE @StartOfWeek DATE = DATEADD(DAY, -(DATEPART(WEEKDAY, @Today) - 1), @Today);
    DECLARE @StartOfLastWeek DATE = DATEADD(WEEK, -1, @StartOfWeek);
    DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);
    
    -- Total Guests (This Month)
    DECLARE @TotalGuests INT;
    SELECT @TotalGuests = COUNT(DISTINCT bg.Id)
    FROM BookingGuests bg
    INNER JOIN Bookings b ON bg.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(b.CreatedDate AS DATE) >= @StartOfMonth
        AND b.Status NOT IN ('Cancelled', 'NoShow');
    
    -- Total Guests Last Month
    DECLARE @TotalGuestsLastMonth INT;
    SELECT @TotalGuestsLastMonth = COUNT(DISTINCT bg.Id)
    FROM BookingGuests bg
    INNER JOIN Bookings b ON bg.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(b.CreatedDate AS DATE) >= @StartOfLastMonth
        AND CAST(b.CreatedDate AS DATE) <= @EndOfLastMonth
        AND b.Status NOT IN ('Cancelled', 'NoShow');
    
    -- Occupancy Rate (Today)
    DECLARE @TotalRooms INT, @OccupiedRooms INT;
    SELECT @TotalRooms = SUM(Max_RoomAvailability)
    FROM RoomTypes
    WHERE BranchID = @BranchID AND IsActive = 1;
    
    SELECT @OccupiedRooms = SUM(b.RequiredRooms)
    FROM Bookings b
    WHERE b.BranchID = @BranchID
        AND b.Status IN ('Confirmed', 'CheckedIn')
        AND CAST(b.CheckInDate AS DATE) <= @Today
        AND (
            CASE 
                WHEN b.ActualCheckOutDate IS NOT NULL THEN CAST(b.ActualCheckOutDate AS DATE)
                ELSE CAST(b.CheckOutDate AS DATE)
            END > @Today
        );
    
    DECLARE @OccupancyRate DECIMAL(5,2) = CASE WHEN @TotalRooms > 0 THEN (@OccupiedRooms * 100.0 / @TotalRooms) ELSE 0 END;
    
    -- Occupancy Rate Last Week
    DECLARE @OccupiedRoomsLastWeek INT;
    SELECT @OccupiedRoomsLastWeek = AVG(CAST(OccupiedCount AS FLOAT))
    FROM (
        SELECT COUNT(*) as OccupiedCount, CAST(b.CheckInDate AS DATE) as StayDate
        FROM Bookings b
        WHERE b.BranchID = @BranchID
            AND b.Status IN ('Confirmed', 'CheckedIn')
            AND CAST(b.CheckInDate AS DATE) >= @StartOfLastWeek
            AND CAST(b.CheckInDate AS DATE) < @StartOfWeek
        GROUP BY CAST(b.CheckInDate AS DATE)
    ) LastWeekData;
    
    DECLARE @OccupancyRateLastWeek DECIMAL(5,2) = CASE WHEN @TotalRooms > 0 THEN (ISNULL(@OccupiedRoomsLastWeek, 0) * 100.0 / @TotalRooms) ELSE 0 END;
    
    -- Revenue (This Month) - Based on actual payments received
    DECLARE @Revenue DECIMAL(18,2);
    SELECT @Revenue = ISNULL(SUM(bp.Amount), 0)
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) >= @StartOfMonth
        AND bp.Status IN ('Completed', 'Captured', 'Success');
    
    -- Revenue Last Month - Based on actual payments received
    DECLARE @RevenueLastMonth DECIMAL(18,2);
    SELECT @RevenueLastMonth = ISNULL(SUM(bp.Amount), 0)
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) >= @StartOfLastMonth
        AND CAST(bp.PaidOn AS DATE) <= @EndOfLastMonth
        AND bp.Status IN ('Completed', 'Captured', 'Success');
    
    -- Check-ins Today
    DECLARE @CheckInsToday INT;
    SELECT @CheckInsToday = COUNT(*)
    FROM Bookings
    WHERE BranchID = @BranchID
        AND CAST(CheckInDate AS DATE) = @Today
        AND Status IN ('Confirmed', 'CheckedIn');
    
    -- Check-ins Yesterday
    DECLARE @CheckInsYesterday INT;
    SELECT @CheckInsYesterday = COUNT(*)
    FROM Bookings
    WHERE BranchID = @BranchID
        AND CAST(CheckInDate AS DATE) = @Yesterday
        AND Status IN ('Confirmed', 'CheckedIn');
    
    -- Return results
    SELECT 
        ISNULL(@TotalGuests, 0) as TotalGuests,
        CASE 
            WHEN @TotalGuestsLastMonth = 0 THEN 0
            ELSE ROUND(((@TotalGuests - @TotalGuestsLastMonth) * 100.0 / @TotalGuestsLastMonth), 2)
        END as GuestsChangePercent,
        ROUND(@OccupancyRate, 0) as OccupancyRate,
        ROUND(@OccupancyRate - @OccupancyRateLastWeek, 2) as OccupancyChangePercent,
        ISNULL(@Revenue, 0) as Revenue,
        CASE 
            WHEN @RevenueLastMonth = 0 THEN 0
            ELSE ROUND(((@Revenue - @RevenueLastMonth) * 100.0 / @RevenueLastMonth), 2)
        END as RevenueChangePercent,
        ISNULL(@CheckInsToday, 0) as CheckInsToday,
        ISNULL(@CheckInsToday, 0) - ISNULL(@CheckInsYesterday, 0) as CheckInsChange;
END;
GO

-- =============================================
-- Stored Procedure: Get Revenue Overview
-- Description: Returns daily revenue for the specified period
-- =============================================
IF OBJECT_ID('sp_GetRevenueOverview', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetRevenueOverview;
GO

CREATE PROCEDURE sp_GetRevenueOverview
    @BranchID INT,
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATE = DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS DATE));
    
    -- Return daily revenue based on actual payments received
    SELECT 
        CAST(bp.PaidOn AS DATE) as Date,
        SUM(bp.Amount) as Revenue
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) >= @StartDate
        AND bp.Status IN ('Completed', 'Captured', 'Success')
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY CAST(bp.PaidOn AS DATE);
END;
GO

-- =============================================
-- Stored Procedure: Get Room Type Distribution
-- Description: Returns booking count by room type
-- =============================================
IF OBJECT_ID('sp_GetRoomTypeDistribution', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetRoomTypeDistribution;
GO

CREATE PROCEDURE sp_GetRoomTypeDistribution
    @BranchID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartOfMonth DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
    
    SELECT 
        rt.TypeName,
        COUNT(b.Id) as BookingCount
    FROM RoomTypes rt
    LEFT JOIN Bookings b ON rt.Id = b.RoomTypeId 
        AND b.BranchID = @BranchID
        AND CAST(b.CreatedDate AS DATE) >= @StartOfMonth
        AND b.Status NOT IN ('Cancelled', 'NoShow')
    WHERE rt.BranchID = @BranchID
        AND rt.IsActive = 1
    GROUP BY rt.TypeName, rt.Id
    ORDER BY COUNT(b.Id) DESC;
END;
GO

-- =============================================
-- Stored Procedure: Get Recent Bookings
-- Description: Returns most recent bookings
-- =============================================
IF OBJECT_ID('sp_GetRecentBookings', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetRecentBookings;
GO

CREATE PROCEDURE sp_GetRecentBookings
    @BranchID INT,
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Top)
        b.Id,
        b.BookingNumber,
        b.PrimaryGuestFirstName + ' ' + ISNULL(b.PrimaryGuestLastName, '') as GuestName,
        rt.TypeName as RoomType,
        b.CheckInDate,
        b.CheckOutDate,
        b.Status,
        b.TotalAmount,
        b.BalanceAmount,
        b.RequiredRooms,
        b.CreatedDate
    FROM Bookings b
    INNER JOIN RoomTypes rt ON b.RoomTypeId = rt.Id
    WHERE b.BranchID = @BranchID
    ORDER BY b.CreatedDate DESC;
END;
GO

PRINT 'Dashboard stored procedures created successfully';
