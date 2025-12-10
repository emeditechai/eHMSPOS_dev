namespace HotelApp.Web.Models
{
    public class WeekendRate
    {
        public int Id { get; set; }
        public int RateMasterId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.
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
