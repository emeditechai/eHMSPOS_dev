-- Migration Script: 48_UpdateRecentBookingsToTodaysCheckouts.sql
-- Description: Update sp_GetRecentBookings to show only bookings with today's checkout date
-- Date: 2025-12-18

USE HMS_dev;
GO

-- Drop and recreate the stored procedure
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
        AND CAST(b.CheckOutDate AS DATE) = CAST(GETDATE() AS DATE)
    ORDER BY b.CheckOutDate ASC, b.CreatedDate DESC;
END;
GO

PRINT 'sp_GetRecentBookings updated to filter by today''s checkout date';
GO
