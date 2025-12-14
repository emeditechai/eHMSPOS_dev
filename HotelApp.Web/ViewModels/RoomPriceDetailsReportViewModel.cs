using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class RoomPriceDetailsReportViewModel
{
    public DateOnly AsOfDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int? SelectedRoomTypeId { get; set; }
    public string? SelectedRoomStatus { get; set; }
    public int? SelectedFloorId { get; set; }

    public List<RoomTypeFilterOption> RoomTypes { get; set; } = new();
    public List<FloorFilterOption> Floors { get; set; } = new();
    public List<string> RoomStatuses { get; set; } = new();

    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int MaintenanceRooms { get; set; }

    public decimal AvgCurrentBaseRate { get; set; }
    public decimal MinCurrentBaseRate { get; set; }
    public decimal MaxCurrentBaseRate { get; set; }

    public List<RoomPriceDetailRow> Rows { get; set; } = new();
}

public sealed class RoomTypeFilterOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class FloorFilterOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
