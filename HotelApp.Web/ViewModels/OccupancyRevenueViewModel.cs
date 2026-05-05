using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class OccupancyRevenueViewModel
{
    public DateOnly FromDate { get; set; } = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public OccupancyRevenueSummary Summary { get; set; } = new();
    public List<OccupancyRevenueDailyRow> Daily { get; set; } = new();
    public List<RoomTypeRevenueRow> RoomTypes { get; set; } = new();
}

public sealed class MonthlyReportViewModel
{
    public int Month { get; set; } = DateTime.Today.Month;
    public int Year { get; set; } = DateTime.Today.Year;

    public OccupancyRevenueSummary Summary { get; set; } = new();
    public List<OccupancyRevenueDailyRow> Daily { get; set; } = new();
    public List<RoomTypeRevenueRow> RoomTypes { get; set; } = new();

    public DateOnly FromDate => new(Year, Month, 1);
    public DateOnly ToDate => new(Year, Month, DateTime.DaysInMonth(Year, Month));
    public string MonthName => FromDate.ToString("MMMM yyyy");
}

public sealed class DueAlertsViewModel
{
    public DueAlertsSummary Summary { get; set; } = new();
    public List<DueAlertRow> Rows { get; set; } = new();
}
