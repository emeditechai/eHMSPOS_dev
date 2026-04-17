-- Migration Script: 135_UpdateRecentBookingsSPShowActiveBookings.sql
-- Description: Update sp_GetRecentBookings to show active bookings (CheckedIn + Confirmed),
--              plus recent completed checkouts (last 7 days), ordered by relevance.
-- Date: 2026-04-17

USE HMS_dev;
GO

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
        LTRIM(RTRIM(b.PrimaryGuestFirstName + ' ' + ISNULL(b.PrimaryGuestLastName, ''))) AS GuestName,
        rt.TypeName AS RoomType,
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
      AND (
          -- Active bookings (currently checked-in or confirmed for near future)
          b.Status IN ('CheckedIn', 'Confirmed', 'Pending')
          OR
          -- Recent checkouts in the last 7 days
          (b.Status = 'CheckedOut' AND CAST(b.CheckOutDate AS DATE) >= CAST(DATEADD(DAY, -7, GETDATE()) AS DATE))
      )
    ORDER BY
        CASE b.Status
            WHEN 'CheckedIn'  THEN 1
            WHEN 'Confirmed'  THEN 2
            WHEN 'Pending'    THEN 3
            WHEN 'CheckedOut' THEN 4
            ELSE 5
        END,
        b.CheckInDate ASC;
END;
GO

PRINT 'sp_GetRecentBookings updated to show active + recent bookings';
GO
