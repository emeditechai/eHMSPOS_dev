using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class CancellationRegisterReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public CancellationRegisterSummaryRow Summary { get; set; } = new();
    public List<CancellationRegisterDetailRow> Rows { get; set; } = new();
}
