namespace HotelApp.Web.Models
{
    public class BookingQuoteRequest
    {
        public int RoomTypeId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int Adults { get; set; }
        public int Children { get; set; }
        public int BranchID { get; set; }
    }

    public class BookingQuoteResult
    {
        public int Nights { get; set; }
        public int? RatePlanId { get; set; }
        public decimal BaseRatePerNight { get; set; }
        public decimal ExtraPaxRatePerNight { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal CGSTPercentage { get; set; }
        public decimal SGSTPercentage { get; set; }
        public decimal TotalRoomRate { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalCGSTAmount { get; set; }
        public decimal TotalSGSTAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class BookingCreationResult
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
    }
}
