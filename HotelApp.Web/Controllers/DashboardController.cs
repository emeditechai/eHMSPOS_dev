using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard";
        
        // Get dashboard data
        var statistics = await _dashboardRepository.GetDashboardStatisticsAsync(CurrentBranchID);
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, 7);
        var roomTypeDistribution = await _dashboardRepository.GetRoomTypeDistributionAsync(CurrentBranchID);
        var recentBookings = await _dashboardRepository.GetRecentBookingsAsync(CurrentBranchID, 5);
        
        ViewBag.Statistics = statistics;
        ViewBag.RevenueData = revenueData;
        ViewBag.RoomTypeDistribution = roomTypeDistribution;
        ViewBag.RecentBookings = recentBookings;
        
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueData(int days = 7)
    {
        var revenueData = await _dashboardRepository.GetRevenueOverviewAsync(CurrentBranchID, days);
        return Json(revenueData);
    }
}
