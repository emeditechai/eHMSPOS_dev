using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RoomMasterController : Controller
    {
        private readonly IRoomRepository _roomRepository;

        public RoomMasterController(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        // GET: RoomMaster/List
        public async Task<IActionResult> List()
        {
            var rooms = await _roomRepository.GetAllAsync();
            return View(rooms);
        }

        // GET: RoomMaster/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesAsync();
            ViewBag.Statuses = new List<string> { "Available", "Occupied", "Cleaning", "Maintenance", "Out of Order" };
            return View();
        }

        // POST: RoomMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            if (await _roomRepository.RoomNumberExistsAsync(room.RoomNumber))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists");
            }

            if (ModelState.IsValid)
            {
                room.IsActive = true;
                await _roomRepository.CreateAsync(room);
                TempData["SuccessMessage"] = "Room created successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesAsync();
            ViewBag.Statuses = new List<string> { "Available", "Occupied", "Cleaning", "Maintenance", "Out of Order" };
            return View(room);
        }

        // GET: RoomMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesAsync();
            ViewBag.Statuses = new List<string> { "Available", "Occupied", "Cleaning", "Maintenance", "Out of Order" };
            return View(room);
        }

        // POST: RoomMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id)
            {
                return BadRequest();
            }

            if (await _roomRepository.RoomNumberExistsAsync(room.RoomNumber, room.Id))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists");
            }

            if (ModelState.IsValid)
            {
                await _roomRepository.UpdateAsync(room);
                TempData["SuccessMessage"] = "Room updated successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesAsync();
            ViewBag.Statuses = new List<string> { "Available", "Occupied", "Cleaning", "Maintenance", "Out of Order" };
            return View(room);
        }

        // POST: RoomMaster/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _roomRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Room deleted successfully!";
            return RedirectToAction(nameof(List));
        }
    }
}
