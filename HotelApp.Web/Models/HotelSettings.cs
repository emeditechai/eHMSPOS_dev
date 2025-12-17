namespace HotelApp.Web.Models
{
    public class HotelSettings
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber1 { get; set; } = string.Empty;
        public string? ContactNumber2 { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? GSTCode { get; set; }
        public string? LogoPath { get; set; }
        public TimeSpan CheckInTime { get; set; } = new TimeSpan(14, 0, 0); // 2:00 PM
        public TimeSpan CheckOutTime { get; set; } = new TimeSpan(12, 0, 0); // 12:00 PM
        public bool ByPassActualDayRate { get; set; } = false;
        public bool DiscountApprovalRequired { get; set; } = false;
        public bool MinimumBookingAmountRequired { get; set; } = false;
        public decimal? MinimumBookingAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public int? LastModifiedBy { get; set; }
    }
}
