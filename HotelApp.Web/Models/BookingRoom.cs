namespace HotelApp.Web.Models
{
    public class BookingRoom
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomId { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? UnassignedDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation properties
        public Booking? Booking { get; set; }
        public Room? Room { get; set; }
    }
}
