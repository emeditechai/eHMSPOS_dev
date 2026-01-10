using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BookingReceiptTemplateController : BaseController
    {
        private readonly IBookingReceiptTemplateRepository _bookingReceiptTemplateRepository;

        public BookingReceiptTemplateController(IBookingReceiptTemplateRepository bookingReceiptTemplateRepository)
        {
            _bookingReceiptTemplateRepository = bookingReceiptTemplateRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var existing = await _bookingReceiptTemplateRepository.GetByBranchAsync(CurrentBranchID);

            var vm = new BookingReceiptTemplateConfigurationViewModel
            {
                CurrentTemplateKey = string.IsNullOrWhiteSpace(existing?.TemplateKey) ? "classic" : existing!.TemplateKey,
                Templates = new[]
                {
                    new BookingReceiptTemplateConfigurationViewModel.ReceiptTemplateOption
                    {
                        Key = "classic",
                        Name = "Classic (Existing)",
                        Description = "Current receipt design (existing template)."
                    },
                    new BookingReceiptTemplateConfigurationViewModel.ReceiptTemplateOption
                    {
                        Key = "modern",
                        Name = "Modern",
                        Description = "Clean card-style layout, strong headings, clear totals."
                    },
                    new BookingReceiptTemplateConfigurationViewModel.ReceiptTemplateOption
                    {
                        Key = "compact",
                        Name = "Compact",
                        Description = "Print-friendly condensed layout with smaller spacing."
                    },
                    new BookingReceiptTemplateConfigurationViewModel.ReceiptTemplateOption
                    {
                        Key = "minimal",
                        Name = "Minimal",
                        Description = "Black & white minimalist layout for fast printing."
                    }
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string templateKey)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "classic",
                "modern",
                "compact",
                "minimal"
            };

            if (string.IsNullOrWhiteSpace(templateKey) || !allowed.Contains(templateKey))
            {
                TempData["ErrorMessage"] = "Invalid template selected.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _bookingReceiptTemplateRepository.UpsertAsync(CurrentBranchID, templateKey.Trim().ToLowerInvariant(), CurrentUserId);
                TempData["SuccessMessage"] = "Default booking receipt template saved.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving template: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
