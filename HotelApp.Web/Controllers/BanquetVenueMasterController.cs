using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BanquetVenueMasterController : BaseController
    {
        private readonly IBanquetVenueRepository _venueRepo;
        private readonly IFloorRepository _floorRepo;
        private readonly IGstSlabRepository _gstSlabRepo;

        public BanquetVenueMasterController(
            IBanquetVenueRepository venueRepo,
            IFloorRepository floorRepo,
            IGstSlabRepository gstSlabRepo)
        {
            _venueRepo    = venueRepo;
            _floorRepo    = floorRepo;
            _gstSlabRepo  = gstSlabRepo;
        }

        public async Task<IActionResult> List()
        {
            var venues = await _venueRepo.GetByBranchAsync(CurrentBranchID, false);
            ViewData["Title"] = "Venue Master";
            return View(venues);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Venue";
            ViewBag.Floors    = await _floorRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.GstSlabs  = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(new BanquetVenue { IsActive = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanquetVenue model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Floors   = await _floorRepo.GetByBranchAsync(CurrentBranchID);
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }

            if (await _venueRepo.CodeExistsAsync(model.VenueCode, CurrentBranchID))
            {
                ModelState.AddModelError("VenueCode", "Venue Code already exists in this branch.");
                ViewBag.Floors   = await _floorRepo.GetByBranchAsync(CurrentBranchID);
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }

            model.BranchID  = CurrentBranchID;
            model.CreatedBy = GetCurrentUserId();
            await _venueRepo.CreateAsync(model);
            TempData["SuccessMessage"] = "Venue created successfully.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _venueRepo.GetByIdAsync(id);
            if (venue == null) return NotFound();
            ViewData["Title"] = "Edit Venue";
            ViewBag.Floors   = await _floorRepo.GetByBranchAsync(CurrentBranchID);
            ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(venue);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BanquetVenue model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Floors   = await _floorRepo.GetByBranchAsync(CurrentBranchID);
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }

            if (await _venueRepo.CodeExistsAsync(model.VenueCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError("VenueCode", "Venue Code already exists in this branch.");
                ViewBag.Floors   = await _floorRepo.GetByBranchAsync(CurrentBranchID);
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }

            model.UpdatedBy = GetCurrentUserId();
            await _venueRepo.UpdateAsync(model);
            TempData["SuccessMessage"] = "Venue updated successfully.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Details(int id)
        {
            var venue = await _venueRepo.GetByIdAsync(id);
            if (venue == null) return NotFound();
            ViewData["Title"] = "Venue Details";
            return View(venue);
        }

        [HttpGet]
        public async Task<IActionResult> GetGstSlabBands(int slabId)
        {
            var slab = await _gstSlabRepo.GetByIdAsync(slabId);
            if (slab == null) return NotFound();
            var bands = slab.TariffBands
                .Where(b => b.IsActive)
                .OrderBy(b => b.TariffFrom)
                .Select(b => new
                {
                    b.TariffFrom,
                    b.TariffTo,
                    b.GstPercent,
                    b.CgstPercent,
                    b.SgstPercent,
                    b.IgstPercent
                });
            return Json(bands);
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int venueId, string eventDate, string? startTime, string? endTime, int? excludeBookingId)
        {
            if (!DateOnly.TryParse(eventDate, out var date))
                return Json(new { available = false, message = "Invalid date." });

            TimeOnly? start = TimeOnly.TryParse(startTime, out var ts) ? ts : null;
            TimeOnly? end   = TimeOnly.TryParse(endTime,   out var te) ? te : null;

            var available = await _venueRepo.IsVenueAvailableAsync(venueId, date, start, end, excludeBookingId);
            return Json(new { available, message = available ? "Venue is available." : "Venue is already booked for this time slot." });
        }

        private int? GetCurrentUserId() => CurrentUserId;
    }
}
