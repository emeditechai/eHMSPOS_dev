using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RateMasterController : Controller
    {
        private readonly IRateMasterRepository _rateMasterRepository;

        public RateMasterController(IRateMasterRepository rateMasterRepository)
        {
            _rateMasterRepository = rateMasterRepository;
        }

        // GET: RateMaster/List
        public async Task<IActionResult> List()
        {
            var rates = await _rateMasterRepository.GetAllAsync();
            return View(rates);
        }

        // GET: RateMaster/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = await _rateMasterRepository.GetRoomTypesAsync();
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
                
                await _rateMasterRepository.CreateAsync(rate);
                TempData["SuccessMessage"] = "Rate created successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _rateMasterRepository.GetRoomTypesAsync();
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

            ViewBag.RoomTypes = await _rateMasterRepository.GetRoomTypesAsync();
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
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

            ViewBag.RoomTypes = await _rateMasterRepository.GetRoomTypesAsync();
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
        }

        // POST: RateMaster/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _rateMasterRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = "Rate deleted successfully!";
            return RedirectToAction(nameof(List));
        }
    }
}
