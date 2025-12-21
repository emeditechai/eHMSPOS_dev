-- =============================================
-- Checked-in + Occupied Rooms Stored Procedure
-- Description: Returns current occupied rooms with checked-in guest contact details
-- Columns: BookingID, BookingNo, RoomID, RoomNo, GuestName, GuestPhone, GuestEmailID
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('dbo.sp_GetCheckedInOccupiedRooms', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCheckedInOccupiedRooms;
GO

CREATE PROCEDURE dbo.sp_GetCheckedInOccupiedRooms
    @BranchID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = CAST(GETDATE() AS DATETIME2);

    SELECT
        b.Id AS BookingID,
        b.BookingNumber AS BookingNo,
        r.Id AS RoomID,
        r.RoomNumber AS RoomNo,
        COALESCE(
            bgPick.FullName,
            NULLIF(LTRIM(RTRIM(CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName))), '')
        ) AS GuestName,
        COALESCE(bgPick.Phone, b.PrimaryGuestPhone) AS GuestPhone,
        COALESCE(bgPick.Email, b.PrimaryGuestEmail) AS GuestEmailID
    FROM [HMS_dev].[dbo].[Bookings] b
    OUTER APPLY (
        SELECT TOP (1)
            br.RoomId
        FROM [HMS_dev].[dbo].[BookingRooms] br
        WHERE br.BookingId = b.Id
          AND br.IsActive = 1
          AND br.AssignedDate <= @Now
          AND (br.UnassignedDate IS NULL OR br.UnassignedDate > @Now)
        ORDER BY br.AssignedDate DESC, br.Id DESC
    ) brPick
    INNER JOIN [HMS_dev].[dbo].[Rooms] r
        ON r.Id = COALESCE(brPick.RoomId, b.RoomId)
        AND r.IsActive = 1
        AND r.BranchID = b.BranchID
    OUTER APPLY (
        SELECT TOP (1)
            bg.FullName,
            bg.Phone,
            bg.Email
        FROM [HMS_dev].[dbo].[BookingGuests] bg
        WHERE bg.BookingId = b.Id
        ORDER BY CASE WHEN bg.IsPrimary = 1 THEN 0 ELSE 1 END, bg.Id
    ) bgPick
    WHERE ISNULL(b.Status, '') <> 'Cancelled'
      AND COALESCE(brPick.RoomId, b.RoomId) IS NOT NULL
      AND b.ActualCheckInDate IS NOT NULL
      AND CAST(b.ActualCheckInDate AS DATETIME2) <= @Now
      AND (b.ActualCheckOutDate IS NULL OR CAST(b.ActualCheckOutDate AS DATETIME2) > @Now)
      AND (@BranchID IS NULL OR b.BranchID = @BranchID)
    ORDER BY r.RoomNumber, b.BookingNumber;
END;
GO

PRINT 'Stored Procedure dbo.sp_GetCheckedInOccupiedRooms created successfully';
GO
