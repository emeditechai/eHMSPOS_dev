using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class PoliceGuestRegisterViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly FromTime { get; set; } = TimeOnly.MinValue;           // 00:00
    public TimeOnly ToTime { get; set; } = new TimeOnly(23, 59);          // 23:59
    public string? RoomNumber { get; set; }
    public string? Nationality { get; set; }
    public string? GuestName { get; set; }

    // Hotel header info
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string PoliceStation { get; set; } = string.Empty;
    public string? LogoPath { get; set; }

    public PoliceGuestRegisterSummary Summary { get; set; } = new();
    public List<PoliceGuestRegisterRow> Rows { get; set; } = new();
}
