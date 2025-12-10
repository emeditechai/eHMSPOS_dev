namespace HotelApp.Web.Models
{
    public class SpecialDayRate
    {
        public int Id { get; set; }
        public int RateMasterId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EventName { get; set; }
        public decimal BaseRate { get; set; }
        public decimal ExtraPaxRate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
        
        // Navigation property
        public RateMaster? RateMaster { get; set; }
    }
}
