using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels
{
    public class RoomDashboardViewModel
    {
        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int CleaningCount { get; set; }
        public int AvailableChange { get; set; }
        public int OccupiedChange { get; set; }
        public int MaintenanceChange { get; set; }
        public int CleaningChange { get; set; }
        public List<RoomDashboardItem> Rooms { get; set; } = new List<RoomDashboardItem>();
        public List<Floor> Floors { get; set; } = new List<Floor>();
    }

    public class RoomDashboardItem
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string? FloorName { get; set; }
        public decimal BaseRate { get; set; }
        public int MaxOccupancy { get; set; }
        public string? CurrentGuest { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? BookingNumber { get; set; }
        public string? PrimaryGuestName { get; set; }
    }
}
