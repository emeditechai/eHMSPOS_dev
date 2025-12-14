using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class AmenitiesMasterController : BaseController
    {
        private readonly IAmenityRepository _amenityRepository;

        public AmenitiesMasterController(IAmenityRepository amenityRepository)
        {
            _amenityRepository = amenityRepository;
        }

        // GET: AmenitiesMaster/List
        public async Task<IActionResult> List()
        {
            var amenities = await _amenityRepository.GetByBranchAsync(CurrentBranchID);
            ViewData["Title"] = "Amenities Master";
            return View(amenities);
        }

        // GET: AmenitiesMaster/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Amenity";
            return View();
        }

        // POST: AmenitiesMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Amenity amenity)
        {
            if (ModelState.IsValid)
            {
                if (await _amenityRepository.AmenityNameExistsAsync(amenity.AmenityName, CurrentBranchID))
                {
                    ModelState.AddModelError("AmenityName", "An amenity with this name already exists in this branch.");
                    return View(amenity);
                }

                amenity.CreatedBy = GetCurrentUserId();
                amenity.BranchID = CurrentBranchID;
                await _amenityRepository.CreateAsync(amenity);
                TempData["SuccessMessage"] = "Amenity created successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(amenity);
        }

        // GET: AmenitiesMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var amenity = await _amenityRepository.GetByIdAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Amenity";
            return View(amenity);
        }

        // GET: AmenitiesMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var amenity = await _amenityRepository.GetByIdAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }

            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Amenity";
            return View("Edit", amenity);
        }

        // POST: AmenitiesMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Amenity amenity)
        {
            if (id != amenity.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                if (await _amenityRepository.AmenityNameExistsAsync(amenity.AmenityName, CurrentBranchID, amenity.Id))
                {
                    ModelState.AddModelError("AmenityName", "An amenity with this name already exists in this branch.");
                    return View(amenity);
                }

                amenity.UpdatedBy = GetCurrentUserId();
                await _amenityRepository.UpdateAsync(amenity);
                TempData["SuccessMessage"] = "Amenity updated successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(amenity);
        }
    }
}
