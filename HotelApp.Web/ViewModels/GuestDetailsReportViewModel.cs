using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class GuestDetailsReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public GuestDetailsReportSummaryRow Summary { get; set; } = new();
    public List<GuestDetailsReportRow> Rows { get; set; } = new();
}
