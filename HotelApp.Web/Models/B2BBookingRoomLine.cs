namespace HotelApp.Web.Models
{
    public class B2BBookingRoomLine
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public int RequiredRooms { get; set; } = 1;
        public decimal RatePerNight { get; set; }
        public int Nights { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; }
        public string? MealPlan { get; set; }
        public int ExtraPaxCount { get; set; }
        public decimal ExtraPaxRatePerNight { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancelledDate { get; set; }
        public int? CancelledBy { get; set; }
    }
}
