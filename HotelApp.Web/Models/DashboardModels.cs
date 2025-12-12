namespace HotelApp.Web.Models
{
    public class DashboardStatistics
    {
        public int TotalGuests { get; set; }
        public decimal GuestsChangePercent { get; set; }
        public decimal OccupancyRate { get; set; }
        public decimal OccupancyChangePercent { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int CheckInsToday { get; set; }
        public int CheckInsChange { get; set; }
    }

    public class RevenueData
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RoomTypeDistribution
    {
        public string TypeName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
    }

    public class RecentBooking
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int RequiredRooms { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
