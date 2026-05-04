using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class BookingDetailsReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string? SelectedBookingType { get; set; }   // null=All / "B2C" / "B2B"
    public string? SelectedStatus { get; set; }         // null=All / "Confirmed" / "CheckedIn" ...

    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string? GSTCode { get; set; }

    public BookingDetailsReportSummary Summary { get; set; } = new();
    public List<BookingDetailsHeaderRow> Bookings { get; set; } = new();

    // Keyed by BookingId for quick lookup in the view
    public Dictionary<int, List<BookingDetailsLineRow>> Lines { get; set; } = new();
}
