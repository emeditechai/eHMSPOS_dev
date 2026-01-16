using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class OutstandingBalanceReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public OutstandingBalanceSummaryRow Summary { get; set; } = new();
    public List<OutstandingBalanceRow> Rows { get; set; } = new();
}
