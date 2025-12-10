namespace HotelApp.Web.ViewModels
{
    public class RateMasterViewModel
    {
        // Base Rate Master Information
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public decimal ExtraPaxRate { get; set; }
        public decimal TaxPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsWeekdayRate { get; set; } = true;
        public string? ApplyDiscount { get; set; }
        public bool IsDynamicRate { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Audit Fields
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
        
        // Weekend Rates
        public bool HasWeekendRates { get; set; }
        public List<WeekendRateItem> WeekendRates { get; set; } = new();
        
        // Special Day Rates
        public bool HasSpecialDayRates { get; set; }
        public List<SpecialDayRateItem> SpecialDayRates { get; set; } = new();
        
        // Nested Classes
        public class WeekendRateItem
        {
            public int Id { get; set; }
            public string DayOfWeek { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public decimal BaseRate { get; set; }
            public decimal ExtraPaxRate { get; set; }
            public bool IsActive { get; set; } = true;
        }
        
        public class SpecialDayRateItem
        {
            public int Id { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }
            public string? EventName { get; set; }
            public decimal BaseRate { get; set; }
            public decimal ExtraPaxRate { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
}
