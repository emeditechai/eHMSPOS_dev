using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    public class GuestController : BaseController
    {
        private readonly IGuestRepository _guestRepository;
        private readonly ILocationRepository _locationRepository;

        public GuestController(
            IGuestRepository guestRepository,
            ILocationRepository locationRepository)
        {
            _guestRepository = guestRepository;
            _locationRepository = locationRepository;
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

            // Load location data for dropdowns
            await PopulateLocationDataAsync(guest.CountryId, guest.StateId, guest.CityId);

            return View(guest);
        }
        
        private async Task PopulateLocationDataAsync(int? countryId = null, int? stateId = null, int? cityId = null)
        {
            // Load all countries
            ViewBag.Countries = await _locationRepository.GetCountriesAsync();
            
            // Load states if country is selected
            if (countryId.HasValue)
            {
                ViewBag.States = await _locationRepository.GetStatesByCountryAsync(countryId.Value);
            }
            else
            {
                ViewBag.States = new List<State>();
            }
            
            // Load cities if state is selected
            if (stateId.HasValue)
            {
                ViewBag.Cities = await _locationRepository.GetCitiesByStateAsync(stateId.Value);
            }
            else
            {
                ViewBag.Cities = new List<City>();
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetStates(int countryId)
        {
            var states = await _locationRepository.GetStatesByCountryAsync(countryId);
            return Json(states.Select(s => new { id = s.Id, name = s.Name }));
        }
        
        [HttpGet]
        public async Task<IActionResult> GetCities(int stateId)
        {
            var cities = await _locationRepository.GetCitiesByStateAsync(stateId);
            return Json(cities.Select(c => new { id = c.Id, name = c.Name }));
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
                
                // Reload location data on validation failure
                await PopulateLocationDataAsync(guest.CountryId, guest.StateId, guest.CityId);
                
                return View(guest);
            }

            // Preserve BranchID from the existing guest record
            var existingGuest = await _guestRepository.GetByIdAsync(guest.Id);
            if (existingGuest != null)
            {
                guest.BranchID = existingGuest.BranchID;
            }
            else
            {
                // If guest not found, use current user's branch
                guest.BranchID = CurrentBranchID;
            }

            var success = await _guestRepository.UpdateAsync(guest);
            if (success)
            {
                TempData["SuccessMessage"] = "Guest details updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update guest details.";
            
            // Reload location data on update failure
            await PopulateLocationDataAsync(guest.CountryId, guest.StateId, guest.CityId);
            
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

            // Load location names from IDs if not already populated
            if (guest.CountryId.HasValue && string.IsNullOrEmpty(guest.Country))
            {
                var countries = await _locationRepository.GetCountriesAsync();
                var country = countries.FirstOrDefault(c => c.Id == guest.CountryId.Value);
                if (country != null)
                {
                    guest.Country = country.Name;
                }
            }
            
            if (guest.StateId.HasValue && string.IsNullOrEmpty(guest.State))
            {
                var states = await _locationRepository.GetStatesByCountryAsync(guest.CountryId ?? 1);
                var state = states.FirstOrDefault(s => s.Id == guest.StateId.Value);
                if (state != null)
                {
                    guest.State = state.Name;
                }
            }
            
            if (guest.CityId.HasValue && string.IsNullOrEmpty(guest.City))
            {
                var cities = await _locationRepository.GetCitiesByStateAsync(guest.StateId ?? 1);
                var city = cities.FirstOrDefault(c => c.Id == guest.CityId.Value);
                if (city != null)
                {
                    guest.City = city.Name;
                }
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
            var guests = await _guestRepository.GetAllByBranchAsync(CurrentBranchID);
            return guests.ToList();
        }
    }
}
