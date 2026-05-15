using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BanquetReportsController : BaseController
    {
        private readonly IBanquetReportsRepository _reportsRepo;

        public BanquetReportsController(IBanquetReportsRepository reportsRepo)
        {
            _reportsRepo = reportsRepo;
        }

        // ── 1. Collection Register ────────────────────────────────────────────

        public async Task<IActionResult> CollectionRegister(DateOnly? fromDate, DateOnly? toDate)
        {
            fromDate ??= DateOnly.FromDateTime(DateTime.Today);
            toDate   ??= DateOnly.FromDateTime(DateTime.Today);

            var data = await _reportsRepo.GetCollectionRegisterAsync(CurrentBranchID, fromDate.Value, toDate.Value);

            var vm = new BanquetCollectionRegisterViewModel
            {
                FromDate   = fromDate.Value,
                ToDate     = toDate.Value,
                Summary    = data.Summary,
                DailyTotals = data.DailyTotals,
                Details    = data.Details
            };

            ViewData["Title"] = "Banquet Collection Register";
            return View(vm);
        }

        // ── 2. GST Register ───────────────────────────────────────────────────

        public async Task<IActionResult> GSTRegister(DateOnly? fromDate, DateOnly? toDate)
        {
            fromDate ??= DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            toDate   ??= DateOnly.FromDateTime(DateTime.Today);

            var rows = await _reportsRepo.GetGSTRegisterAsync(CurrentBranchID, fromDate.Value, toDate.Value);

            var vm = new BanquetGSTRegisterViewModel
            {
                FromDate = fromDate.Value,
                ToDate   = toDate.Value,
                Rows     = rows
            };

            ViewData["Title"] = "Banquet GST Register";
            return View(vm);
        }

        // ── 3. Venue Utilization ──────────────────────────────────────────────

        public async Task<IActionResult> VenueUtilization(DateOnly? fromDate, DateOnly? toDate)
        {
            fromDate ??= DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            toDate   ??= DateOnly.FromDateTime(DateTime.Today);

            var rows = await _reportsRepo.GetVenueUtilizationAsync(CurrentBranchID, fromDate.Value, toDate.Value);

            var vm = new BanquetVenueUtilizationViewModel
            {
                FromDate = fromDate.Value,
                ToDate   = toDate.Value,
                Rows     = rows
            };

            ViewData["Title"] = "Venue Utilization Report";
            return View(vm);
        }

        // ── 4. Event Type Performance ─────────────────────────────────────────

        public async Task<IActionResult> EventTypePerformance(DateOnly? fromDate, DateOnly? toDate)
        {
            fromDate ??= DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, 1, 1));
            toDate   ??= DateOnly.FromDateTime(DateTime.Today);

            var rows = await _reportsRepo.GetEventTypePerformanceAsync(CurrentBranchID, fromDate.Value, toDate.Value);

            var vm = new BanquetEventTypePerformanceViewModel
            {
                FromDate = fromDate.Value,
                ToDate   = toDate.Value,
                Rows     = rows
            };

            ViewData["Title"] = "Event Type Performance";
            return View(vm);
        }

        // ── 5. Outstanding Balance ────────────────────────────────────────────

        public async Task<IActionResult> OutstandingBalance()
        {
            var rows = await _reportsRepo.GetOutstandingBalanceAsync(CurrentBranchID);

            var vm = new BanquetOutstandingViewModel
            {
                Rows = rows
            };

            ViewData["Title"] = "Banquet Outstanding Balance";
            return View(vm);
        }
    }
}
