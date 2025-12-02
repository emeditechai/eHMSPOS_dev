using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RateMasterController : BaseController
    {
        private readonly IRateMasterRepository _rateMasterRepository;
        private readonly IRoomRepository _roomRepository;

        public RateMasterController(IRateMasterRepository rateMasterRepository, IRoomRepository roomRepository)
        {
            _rateMasterRepository = rateMasterRepository;
            _roomRepository = roomRepository;
        }

        // GET: RateMaster/List
        public async Task<IActionResult> List()
        {
            var rates = await _rateMasterRepository.GetByBranchAsync(CurrentBranchID);
            return View(rates);
        }

        // GET: RateMaster/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            
            var model = new RateMaster
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                IsWeekdayRate = true,
                IsDynamicRate = false,
                IsActive = true
            };
            
            return View(model);
        }

        // POST: RateMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RateMaster rate)
        {
            if (rate.EndDate < rate.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userId, out int userIdInt))
                {
                    rate.CreatedBy = userIdInt;
                }
                rate.BranchID = CurrentBranchID;
                
                await _rateMasterRepository.CreateAsync(rate);
                TempData["SuccessMessage"] = "Rate created successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
        }

        // GET: RateMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var rate = await _rateMasterRepository.GetByIdAsync(id);
            if (rate == null)
            {
                return NotFound();
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
        }

        // GET: RateMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var rate = await _rateMasterRepository.GetByIdAsync(id);
            if (rate == null)
            {
                return NotFound();
            }
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Rate";
            return View("Edit", rate);
        }

        // POST: RateMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RateMaster rate)
        {
            if (id != rate.Id)
            {
                return BadRequest();
            }

            if (rate.EndDate < rate.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                await _rateMasterRepository.UpdateAsync(rate);
                TempData["SuccessMessage"] = "Rate updated successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
        }

        // Delete functionality removed (business rule: no deletions)
    }
}
