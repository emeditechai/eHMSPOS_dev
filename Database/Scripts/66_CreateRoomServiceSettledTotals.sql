-- Create stored procedure to return settled room service totals per order
-- Filters by OrderID; returns one row per BookingID/BranchID/OrderID/OrderNo

IF OBJECT_ID(N'HMS_dev.dbo.usp_GetRoomServiceSettledTotals', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE HMS_dev.dbo.usp_GetRoomServiceSettledTotals;
END
GO

CREATE PROCEDURE HMS_dev.dbo.usp_GetRoomServiceSettledTotals
    @BookingID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rs.BookingID,
        rs.BranchID,
        rs.OrderID,
        rs.OrderNo,
        MAX(b.BookingNumber) AS BookingNo,
        MAX(LTRIM(RTRIM(CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)))) AS PrimaryGuestName,
        MAX(b.PrimaryGuestPhone) AS GuestPhone,
        MAX(rs.SettleAmount) AS PaidAmount,
        MAX(rs.DiscountAmount) AS DiscountAmount,
        MAX(rs.CGSTAmount) AS CGSTAmount,
        MAX(rs.SGSTAmount) AS SGSTAmount,
        MAX(rs.GSTAmount) AS GSTAmount,
        MAX(CAST(rs.IsSettled AS INT)) AS IsSettled
    FROM HMS_dev.dbo.RoomServices rs
    INNER JOIN HMS_dev.dbo.Bookings b
        ON b.Id = rs.BookingID
       AND b.BranchID = rs.BranchID
    WHERE rs.IsSettled = 1
      AND (@BookingID IS NULL OR rs.BookingID = @BookingID)
    GROUP BY rs.BookingID, rs.BranchID, rs.OrderID, rs.OrderNo;
END
GO
