using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IReportsRepository _reportsRepository;

    public ReportsController(IReportsRepository reportsRepository)
    {
        _reportsRepository = reportsRepository;
    }

    [HttpGet]
    public async Task<IActionResult> RoomPriceDetails(
        DateOnly? asOfDate,
        int? roomTypeId,
        string? roomStatus,
        int? floorId
    )
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveAsOfDate = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        var rows = (await _reportsRepository.GetRoomPriceDetailsAsync(
            branchId,
            effectiveAsOfDate,
            roomTypeId,
            roomStatus,
            floorId
        )).ToList();

        var currentRates = rows
            .Select(r => r.CurrentBaseRate)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        var vm = new RoomPriceDetailsReportViewModel
        {
            AsOfDate = effectiveAsOfDate,
            SelectedRoomTypeId = roomTypeId,
            SelectedRoomStatus = roomStatus,
            SelectedFloorId = floorId,

            RoomTypes = rows
                .Where(r => r.RoomTypeId > 0 && !string.IsNullOrWhiteSpace(r.RoomType))
                .GroupBy(r => new { r.RoomTypeId, Name = r.RoomType!.Trim() })
                .Select(g => new RoomTypeFilterOption { Id = g.Key.RoomTypeId, Name = g.Key.Name })
                .OrderBy(x => x.Name)
                .ToList(),
            Floors = rows
                .Where(r => r.Floor.HasValue)
                .GroupBy(r => new { Id = r.Floor!.Value, Name = (r.FloorName ?? r.Floor!.Value.ToString()).Trim() })
                .Select(g => new FloorFilterOption { Id = g.Key.Id, Name = g.Key.Name })
                .OrderBy(x => x.Id)
                .ToList(),
            RoomStatuses = rows
                .Select(r => r.RoomStatus)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList(),

            TotalRooms = rows.Count,
            AvailableRooms = rows.Count(r => string.Equals(r.RoomStatus, "Available", StringComparison.OrdinalIgnoreCase)),
            OccupiedRooms = rows.Count(r => string.Equals(r.RoomStatus, "Occupied", StringComparison.OrdinalIgnoreCase)),
            MaintenanceRooms = rows.Count(r => string.Equals(r.RoomStatus, "Maintenance", StringComparison.OrdinalIgnoreCase)),
            AvgCurrentBaseRate = currentRates.Count == 0 ? 0 : Math.Round(currentRates.Average(), 2),
            MinCurrentBaseRate = currentRates.Count == 0 ? 0 : currentRates.Min(),
            MaxCurrentBaseRate = currentRates.Count == 0 ? 0 : currentRates.Max(),
            Rows = rows
        };

        ViewData["Title"] = "Room Price Details";
        return View(vm);
    }
}
