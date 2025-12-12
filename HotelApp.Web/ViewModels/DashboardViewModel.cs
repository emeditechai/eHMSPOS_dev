namespace HotelApp.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalGuests { get; set; }
    public decimal GuestsPercentageChange { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal OccupancyPercentageChange { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenuePercentageChange { get; set; }
    public int CheckInsToday { get; set; }
    public int CheckInsTodayChange { get; set; }
    public List<DailyRevenue> RevenueData { get; set; } = new();
    public List<RoomTypeStats> RoomTypeStats { get; set; } = new();
    public List<RecentBookingItem> RecentBookings { get; set; } = new();
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public class RoomTypeStats
{
    public string TypeName { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
}

public class RecentBookingItem
{
    public string BookingNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
