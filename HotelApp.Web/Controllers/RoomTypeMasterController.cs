using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RoomTypeMasterController : Controller
    {
        private readonly IRoomTypeRepository _roomTypeRepository;

        public RoomTypeMasterController(IRoomTypeRepository roomTypeRepository)
        {
            _roomTypeRepository = roomTypeRepository;
        }

        // GET: RoomTypeMaster/List
        public async Task<IActionResult> List()
        {
            var roomTypes = await _roomTypeRepository.GetAllAsync();
            return View(roomTypes);
        }

        // GET: RoomTypeMaster/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoomTypeMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                // Check if room type name already exists
                if (await _roomTypeRepository.RoomTypeNameExistsAsync(roomType.TypeName))
                {
                    ModelState.AddModelError("TypeName", "A room type with this name already exists.");
                    return View(roomType);
                }

                roomType.CreatedBy = GetCurrentUserId();
                await _roomTypeRepository.CreateAsync(roomType);
                TempData["SuccessMessage"] = "Room Type created successfully!";
                return RedirectToAction(nameof(List));
            }

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
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Room Type";
            return View("Edit", roomType);
        }

        // POST: RoomTypeMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomType roomType)
        {
            if (id != roomType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if room type name already exists (excluding current room type)
                if (await _roomTypeRepository.RoomTypeNameExistsAsync(roomType.TypeName, roomType.Id))
                {
                    ModelState.AddModelError("TypeName", "A room type with this name already exists.");
                    return View(roomType);
                }

                roomType.LastModifiedBy = GetCurrentUserId();
                await _roomTypeRepository.UpdateAsync(roomType);
                TempData["SuccessMessage"] = "Room Type updated successfully!";
                return RedirectToAction(nameof(List));
            }

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
