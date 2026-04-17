using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

[Authorize]
public class CashierDashboardController : BaseController
{
    private readonly IPaymentDashboardRepository _paymentDashboardRepository;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;

    public CashierDashboardController(
        IPaymentDashboardRepository paymentDashboardRepository,
        ILicenseRepository licenseRepository,
        IHotelSettingsRepository hotelSettingsRepository)
    {
        _paymentDashboardRepository = paymentDashboardRepository;
        _licenseRepository = licenseRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
    }

    public async Task<IActionResult> Index(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var to   = toDate   ?? DateTime.Today;
        var from = fromDate ?? DateTime.Today;

        // Clamp: from cannot be after to
        if (from > to) from = to;

        var data = await _paymentDashboardRepository.GetPaymentDashboardDataAsync(CurrentBranchID, from, to);

        // License alert (same logic as other dashboards)
        string? alertMessage = null;
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var license = await _licenseRepository.GetActiveLicenseAsync(appUrl);
        if (license is { IsDisplayAlerts: true, AlertStartDate: not null, AlertEndDate: not null, AlertStartTime: not null, AlertEndTime: not null }
            && !string.IsNullOrWhiteSpace(license.AlertMessage))
        {
            var now        = DateTime.Now;
            var alertStart = license.AlertStartDate.Value.Date + license.AlertStartTime.Value;
            var alertEnd   = license.AlertEndDate.Value.Date   + license.AlertEndTime.Value;
            if (now >= alertStart && now <= alertEnd)
                alertMessage = license.AlertMessage;
        }

        var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);

        ViewData["Title"]    = "Payment Dashboard";
        ViewBag.AlertMessage = alertMessage;
        ViewBag.HotelName    = hotelSettings?.HotelName;
        return View(data);
    }

    // AJAX endpoint — refresh data for a date range without page reload
    [HttpGet]
    public async Task<IActionResult> GetData(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var to   = toDate   ?? DateTime.Today;
        var from = fromDate ?? DateTime.Today;
        if (from > to) from = to;

        var data = await _paymentDashboardRepository.GetPaymentDashboardDataAsync(CurrentBranchID, from, to);

        return Json(new
        {
            summary = new
            {
                totalPayments = data.Summary.TotalPayments,
                totalGst         = data.Summary.TotalGST,
                totalDiscount    = data.Summary.TotalDiscount,
                paymentCount     = data.Summary.PaymentCount,
                avgPayment    = data.Summary.AveragePayment
            },
            methodBreakdown     = data.MethodBreakdown,
            billingHeadBreakdown = data.BillingHeadBreakdown,
            recentPayments      = data.RecentPayments,
            dailyTrend          = data.DailyTrend.Select(t => new
            {
                date  = t.PaymentDate.ToString("dd MMM"),
                count = t.TransactionCount,
                total = t.TotalAmount
            })
        });
    }
}
