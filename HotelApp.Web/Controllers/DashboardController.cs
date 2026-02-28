using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;

    public DashboardController(IDashboardRepository dashboardRepository, IHotelSettingsRepository hotelSettingsRepository)
    {
        _dashboardRepository = dashboardRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard";
        
        // Get dashboard data
        var statistics = await _dashboardRepository.GetDashboardStatisticsAsync(CurrentBranchID);
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, 7);
        var roomTypeDistribution = await _dashboardRepository.GetRoomTypeDistributionAsync(CurrentBranchID);
        var recentBookings = await _dashboardRepository.GetRecentBookingsAsync(CurrentBranchID, 5);
        var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
        
        ViewBag.Statistics = statistics;
        ViewBag.RevenueData = revenueData;
        ViewBag.RoomTypeDistribution = roomTypeDistribution;
        ViewBag.RecentBookings = recentBookings;
        ViewBag.HotelName = hotelSettings?.HotelName;
        
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueData(int days = 7)
    {
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, days);
        return Json(revenueData);
    }
}
