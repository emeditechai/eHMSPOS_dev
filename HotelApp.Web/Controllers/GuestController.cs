using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    public class GuestController : Controller
    {
        private readonly IGuestRepository _guestRepository;

        public GuestController(IGuestRepository guestRepository)
        {
            _guestRepository = guestRepository;
        }

        public async Task<IActionResult> Index()
        {
            // Get all active guests
            var guests = await GetAllGuestsAsync();
            
            // Calculate statistics
            var totalGuests = guests.Count();
            var primaryGuests = guests.Count(g => g.GuestType == "Primary");
            var companionGuests = guests.Count(g => g.GuestType == "Companion" || g.GuestType == "Child");
            var recentGuests = guests.Count(g => g.CreatedDate >= DateTime.UtcNow.AddDays(-30));

            ViewBag.TotalGuests = totalGuests;
            ViewBag.PrimaryGuests = primaryGuests;
            ViewBag.CompanionGuests = companionGuests;
            ViewBag.RecentGuests = recentGuests;

            return View(guests.OrderByDescending(g => g.LastModifiedDate));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var guest = await _guestRepository.GetByIdAsync(id);
            if (guest == null)
            {
                TempData["ErrorMessage"] = "Guest not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get child guests if this is a parent
            if (guest.GuestType == "Primary")
            {
                var childGuests = await _guestRepository.GetChildGuestsAsync(id);
                ViewBag.ChildGuests = childGuests;
            }

            return View(guest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guest guest)
        {
            if (!ModelState.IsValid)
            {
                if (guest.GuestType == "Primary")
                {
                    var childGuests = await _guestRepository.GetChildGuestsAsync(guest.Id);
                    ViewBag.ChildGuests = childGuests;
                }
                return View(guest);
            }

            var success = await _guestRepository.UpdateAsync(guest);
            if (success)
            {
                TempData["SuccessMessage"] = "Guest details updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update guest details.";
            return View(guest);
        }

        public async Task<IActionResult> Details(int id)
        {
            var guest = await _guestRepository.GetByIdAsync(id);
            if (guest == null)
            {
                TempData["ErrorMessage"] = "Guest not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get child guests if this is a parent
            if (guest.GuestType == "Primary")
            {
                var childGuests = await _guestRepository.GetChildGuestsAsync(id);
                ViewBag.ChildGuests = childGuests;
            }

            // Get parent guest if this is a child
            if (guest.ParentGuestId.HasValue)
            {
                var parentGuest = await _guestRepository.GetByIdAsync(guest.ParentGuestId.Value);
                ViewBag.ParentGuest = parentGuest;
            }

            return View(guest);
        }

        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var allGuests = await GetAllGuestsAsync();
            var filteredGuests = allGuests.Where(g =>
                g.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).OrderByDescending(g => g.LastModifiedDate);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalGuests = filteredGuests.Count();
            ViewBag.PrimaryGuests = 0;
            ViewBag.CompanionGuests = 0;
            ViewBag.RecentGuests = 0;

            return View("Index", filteredGuests);
        }

        private async Task<List<Guest>> GetAllGuestsAsync()
        {
            var guests = await _guestRepository.GetAllAsync();
            return guests.ToList();
        }
    }
}
