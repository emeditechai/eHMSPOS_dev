namespace HotelApp.Web.Models
{
    public class BookingAuditLog
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public int BranchID { get; set; }
        public int? PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; }
    }
}
