using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class GstReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string? GSTCode { get; set; }

    public GstReportSummaryRow Summary { get; set; } = new();
    public List<GstReportDetailRow> Rows { get; set; } = new();
}
