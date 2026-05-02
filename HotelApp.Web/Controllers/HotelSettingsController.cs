using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class HotelSettingsController : BaseController
    {
        private readonly IHotelSettingsRepository _hotelSettingsRepository;
        private readonly IEInvoiceProtector _eInvoiceProtector;

        public HotelSettingsController(
            IHotelSettingsRepository hotelSettingsRepository,
            IEInvoiceProtector eInvoiceProtector)
        {
            _hotelSettingsRepository = hotelSettingsRepository;
            _eInvoiceProtector = eInvoiceProtector;
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
                    ByPassActualDayRate = false,
                    DiscountApprovalRequired = false,
                    MinimumBookingAmountRequired = false,
                    MinimumBookingAmount = null,
                    NoShowGraceHours = 6,
                    CancellationRefundApprovalThreshold = null,
                    EnableCancellationPolicy = true,
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
                    ByPassActualDayRate = false,
                    DiscountApprovalRequired = false,
                    MinimumBookingAmountRequired = false,
                    MinimumBookingAmount = null,
                    NoShowGraceHours = 6,
                    CancellationRefundApprovalThreshold = null,
                    EnableCancellationPolicy = true,
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

            if (model.MinimumBookingAmountRequired)
            {
                if (!model.MinimumBookingAmount.HasValue || model.MinimumBookingAmount.Value <= 0 || model.MinimumBookingAmount.Value > 100)
                {
                    ModelState.AddModelError(nameof(model.MinimumBookingAmount), "Minimum Booking Percentage must be between 0 and 100.");
                }
            }

            if (model.NoShowGraceHours < 0 || model.NoShowGraceHours > 72)
            {
                ModelState.AddModelError(nameof(model.NoShowGraceHours), "No-show grace hours must be between 0 and 72.");
            }

            if (model.CancellationRefundApprovalThreshold.HasValue && model.CancellationRefundApprovalThreshold.Value < 0)
            {
                ModelState.AddModelError(nameof(model.CancellationRefundApprovalThreshold), "Cancellation refund approval threshold cannot be negative.");
            }

            // E-Invoice AUTO mode validation
            if (model.EInvoiceMode == "AUTO")
            {
                if (string.IsNullOrWhiteSpace(model.EInvoiceApiBaseUrl))
                    ModelState.AddModelError(nameof(model.EInvoiceApiBaseUrl), "API Base URL is required in AUTO mode.");
                if (string.IsNullOrWhiteSpace(model.EInvoiceAuthUrl))
                    ModelState.AddModelError(nameof(model.EInvoiceAuthUrl), "Authorization URL is required in AUTO mode.");
                if (string.IsNullOrWhiteSpace(model.EInvoiceIrnEndpoint))
                    ModelState.AddModelError(nameof(model.EInvoiceIrnEndpoint), "Generate IRN Endpoint is required in AUTO mode.");
                if (string.IsNullOrWhiteSpace(model.EInvoiceClientId))
                    ModelState.AddModelError(nameof(model.EInvoiceClientId), "Client ID is required in AUTO mode.");
                if (string.IsNullOrWhiteSpace(model.EInvoiceUsername))
                    ModelState.AddModelError(nameof(model.EInvoiceUsername), "Username is required in AUTO mode.");

                // Secrets: validate only when no existing value is stored
                var existingSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
                bool hasExistingSecret = !string.IsNullOrWhiteSpace(existingSettings?.EInvoiceClientSecret);
                bool hasExistingPassword = !string.IsNullOrWhiteSpace(existingSettings?.EInvoicePassword);

                if (string.IsNullOrWhiteSpace(model.EInvoiceClientSecret) && !hasExistingSecret)
                    ModelState.AddModelError(nameof(model.EInvoiceClientSecret), "Client Secret is required in AUTO mode.");
                if (string.IsNullOrWhiteSpace(model.EInvoicePassword) && !hasExistingPassword)
                    ModelState.AddModelError(nameof(model.EInvoicePassword), "Password is required in AUTO mode.");
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

                // Encrypt sensitive E-Invoice fields when in AUTO mode
                if (model.EInvoiceMode == "AUTO")
                {
                    model.EInvoiceClientSecret = !string.IsNullOrWhiteSpace(model.EInvoiceClientSecret)
                        ? _eInvoiceProtector.Protect(model.EInvoiceClientSecret)
                        : null; // null -> COALESCE keeps existing encrypted value in DB

                    model.EInvoicePassword = !string.IsNullOrWhiteSpace(model.EInvoicePassword)
                        ? _eInvoiceProtector.Protect(model.EInvoicePassword)
                        : null; // null -> COALESCE keeps existing encrypted value in DB
                }
                else
                {
                    // MANUAL mode: clear all API config fields
                    model.EInvoiceApiBaseUrl = null;
                    model.EInvoiceAuthUrl = null;
                    model.EInvoiceIrnEndpoint = null;
                    model.EInvoiceClientId = null;
                    model.EInvoiceClientSecret = null;
                    model.EInvoiceUsername = null;
                    model.EInvoicePassword = null;
                    // EInvoiceJsonStoragePath is kept as-is (set by user)
                }

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

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
                
                if (settings == null)
                {
                    return Json(new { success = false, message = "Hotel settings not found" });
                }

                return Json(new
                {
                    success = true,
                    minimumBookingAmountRequired = settings.MinimumBookingAmountRequired,
                    minimumBookingAmount = settings.MinimumBookingAmount ?? 0,
                    enableCancellationPolicy = settings.EnableCancellationPolicy
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
