namespace HotelApp.Web.Models
{
    public class RateMaster
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal ExtraPaxRate { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal CGSTPercentage { get; set; }
        public decimal SGSTPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsWeekdayRate { get; set; } = true;
        public string? ApplyDiscount { get; set; }
        public bool IsDynamicRate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        
        // Navigation property
        public RoomType? RoomType { get; set; }
    }
}
