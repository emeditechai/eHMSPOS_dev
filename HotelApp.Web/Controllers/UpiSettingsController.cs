using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class UpiSettingsController : BaseController
    {
        private readonly IUpiSettingsRepository _upiSettingsRepository;

        public UpiSettingsController(IUpiSettingsRepository upiSettingsRepository)
        {
            _upiSettingsRepository = upiSettingsRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _upiSettingsRepository.GetByBranchAsync(CurrentBranchID);
            if (settings == null)
            {
                settings = new UpiSettings
                {
                    BranchID = CurrentBranchID,
                    IsEnabled = false,
                    UpiVpa = string.Empty,
                    PayeeName = string.Empty
                };
            }

            return View(settings);
        }

        // If someone navigates to /UpiSettings/Save directly, redirect to the settings page.
        [HttpGet]
        public IActionResult Save()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Save(UpiSettings model)
        {
            return Index(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpiSettings model)
        {
            model.BranchID = CurrentBranchID;
            model.LastModifiedBy = CurrentUserId;

            // Validation: only required when enabled.
            if (model.IsEnabled)
            {
                if (string.IsNullOrWhiteSpace(model.UpiVpa))
                {
                    ModelState.AddModelError(nameof(model.UpiVpa), "UPI ID is required when UPI payments are enabled.");
                }
                else
                {
                    var vpa = model.UpiVpa.Trim();
                    if (vpa.Contains(' '))
                    {
                        ModelState.AddModelError(nameof(model.UpiVpa), "UPI ID must not contain spaces.");
                    }
                    if (!vpa.Contains('@'))
                    {
                        ModelState.AddModelError(nameof(model.UpiVpa), "UPI ID must be a valid VPA (example: name@bank).");
                    }
                    if (vpa.Length > 100)
                    {
                        ModelState.AddModelError(nameof(model.UpiVpa), "UPI ID is too long.");
                    }
                }

                if (string.IsNullOrWhiteSpace(model.PayeeName))
                {
                    ModelState.AddModelError(nameof(model.PayeeName), "Business/Hotel Name is required when UPI payments are enabled.");
                }
                else if (model.PayeeName.Trim().Length > 100)
                {
                    ModelState.AddModelError(nameof(model.PayeeName), "Business/Hotel Name is too long.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var existing = await _upiSettingsRepository.GetByBranchAsync(CurrentBranchID);
                if (existing == null)
                {
                    model.CreatedBy = CurrentUserId;
                }
                else
                {
                    model.Id = existing.Id;
                    model.CreatedBy = existing.CreatedBy;
                }

                await _upiSettingsRepository.UpsertAsync(model);

                TempData["SuccessMessage"] = "UPI settings saved successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving UPI settings: {ex.Message}";
                return View(model);
            }
        }
    }
}
