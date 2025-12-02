using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RoomMasterController : BaseController
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IRateMasterRepository _rateMasterRepository;

        public RoomMasterController(IRoomRepository roomRepository, IFloorRepository floorRepository, IRateMasterRepository rateMasterRepository)
        {
            _roomRepository = roomRepository;
            _floorRepository = floorRepository;
            _rateMasterRepository = rateMasterRepository;
        }

        // GET: RoomMaster/List
        public async Task<IActionResult> List()
        {
            var rooms = await _roomRepository.GetAllByBranchAsync(CurrentBranchID);
            return View(rooms);
        }

        // GET: RoomMaster/Create
        public async Task<IActionResult> Create()
        {
            await PopulateRoomLookupsAsync();
            return View();
        }

        // POST: RoomMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            await ValidateFloorAsync(room.Floor);

            if (await _roomRepository.RoomNumberExistsAsync(room.RoomNumber, CurrentBranchID))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists in this branch");
            }

            if (ModelState.IsValid)
            {
                room.IsActive = true;
                room.BranchID = CurrentBranchID;
                await _roomRepository.CreateAsync(room);
                TempData["SuccessMessage"] = "Room created successfully!";
                return RedirectToAction(nameof(List));
            }

            await PopulateRoomLookupsAsync();
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

            await PopulateRoomLookupsAsync();
            return View(room);
        }

        // GET: RoomMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            await PopulateRoomLookupsAsync();
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Room";
            return View("Edit", room);
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

            await ValidateFloorAsync(room.Floor);

            if (await _roomRepository.RoomNumberExistsAsync(room.RoomNumber, CurrentBranchID, room.Id))
            {
                ModelState.AddModelError("RoomNumber", "Room number already exists in this branch");
            }

            if (ModelState.IsValid)
            {
                await _roomRepository.UpdateAsync(room);
                TempData["SuccessMessage"] = "Room updated successfully!";
                return RedirectToAction(nameof(List));
            }

            await PopulateRoomLookupsAsync();
            return View(room);
        }

        // Delete functionality removed (business rule: no deletions)

        private async Task PopulateRoomLookupsAsync()
        {
            // Get room types
            var roomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            
            // Get active rates for display
            var rates = await _rateMasterRepository.GetByBranchAsync(CurrentBranchID);
            var today = DateTime.Today;
            var activeRates = rates
                .Where(r => r.StartDate <= today && r.EndDate >= today)
                .GroupBy(r => r.RoomTypeId)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());
            
            ViewBag.RoomTypes = roomTypes;
            ViewBag.ActiveRates = activeRates; // Dictionary of RoomTypeId -> RateMaster
            ViewBag.Statuses = new List<string> { "Available", "Occupied", "Cleaning", "Maintenance", "Out of Order" };
            var floors = await _floorRepository.GetByBranchAsync(CurrentBranchID);
            ViewBag.Floors = floors.Where(f => f.IsActive).OrderBy(f => f.FloorName).ToList();
        }

        private async Task ValidateFloorAsync(int floorId)
        {
            var floor = await _floorRepository.GetByIdAsync(floorId);
            if (floor == null || !floor.IsActive)
            {
                ModelState.AddModelError("Floor", "Selected floor is inactive or unavailable. Please pick an active floor from Floor Master.");
            }
        }
    }
}
