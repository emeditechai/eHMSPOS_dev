using System.Data;
using Dapper;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnection _dbConnection;

    public NotificationRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<BrowserNotificationItem>> GetBranchNotificationsAsync(int branchId, DateTime today)
    {
        const int maxPerRule = 25;

        var notifications = new List<BrowserNotificationItem>();

        // 1) Today's checkout with pending amount
        const string checkoutPendingSql = @"
SELECT TOP (@MaxRows)
    b.Id,
    b.BookingNumber,
    (b.PrimaryGuestFirstName + ' ' + b.PrimaryGuestLastName) AS GuestName,
    b.BalanceAmount
FROM Bookings b
WHERE b.BranchID = @BranchID
  AND b.Status IN ('Confirmed','CheckedIn')
  AND b.ActualCheckOutDate IS NULL
  AND b.CheckOutDate = @Today
  AND b.BalanceAmount > 0
ORDER BY b.BalanceAmount DESC, b.Id DESC;";

        var checkoutPending = await _dbConnection.QueryAsync(checkoutPendingSql, new
        {
            MaxRows = maxPerRule,
            BranchID = branchId,
            Today = today.Date
        });

        foreach (var row in checkoutPending)
        {
            var bookingNumber = (string?)row.BookingNumber ?? string.Empty;
            var guestName = (string?)row.GuestName ?? string.Empty;
            var balance = row.BalanceAmount is decimal dec ? dec : Convert.ToDecimal(row.BalanceAmount ?? 0m);

            notifications.Add(new BrowserNotificationItem
            {
                Key = $"checkout-pending:{row.Id}",
                Kind = "checkout_pending",
                Title = $"Checkout Today: {bookingNumber} ({guestName})",
                Message = $"Booking #{bookingNumber} ({guestName}) has pending amount ₹{balance:N2}.",
                Url = $"/Booking/Details?bookingNumber={Uri.EscapeDataString(bookingNumber)}"
            });
        }

        // 2) Today's check-in: rooms not fully assigned yet
        var hasBookingRoomsTable = await _dbConnection.ExecuteScalarAsync<int?>(
            "SELECT OBJECT_ID('dbo.BookingRooms', 'U')");

        const string checkinUnassignedWithBookingRoomsSql = @"
SELECT TOP (@MaxRows)
    b.Id,
    b.BookingNumber,
    (b.PrimaryGuestFirstName + ' ' + b.PrimaryGuestLastName) AS GuestName,
    ISNULL(b.RequiredRooms, 1) AS RequiredRooms,
    ISNULL(br.AssignedRooms, 0) AS AssignedRooms
FROM Bookings b
OUTER APPLY (
    SELECT COUNT(1) AS AssignedRooms
    FROM BookingRooms br
    WHERE br.BookingId = b.Id
      AND br.IsActive = 1
) br
WHERE b.BranchID = @BranchID
  AND b.Status IN ('Confirmed','CheckedIn')
  AND b.CheckInDate = @Today
  AND ISNULL(br.AssignedRooms, 0) < ISNULL(b.RequiredRooms, 1)
ORDER BY b.Id DESC;";

        // Fallback for databases that haven't run the BookingRooms migration yet.
        // For multi-room bookings, this can only approximate using RoomId.
        const string checkinUnassignedFallbackSql = @"
SELECT TOP (@MaxRows)
    b.Id,
    b.BookingNumber,
    (b.PrimaryGuestFirstName + ' ' + b.PrimaryGuestLastName) AS GuestName,
    ISNULL(b.RequiredRooms, 1) AS RequiredRooms,
    CASE WHEN b.RoomId IS NULL THEN 0 ELSE 1 END AS AssignedRooms
FROM Bookings b
WHERE b.BranchID = @BranchID
  AND b.Status IN ('Confirmed','CheckedIn')
  AND b.CheckInDate = @Today
  AND (CASE WHEN b.RoomId IS NULL THEN 0 ELSE 1 END) < ISNULL(b.RequiredRooms, 1)
ORDER BY b.Id DESC;";

        var checkinUnassigned = await _dbConnection.QueryAsync(
            hasBookingRoomsTable.HasValue ? checkinUnassignedWithBookingRoomsSql : checkinUnassignedFallbackSql,
            new
        {
            MaxRows = maxPerRule,
            BranchID = branchId,
            Today = today.Date
        });

        foreach (var row in checkinUnassigned)
        {
            var bookingNumber = (string?)row.BookingNumber ?? string.Empty;
            var guestName = (string?)row.GuestName ?? string.Empty;
            var requiredRooms = row.RequiredRooms is int rr ? rr : Convert.ToInt32(row.RequiredRooms ?? 1);
            var assignedRooms = row.AssignedRooms is int ar ? ar : Convert.ToInt32(row.AssignedRooms ?? 0);

            notifications.Add(new BrowserNotificationItem
            {
                Key = $"checkin-unassigned:{row.Id}",
                Kind = "checkin_unassigned",
                Title = $"Check-in Today: {bookingNumber} ({guestName})",
                Message = $"Booking #{bookingNumber} ({guestName}) has {assignedRooms}/{requiredRooms} room(s) assigned.",
                Url = $"/Booking/AssignRoom?bookingNumber={Uri.EscapeDataString(bookingNumber)}"
            });
        }

        return notifications;
    }
}
