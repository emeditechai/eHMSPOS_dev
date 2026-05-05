using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public sealed class ReportsController : BaseController
{
    private readonly IReportsRepository _reportsRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;
    private readonly IMailSender _mailSender;

    public ReportsController(IReportsRepository reportsRepository,
        IHotelSettingsRepository hotelSettingsRepository,
        IMailSender mailSender)
    {
        _reportsRepository = reportsRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
        _mailSender = mailSender;
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

        // Resolve role — session may be lost on hot-reload, fall back to claims.
        var roleName = HttpContext.Session.GetString("SelectedRoleName")
                    ?? User.FindFirst("SelectedRoleName")?.Value
                    ?? string.Empty;

        var isAdmin = roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase)
                   || roleName.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        var userId = HttpContext.Session.GetInt32("UserId")
                  ?? (int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0);

        var data = await _reportsRepository.GetDailyCollectionRegisterAsync(
            branchId, effectiveFrom, effectiveTo, userId, isAdmin);

        var vm = new DailyCollectionRegisterReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            DailyTotals = data.DailyTotals,
            Rows = data.Rows
        };

        ViewBag.IsAdmin = isAdmin;
        ViewBag.RoleName = roleName;
        ViewData["Title"] = "Daily Collection Register";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CancellationRegister(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo = toDate ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
        {
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
        }

        var data = await _reportsRepository.GetCancellationRegisterAsync(branchId, effectiveFrom, effectiveTo);

        var vm = new CancellationRegisterReportViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            Summary = data.Summary,
            Rows = data.Rows
        };

        ViewData["Title"] = "Cancellation Register";
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
        var branchId = CurrentBranchID;

        // Default: first day of current month → today
        var effectiveFrom = fromDate ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var effectiveTo   = toDate   ?? DateOnly.FromDateTime(DateTime.Today);

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

    [HttpGet]
    public async Task<IActionResult> CancellationRefundRegister(
        DateOnly? fromDate, DateOnly? toDate, string? reportType)
    {
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 1;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo   = toDate   ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);

        var type = reportType?.Trim() ?? "All";
        if (!new[] { "All", "Cancelled", "Refunded" }.Contains(type, StringComparer.OrdinalIgnoreCase))
            type = "All";

        var data = await _reportsRepository.GetCancellationRefundRegisterAsync(
            branchId, effectiveFrom, effectiveTo, type);

        var vm = new CancellationRefundRegisterReportViewModel
        {
            FromDate   = effectiveFrom,
            ToDate     = effectiveTo,
            ReportType = type,
            Summary    = data.Summary,
            Rows       = data.Rows
        };

        ViewData["Title"] = "Cancellation & Refund Register";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PoliceGuestRegister(
        DateOnly? fromDate, DateOnly? toDate,
        string? fromTime, string? toTime,
        string? roomNumber, string? nationality, string? guestName)
    {
        var branchId = CurrentBranchID;

        var effectiveFromDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveToDate   = toDate   ?? DateOnly.FromDateTime(DateTime.Today);

        if (effectiveToDate < effectiveFromDate)
            (effectiveFromDate, effectiveToDate) = (effectiveToDate, effectiveFromDate);

        // Parse time, default 00:00 – 23:59
        var effectiveFromTime = TimeOnly.TryParse(fromTime, out var ft) ? ft : TimeOnly.MinValue;
        var effectiveToTime   = TimeOnly.TryParse(toTime, out var tt) ? tt : new TimeOnly(23, 59);

        var fromDateTime = effectiveFromDate.ToDateTime(effectiveFromTime);
        var toDateTime   = effectiveToDate.ToDateTime(effectiveToTime);

        var data = await _reportsRepository.GetPoliceGuestRegisterAsync(
            branchId, fromDateTime, toDateTime, roomNumber, nationality, guestName);

        // Get hotel settings for report header
        var settings = await _hotelSettingsRepository.GetByBranchAsync(branchId);

        var vm = new PoliceGuestRegisterViewModel
        {
            FromDate     = effectiveFromDate,
            ToDate       = effectiveToDate,
            FromTime     = effectiveFromTime,
            ToTime       = effectiveToTime,
            RoomNumber   = roomNumber,
            Nationality  = nationality,
            GuestName    = guestName,
            HotelName    = settings?.HotelName ?? string.Empty,
            HotelAddress = settings?.Address ?? string.Empty,
            PoliceStation = settings?.PoliceStation ?? string.Empty,
            LogoPath     = settings?.LogoPath,
            Summary      = data.Summary,
            Rows         = data.Rows
        };

        ViewData["Title"] = "Police / Guest Register";
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BookingDetailsReport(
        DateOnly? fromDate, DateOnly? toDate,
        string? bookingType, string? status)
    {
        var branchId = CurrentBranchID;

        var effectiveFrom = fromDate ?? DateOnly.FromDateTime(DateTime.Today);
        var effectiveTo   = toDate   ?? effectiveFrom;

        if (effectiveTo < effectiveFrom)
            (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);

        // Sanitise optional filters
        var bType  = string.IsNullOrWhiteSpace(bookingType) ? null : bookingType.Trim();
        var bStatus = string.IsNullOrWhiteSpace(status)     ? null : status.Trim();

        var hotel = await _hotelSettingsRepository.GetByBranchAsync(branchId);
        var data  = await _reportsRepository.GetBookingDetailsReportAsync(
            branchId, effectiveFrom, effectiveTo, bType, bStatus);

        // Group billing-head lines by BookingId
        var linesDict = data.Lines
            .GroupBy(l => l.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Group drill-down (item-level) lines by BookingId
        var drillDict = data.DrillDownLines
            .GroupBy(l => l.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var vm = new BookingDetailsReportViewModel
        {
            FromDate            = effectiveFrom,
            ToDate              = effectiveTo,
            SelectedBookingType = bType,
            SelectedStatus      = bStatus,
            HotelName           = hotel?.HotelName    ?? string.Empty,
            HotelAddress        = hotel?.Address       ?? string.Empty,
            GSTCode             = hotel?.GSTCode,
            Summary             = data.Summary,
            Bookings            = data.Bookings,
            Lines               = linesDict,
            DrillDownLines      = drillDict
        };

        ViewData["Title"] = "Booking Details Report";
        return View(vm);
    }

    // ── Occupancy & Revenue Charts ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> OccupancyRevenue(DateOnly? fromDate, DateOnly? toDate)
    {
        var branchId = CurrentBranchID;
        var effectiveFrom = fromDate ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var effectiveTo   = toDate   ?? DateOnly.FromDateTime(DateTime.Today);
        if (effectiveTo < effectiveFrom) (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);

        var data = await _reportsRepository.GetOccupancyRevenueAnalyticsAsync(branchId, effectiveFrom, effectiveTo);
        var vm = new OccupancyRevenueViewModel
        {
            FromDate  = effectiveFrom,
            ToDate    = effectiveTo,
            Summary   = data.Summary,
            Daily     = data.Daily,
            RoomTypes = data.RoomTypes
        };
        ViewData["Title"] = "Occupancy & Revenue";
        return View(vm);
    }

    // ── Monthly Report ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> MonthlyReport(int? month, int? year)
    {
        var branchId = CurrentBranchID;
        var m = month ?? DateTime.Today.Month;
        var y = year  ?? DateTime.Today.Year;
        // clamp
        if (m < 1 || m > 12) m = DateTime.Today.Month;
        if (y < 2000 || y > 2100) y = DateTime.Today.Year;

        var vm = new MonthlyReportViewModel { Month = m, Year = y };
        var data = await _reportsRepository.GetOccupancyRevenueAnalyticsAsync(branchId, vm.FromDate, vm.ToDate);
        vm.Summary   = data.Summary;
        vm.Daily     = data.Daily;
        vm.RoomTypes = data.RoomTypes;

        ViewData["Title"] = $"Monthly Report – {vm.MonthName}";
        return View(vm);
    }

    // ── Due Amount Alerts ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DueAlerts()
    {
        var branchId = CurrentBranchID;
        var data = await _reportsRepository.GetDueAmountAlertsAsync(branchId);
        var vm = new DueAlertsViewModel { Summary = data.Summary, Rows = data.Rows };
        ViewData["Title"] = "Due Amount Alerts";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendDueAlert(int bookingId, string guestEmail,
        string guestName, string bookingNumber, decimal dueAmount)
    {
        var branchId = CurrentBranchID;
        if (string.IsNullOrWhiteSpace(guestEmail))
            return Json(new { success = false, message = "No email address on file for this guest." });

        var settings = await _hotelSettingsRepository.GetByBranchAsync(branchId);
        var hotelName = settings?.HotelName ?? "Hotel";

        var htmlBody = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden">
              <div style="background:#1e3a5f;padding:20px 24px;">
                <h2 style="color:#fff;margin:0">{hotelName}</h2>
                <p style="color:#93c5fd;margin:4px 0 0">Payment Due Notice</p>
              </div>
              <div style="padding:24px">
                <p>Dear <strong>{guestName}</strong>,</p>
                <p>This is a friendly reminder that your booking <strong>#{bookingNumber}</strong> has an outstanding balance.</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0">
                  <tr style="background:#f3f4f6">
                    <td style="padding:10px 14px;font-weight:600">Booking Number</td>
                    <td style="padding:10px 14px">{bookingNumber}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px 14px;font-weight:600">Outstanding Amount</td>
                    <td style="padding:10px 14px;color:#dc2626;font-size:1.2em;font-weight:700">₹{dueAmount:N2}</td>
                  </tr>
                </table>
                <p>Please settle this amount at the earliest convenience. If you have already made the payment, kindly ignore this message.</p>
                <p style="margin-top:24px">Thank you for staying with us.</p>
                <p style="color:#6b7280;font-size:0.9em">{hotelName}</p>
              </div>
            </div>
            """;

        try
        {
            await _mailSender.SendEmailAsync(branchId, guestEmail,
                $"Payment Due Reminder – Booking #{bookingNumber}", htmlBody);
            return Json(new { success = true, message = $"Alert sent to {guestEmail}" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
