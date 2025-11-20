using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class FloorMasterController : Controller
    {
        private readonly IFloorRepository _floorRepository;

        public FloorMasterController(IFloorRepository floorRepository)
        {
            _floorRepository = floorRepository;
        }

        // GET: FloorMaster/List
        public async Task<IActionResult> List()
        {
            var floors = await _floorRepository.GetAllAsync();
            return View(floors);
        }

        // GET: FloorMaster/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FloorMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Floor floor)
        {
            if (ModelState.IsValid)
            {
                // Check if floor name already exists
                if (await _floorRepository.FloorNameExistsAsync(floor.FloorName))
                {
                    ModelState.AddModelError("FloorName", "A floor with this name already exists.");
                    return View(floor);
                }

                floor.CreatedBy = GetCurrentUserId();
                await _floorRepository.CreateAsync(floor);
                TempData["SuccessMessage"] = "Floor created successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(floor);
        }

        // GET: FloorMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var floor = await _floorRepository.GetByIdAsync(id);
            if (floor == null)
            {
                return NotFound();
            }

            return View(floor);
        }

        // GET: FloorMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var floor = await _floorRepository.GetByIdAsync(id);
            if (floor == null)
            {
                return NotFound();
            }
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Floor";
            return View("Edit", floor);
        }

        // POST: FloorMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Floor floor)
        {
            if (id != floor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if floor name already exists (excluding current floor)
                if (await _floorRepository.FloorNameExistsAsync(floor.FloorName, floor.Id))
                {
                    ModelState.AddModelError("FloorName", "A floor with this name already exists.");
                    return View(floor);
                }

                floor.LastModifiedBy = GetCurrentUserId();
                await _floorRepository.UpdateAsync(floor);
                TempData["SuccessMessage"] = "Floor updated successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(floor);
        }

        // Delete functionality removed (business rule: no deletions)

        private int? GetCurrentUserId()
        {
            // This would typically get the user ID from the authentication system
            // For now, returning null as we don't have user IDs in the current auth
            return null;
        }
    }
}
