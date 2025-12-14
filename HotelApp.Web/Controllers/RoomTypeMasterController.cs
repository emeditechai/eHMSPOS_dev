using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RoomTypeMasterController : BaseController
    {
        private readonly IRoomTypeRepository _roomTypeRepository;

        public RoomTypeMasterController(IRoomTypeRepository roomTypeRepository)
        {
            _roomTypeRepository = roomTypeRepository;
        }

        // GET: RoomTypeMaster/List
        public async Task<IActionResult> List()
        {
            var roomTypes = await _roomTypeRepository.GetByBranchAsync(CurrentBranchID);
            return View(roomTypes);
        }

        // GET: RoomTypeMaster/Create
        public async Task<IActionResult> Create()
        {
            var amenities = await _roomTypeRepository.GetAmenitiesByBranchAsync(CurrentBranchID);
            ViewBag.Amenities = amenities;
            return View();
        }

        // POST: RoomTypeMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomType roomType, string[]? amenitiesSelected)
        {
            if (ModelState.IsValid)
            {
                // Check if room type name already exists in this branch
                if (await _roomTypeRepository.RoomTypeNameExistsAsync(roomType.TypeName, CurrentBranchID))
                {
                    ModelState.AddModelError("TypeName", "A room type with this name already exists in this branch.");
                    return View(roomType);
                }

                roomType.CreatedBy = GetCurrentUserId();
                roomType.BranchID = CurrentBranchID;
                roomType.BaseRate = 0; // Base rate is managed in Rate Master

                if (amenitiesSelected != null)
                {
                    roomType.Amenities = string.Join(", ", amenitiesSelected.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct());
                }

                await _roomTypeRepository.CreateAsync(roomType);
                TempData["SuccessMessage"] = "Room Type created successfully!";
                return RedirectToAction(nameof(List));
            }

            var amenities = await _roomTypeRepository.GetAmenitiesByBranchAsync(CurrentBranchID);
            ViewBag.Amenities = amenities;
            return View(roomType);
        }

        // GET: RoomTypeMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var roomType = await _roomTypeRepository.GetByIdAsync(id);
            if (roomType == null)
            {
                return NotFound();
            }

            var amenities = await _roomTypeRepository.GetAmenitiesByBranchAsync(CurrentBranchID);
            ViewBag.Amenities = amenities;
            return View(roomType);
        }

        // GET: RoomTypeMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var roomType = await _roomTypeRepository.GetByIdAsync(id);
            if (roomType == null)
            {
                return NotFound();
            }

            var amenities = await _roomTypeRepository.GetAmenitiesByBranchAsync(CurrentBranchID);
            ViewBag.Amenities = amenities;
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Room Type";
            return View("Edit", roomType);
        }

        // POST: RoomTypeMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomType roomType, string[]? amenitiesSelected)
        {
            if (id != roomType.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                // Check if room type name already exists in this branch (excluding current record)
                if (await _roomTypeRepository.RoomTypeNameExistsAsync(roomType.TypeName, CurrentBranchID, roomType.Id))
                {
                    ModelState.AddModelError("TypeName", "A room type with this name already exists in this branch.");
                    return View(roomType);
                }

                roomType.BaseRate = 0; // Base rate is managed in Rate Master

                if (amenitiesSelected != null)
                {
                    roomType.Amenities = string.Join(", ", amenitiesSelected.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct());
                }

                await _roomTypeRepository.UpdateAsync(roomType);
                TempData["SuccessMessage"] = "Room Type updated successfully!";
                return RedirectToAction(nameof(List));
            }

            var amenities = await _roomTypeRepository.GetAmenitiesByBranchAsync(CurrentBranchID);
            ViewBag.Amenities = amenities;
            return View(roomType);
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
