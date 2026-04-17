using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IRoleDashboardConfigRepository _roleDashboardConfigRepository;

    public DashboardController(IDashboardRepository dashboardRepository, IHotelSettingsRepository hotelSettingsRepository, ILicenseRepository licenseRepository, IRoleDashboardConfigRepository roleDashboardConfigRepository)
    {
        _dashboardRepository = dashboardRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
        _licenseRepository = licenseRepository;
        _roleDashboardConfigRepository = roleDashboardConfigRepository;
    }

    public async Task<IActionResult> Index()
    {
        // If this role is configured to a different dashboard, redirect there
        var roleId = HttpContext.Session.GetInt32("SelectedRoleId") ?? 0;
        if (roleId > 0)
        {
            var config = await _roleDashboardConfigRepository.GetByRoleIdAsync(roleId);
            if (config != null && config.IsActive &&
                !(config.DashboardController.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) &&
                  config.DashboardAction.Equals("Index", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction(config.DashboardAction, config.DashboardController);
            }
        }

        ViewData["Title"] = "Dashboard";
        
        // Get dashboard data
        var statistics = await _dashboardRepository.GetDashboardStatisticsAsync(CurrentBranchID);
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, 7);
        var roomTypeDistribution = await _dashboardRepository.GetRoomTypeDistributionAsync(CurrentBranchID);
        var recentBookings = await _dashboardRepository.GetRecentBookingsAsync(CurrentBranchID, 10);
        var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);

        // Get alert message from active license matched by AppUrl
        string? alertMessage = null;
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var license = await _licenseRepository.GetActiveLicenseAsync(appUrl);

        if (license is { IsDisplayAlerts: true, AlertStartDate: not null, AlertEndDate: not null, AlertStartTime: not null, AlertEndTime: not null }
            && !string.IsNullOrWhiteSpace(license.AlertMessage))
        {
            var now = DateTime.Now;
            var alertStart = license.AlertStartDate.Value.Date + license.AlertStartTime.Value;
            var alertEnd = license.AlertEndDate.Value.Date + license.AlertEndTime.Value;

            if (now >= alertStart && now <= alertEnd)
            {
                alertMessage = license.AlertMessage;
            }
        }
        
        ViewBag.Statistics = statistics;
        ViewBag.RevenueData = revenueData;
        ViewBag.RoomTypeDistribution = roomTypeDistribution;
        ViewBag.RecentBookings = recentBookings;
        ViewBag.HotelName = hotelSettings?.HotelName;
        ViewBag.AlertMessage = alertMessage;
        
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueData(int days = 7)
    {
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, days);
        return Json(revenueData);
    }
}
