using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class HotelSettingsController : BaseController
    {
        private readonly IHotelSettingsRepository _hotelSettingsRepository;

        public HotelSettingsController(IHotelSettingsRepository hotelSettingsRepository)
        {
            _hotelSettingsRepository = hotelSettingsRepository;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
            
            // If no settings exist, create a default one
            if (settings == null)
            {
                settings = new HotelSettings
                {
                    BranchID = CurrentBranchID,
                    HotelName = "",
                    Address = "",
                    ContactNumber1 = "",
                    EmailAddress = "",
                    GSTCode = "",
                    CheckInTime = new TimeSpan(14, 0, 0), // 2:00 PM
                    CheckOutTime = new TimeSpan(11, 0, 0), // 11:00 AM
                    IsActive = true
                };
            }

            return View(settings);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var settings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
            
            // If no settings exist, create a default one
            if (settings == null)
            {
                settings = new HotelSettings
                {
                    BranchID = CurrentBranchID,
                    HotelName = "",
                    Address = "",
                    ContactNumber1 = "",
                    EmailAddress = "",
                    GSTCode = "",
                    CheckInTime = new TimeSpan(14, 0, 0), // 2:00 PM
                    CheckOutTime = new TimeSpan(11, 0, 0), // 11:00 AM
                    IsActive = true
                };
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HotelSettings model, IFormFile? logoFile)
        {
            // Handle logo file upload
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Generate unique filename
                var fileExtension = Path.GetExtension(logoFile.FileName);
                var uniqueFileName = $"logo_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                
                // Set the logo path in the model
                model.LogoPath = $"/uploads/logos/{uniqueFileName}";
            }
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Ensure BranchID matches current user's branch
                model.BranchID = CurrentBranchID;
                model.IsActive = true;

                await _hotelSettingsRepository.UpsertAsync(model, CurrentUserId ?? 0);

                TempData["SuccessMessage"] = "Hotel settings saved successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving settings: {ex.Message}";
                return View(model);
            }
        }
    }
}
