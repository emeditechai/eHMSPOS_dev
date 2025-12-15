using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class OtherChargesMasterController : BaseController
    {
        private readonly IOtherChargeRepository _otherChargeRepository;

        public OtherChargesMasterController(IOtherChargeRepository otherChargeRepository)
        {
            _otherChargeRepository = otherChargeRepository;
        }

        // GET: OtherChargesMaster/List
        public async Task<IActionResult> List()
        {
            var rows = await _otherChargeRepository.GetByBranchAsync(CurrentBranchID);
            ViewData["Title"] = "Other Charges Master";
            return View(rows);
        }

        // GET: OtherChargesMaster/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Other Charge";
            return View(new OtherCharge { IsActive = true, GSTPercent = 0, CGSTPercent = 0, SGSTPercent = 0 });
        }

        // POST: OtherChargesMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OtherCharge otherCharge)
        {
            NormalizeAndCalculateTax(otherCharge);

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(otherCharge.Code))
                {
                    ModelState.AddModelError("Code", "Code is required.");
                    return View(otherCharge);
                }

                if (await _otherChargeRepository.CodeExistsAsync(otherCharge.Code, CurrentBranchID))
                {
                    ModelState.AddModelError("Code", "Code already exists in this branch.");
                    return View(otherCharge);
                }

                otherCharge.CreatedBy = GetCurrentUserId();
                otherCharge.BranchID = CurrentBranchID;

                await _otherChargeRepository.CreateAsync(otherCharge);
                TempData["SuccessMessage"] = "Other charge created successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(otherCharge);
        }

        // GET: OtherChargesMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var row = await _otherChargeRepository.GetByIdAsync(id);
            if (row == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Other Charge";
            return View(row);
        }

        // GET: OtherChargesMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var row = await _otherChargeRepository.GetByIdAsync(id);
            if (row == null)
            {
                return NotFound();
            }

            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Other Charge";
            return View("Edit", row);
        }

        // POST: OtherChargesMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OtherCharge otherCharge)
        {
            if (id != otherCharge.Id)
            {
                return BadRequest();
            }

            NormalizeAndCalculateTax(otherCharge);

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(otherCharge.Code))
                {
                    ModelState.AddModelError("Code", "Code is required.");
                    return View(otherCharge);
                }

                if (await _otherChargeRepository.CodeExistsAsync(otherCharge.Code, CurrentBranchID, otherCharge.Id))
                {
                    ModelState.AddModelError("Code", "Code already exists in this branch.");
                    return View(otherCharge);
                }

                otherCharge.UpdatedBy = GetCurrentUserId();
                await _otherChargeRepository.UpdateAsync(otherCharge);

                TempData["SuccessMessage"] = "Other charge updated successfully!";
                return RedirectToAction(nameof(List));
            }

            return View(otherCharge);
        }

        private static void NormalizeAndCalculateTax(OtherCharge otherCharge)
        {
            otherCharge.Code = (otherCharge.Code ?? string.Empty).Trim();
            otherCharge.Name = (otherCharge.Name ?? string.Empty).Trim();

            if (otherCharge.GSTPercent < 0) otherCharge.GSTPercent = 0;

            var half = Math.Round(otherCharge.GSTPercent / 2m, 2, MidpointRounding.AwayFromZero);
            otherCharge.CGSTPercent = half;
            otherCharge.SGSTPercent = half;
        }
    }
}
