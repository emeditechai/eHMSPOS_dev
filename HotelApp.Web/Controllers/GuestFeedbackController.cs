using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class GuestFeedbackController : BaseController
    {
        private readonly IGuestFeedbackRepository _guestFeedbackRepository;
        private readonly IHotelSettingsRepository _hotelSettingsRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IGuestFeedbackLinkService _linkService;

        public GuestFeedbackController(
            IGuestFeedbackRepository guestFeedbackRepository,
            IHotelSettingsRepository hotelSettingsRepository,
            IBookingRepository bookingRepository,
            IGuestFeedbackLinkService linkService)
        {
            _guestFeedbackRepository = guestFeedbackRepository;
            _hotelSettingsRepository = hotelSettingsRepository;
            _bookingRepository = bookingRepository;
            _linkService = linkService;
        }

        [HttpGet]
        public async Task<IActionResult> List(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var items = await _guestFeedbackRepository.GetByBranchAsync(CurrentBranchID, fromDate, toDate);

            var vm = items.Select(x => new GuestFeedbackListItemViewModel
            {
                Id = x.Id,
                VisitDate = x.VisitDate,
                BookingNumber = x.BookingNumber,
                RoomNumber = x.RoomNumber,
                GuestName = x.GuestName,
                Phone = x.Phone,
                OverallRating = x.OverallRating,
                QuickTags = x.QuickTags,
                CreatedDate = x.CreatedDate
            }).ToList();

            ViewData["Title"] = "Guest Feedback";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Create(string? bookingNumber = null, string? t = null)
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;
            Booking? booking = null;
            var resolvedBranchId = CurrentBranchID;
            string? resolvedToken = t;

            if (!isAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(resolvedToken) || !_linkService.TryValidateToken(resolvedToken, out var payload))
                {
                    return BadRequest("Invalid or expired feedback link.");
                }

                bookingNumber = payload.BookingNumber;
                resolvedBranchId = payload.BranchId;
            }

            var hotel = await _hotelSettingsRepository.GetByBranchAsync(resolvedBranchId);

            var vm = new GuestFeedbackCreateViewModel
            {
                VisitDate = DateTime.Today,
                HotelName = hotel?.HotelName,
                HotelAddress = hotel?.Address,
                HotelEmail = hotel?.EmailAddress,
                HotelWebsite = hotel?.Website,
                AccessToken = resolvedToken
            };

            if (!string.IsNullOrWhiteSpace(bookingNumber))
            {
                booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
                if (booking != null && booking.BranchID == resolvedBranchId)
                {
                    var assignedRoomNumbers = booking.AssignedRooms
                        .Select(ar => ar.Room?.RoomNumber)
                        .Where(rn => !string.IsNullOrWhiteSpace(rn))
                        .Distinct()
                        .ToList();

                    var resolvedRoomNumber = assignedRoomNumbers.Any()
                        ? string.Join(", ", assignedRoomNumbers)
                        : booking.Room?.RoomNumber;

                    vm.BookingId = booking.Id;
                    vm.BookingNumber = booking.BookingNumber;
                    vm.RoomNumber = resolvedRoomNumber;
                    vm.GuestName = $"{booking.PrimaryGuestFirstName} {booking.PrimaryGuestLastName}".Trim();
                    vm.Phone = booking.PrimaryGuestPhone;
                    vm.Email = booking.PrimaryGuestEmail;
                    vm.VisitDate = booking.CheckOutDate.Date;
                }
                else if (!isAuthenticated)
                {
                    return NotFound();
                }
            }

            ViewData["Title"] = "Guest Feedback";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create(GuestFeedbackCreateViewModel vm)
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;
            var resolvedBranchId = CurrentBranchID;

            if (!isAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(vm.AccessToken) || !_linkService.TryValidateToken(vm.AccessToken, out var payload))
                {
                    return BadRequest("Invalid or expired feedback link.");
                }

                resolvedBranchId = payload.BranchId;
                var booking = await _bookingRepository.GetByBookingNumberAsync(payload.BookingNumber);
                if (booking == null || booking.BranchID != resolvedBranchId)
                {
                    return NotFound();
                }

                var assignedRoomNumbers = booking.AssignedRooms
                    .Select(ar => ar.Room?.RoomNumber)
                    .Where(rn => !string.IsNullOrWhiteSpace(rn))
                    .Distinct()
                    .ToList();

                vm.BookingId = booking.Id;
                vm.BookingNumber = booking.BookingNumber;
                vm.RoomNumber = assignedRoomNumbers.Any()
                    ? string.Join(", ", assignedRoomNumbers)
                    : booking.Room?.RoomNumber;
            }

            var hotel = await _hotelSettingsRepository.GetByBranchAsync(resolvedBranchId);
            vm.HotelName = hotel?.HotelName;
            vm.HotelAddress = hotel?.Address;
            vm.HotelEmail = hotel?.EmailAddress;
            vm.HotelWebsite = hotel?.Website;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Guest Feedback";
                return View(vm);
            }

            var createdBy = isAuthenticated ? GetCurrentUserId() : null;

            var entity = new GuestFeedback
            {
                BranchID = resolvedBranchId,
                BookingId = vm.BookingId,
                BookingNumber = vm.BookingNumber,
                RoomNumber = vm.RoomNumber,
                VisitDate = vm.VisitDate.Date,
                GuestName = vm.GuestName,
                Email = vm.Email,
                Phone = vm.Phone,
                Birthday = vm.Birthday?.Date,
                Anniversary = vm.Anniversary?.Date,
                IsFirstVisit = vm.IsFirstVisit,

                OverallRating = vm.OverallRating,
                RoomCleanlinessRating = vm.RoomCleanlinessRating,
                StaffBehaviorRating = vm.StaffBehaviorRating,
                ServiceRating = vm.ServiceRating,
                RoomComfortRating = vm.RoomComfortRating,
                AmenitiesRating = vm.AmenitiesRating,
                FoodRating = vm.FoodRating,
                ValueForMoneyRating = vm.ValueForMoneyRating,
                CheckInExperienceRating = vm.CheckInExperienceRating,

                QuickTags = vm.QuickTags,
                Comments = vm.Comments,
                CreatedBy = createdBy
            };

            var id = await _guestFeedbackRepository.CreateAsync(entity);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var hotel = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
            var feedback = await _guestFeedbackRepository.GetByIdAsync(id, CurrentBranchID);
            if (feedback == null)
            {
                return NotFound();
            }

            var vm = new GuestFeedbackDetailsViewModel
            {
                Id = feedback.Id,
                VisitDate = feedback.VisitDate,
                BookingNumber = feedback.BookingNumber,
                RoomNumber = feedback.RoomNumber,
                GuestName = feedback.GuestName,
                Email = feedback.Email,
                Phone = feedback.Phone,
                Birthday = feedback.Birthday,
                Anniversary = feedback.Anniversary,
                IsFirstVisit = feedback.IsFirstVisit,

                OverallRating = feedback.OverallRating,
                RoomCleanlinessRating = feedback.RoomCleanlinessRating,
                StaffBehaviorRating = feedback.StaffBehaviorRating,
                ServiceRating = feedback.ServiceRating,
                RoomComfortRating = feedback.RoomComfortRating,
                AmenitiesRating = feedback.AmenitiesRating,
                FoodRating = feedback.FoodRating,
                ValueForMoneyRating = feedback.ValueForMoneyRating,
                CheckInExperienceRating = feedback.CheckInExperienceRating,

                QuickTags = feedback.QuickTags,
                Comments = feedback.Comments,
                CreatedDate = feedback.CreatedDate,

                HotelName = hotel?.HotelName,
                HotelAddress = hotel?.Address,
                HotelEmail = hotel?.EmailAddress,
                HotelWebsite = hotel?.Website
            };

            ViewData["Title"] = "Guest Feedback";
            return View(vm);
        }
    }
}
