using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IReportsRepository _reportsRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;

    public ReportsController(IReportsRepository reportsRepository, IHotelSettingsRepository hotelSettingsRepository)
    {
        _reportsRepository = reportsRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
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

    [HttpGet]
    public async Task<IActionResult> DailyCollectionRegister(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetDailyCollectionRegisterAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new DailyCollectionRegisterReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            DailyTotals = data.DailyTotals,
            Rows = data.Rows
        };

        ViewData["Title"] = "Daily Collection Register";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GstReport(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var hotel = await _hotelSettingsRepository.GetByBranchAsync(branchId);
        var data = await _reportsRepository.GetGstReportAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new GstReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            HotelName = hotel?.HotelName ?? string.Empty,
            HotelAddress = hotel?.Address ?? string.Empty,
            GSTCode = hotel?.GSTCode,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "GST Report";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BusinessAnalyticsDashboard(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetBusinessAnalyticsDashboardAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new BusinessAnalyticsDashboardViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Daily = data.Daily,
            PaymentMethods = data.PaymentMethods
        };

        ViewData["Title"] = "Business Analytics Dashboard";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> RoomTypePerformance(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetRoomTypePerformanceReportAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new RoomTypePerformanceReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "Room Type Performance";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> OutstandingBalances(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetOutstandingBalanceReportAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new OutstandingBalanceReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "Outstanding Balances";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ChannelSourcePerformance(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetChannelSourcePerformanceReportAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new ChannelSourcePerformanceReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "Channel & Source Performance";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GuestDetails(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetGuestDetailsReportAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new GuestDetailsReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "Guest Details";
        return View(vm);
    }
}
