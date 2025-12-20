using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class DailyCollectionRegisterReportViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DailyCollectionRegisterSummaryRow Summary { get; set; } = new();
    public List<DailyCollectionRegisterDailyRow> DailyTotals { get; set; } = new();
    public List<DailyCollectionRegisterDetailRow> Rows { get; set; } = new();
}
