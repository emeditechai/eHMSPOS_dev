using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels
{
    public class BookingDashboardViewModel
    {
        public int TodayBookingCount { get; set; }
        public decimal TodayAdvanceAmount { get; set; }
        public int TodayCheckInCount { get; set; }
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
