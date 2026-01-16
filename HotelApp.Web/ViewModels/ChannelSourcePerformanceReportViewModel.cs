using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class ChannelSourcePerformanceReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public ChannelSourcePerformanceSummaryRow Summary { get; set; } = new();
    public List<ChannelSourcePerformanceRow> Rows { get; set; } = new();
}
