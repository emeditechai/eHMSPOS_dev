using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class CancellationRefundRegisterReportViewModel
{
    public DateOnly FromDate    { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate      { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string   ReportType  { get; set; } = "All";   // All | Cancelled | Refunded

    public CancellationRefundRegisterSummary Summary { get; set; } = new();
    public List<CancellationRefundRegisterRow> Rows   { get; set; } = new();
}
