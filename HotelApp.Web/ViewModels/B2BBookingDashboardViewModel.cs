using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels
{
    public class B2BBookingDashboardViewModel
    {
        public int TodayBookingCount { get; set; }
        public decimal TodayAdvanceAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public int ActiveClientCount { get; set; }
        public int ActiveAgreementCount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StatusFilter { get; set; }
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();
    }
}