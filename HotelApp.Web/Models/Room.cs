namespace HotelApp.Web.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public int Floor { get; set; }
        public string? FloorName { get; set; }
        public string Status { get; set; } = "Available";
        public string? Notes { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        
        // Booking information for occupied rooms
        public DateTime? CheckOutDate { get; set; }
        public string? BookingNumber { get; set; }
        public decimal? BalanceAmount { get; set; }
        
        // Navigation property
        public RoomType? RoomType { get; set; }
    }
}
