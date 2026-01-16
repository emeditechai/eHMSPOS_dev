using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class RoomTypePerformanceReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public RoomTypePerformanceSummaryRow Summary { get; set; } = new();
    public List<RoomTypePerformanceRow> Rows { get; set; } = new();
}
