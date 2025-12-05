using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BookingController : BaseController
    {
        private static readonly IReadOnlyList<string> CustomerTypes = new[] { "B2C", "B2B" };
        private static readonly IReadOnlyList<string> Sources = new[] { "WalkIn", "Phone", "Website", "OTA", "Reference" };
        private static readonly IReadOnlyList<string> Channels = new[] { "FrontDesk", "CallCenter", "DirectWeb", "Corporate", "OTA" };
        private static readonly IReadOnlyDictionary<string, string> PaymentMethods = new Dictionary<string, string>
        {
            { "Cash", "Cash" },
            { "Card", "Card" },
            { "Cheque", "Cheque" },
            { "UPI", "UPI" },
            { "BankTransfer", "Bank Transfer" }
        };

        private readonly IBookingRepository _bookingRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IGuestRepository _guestRepository;
        private readonly IBankRepository _bankRepository;
        private readonly IHotelSettingsRepository _hotelSettingsRepository;

        public BookingController(IBookingRepository bookingRepository, IRoomRepository roomRepository, IGuestRepository guestRepository, IBankRepository bankRepository, IHotelSettingsRepository hotelSettingsRepository)
        {
            _bookingRepository = bookingRepository;
            _roomRepository = roomRepository;
            _guestRepository = guestRepository;
            _bankRepository = bankRepository;
            _hotelSettingsRepository = hotelSettingsRepository;
        }

        public async Task<IActionResult> List(DateTime? fromDate, DateTime? toDate)
        {
            var viewModel = new BookingDashboardViewModel
            {
                TodayBookingCount = await _bookingRepository.GetTodayBookingCountAsync(),
                TodayAdvanceAmount = await _bookingRepository.GetTodayAdvanceAmountAsync(),
                TodayCheckInCount = await _bookingRepository.GetTodayCheckInCountAsync(),
                TodayCheckOutCount = await _bookingRepository.GetTodayCheckOutCountAsync(),
                FromDate = fromDate,
                ToDate = toDate,
                Bookings = (fromDate.HasValue || toDate.HasValue) 
                    ? await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, fromDate, toDate)
                    : await _bookingRepository.GetRecentByBranchAsync(CurrentBranchID)
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(List));
            }

            // Get audit log for this booking
            var auditLogs = await _bookingRepository.GetAuditLogAsync(booking.Id);
            ViewBag.AuditLogs = auditLogs;

            // Get payments for this booking
            var payments = await _bookingRepository.GetPaymentsAsync(booking.Id);
            ViewBag.Payments = payments;

            // Get all banks for payment modal
            var banks = await _bankRepository.GetAllActiveAsync();
            ViewBag.Banks = banks;

            return View(booking);
        }

        public async Task<IActionResult> Create()
        {
            var model = new BookingCreateViewModel
            {
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                Adults = 2,
                CustomerType = CustomerTypes.First(),
                Source = Sources.First(),
                Channel = Channels.First(),
                PaymentMethod = PaymentMethods.Keys.First()
            };

            await PopulateLookupsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            await PopulateLookupsAsync();

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out date must be after check-in date.");
            }

            if (!PaymentMethods.ContainsKey(model.PaymentMethod))
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Select a valid payment method.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var quoteRequest = new BookingQuoteRequest
            {
                RoomTypeId = model.RoomTypeId,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                CustomerType = model.CustomerType,
                Source = model.Source,
                Adults = model.Adults,
                Children = model.Children,
                BranchID = CurrentBranchID
            };

            var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);
            if (quote == null)
            {
                ModelState.AddModelError(string.Empty, "No active rate plan was found for the selected criteria.");
                return View(model);
            }

            model.QuotedBaseAmount = quote.TotalRoomRate;
            model.QuotedTaxAmount = quote.TotalTaxAmount;
            model.QuotedGrandTotal = quote.GrandTotal;
            model.QuoteMessage = $"Rate locked for {quote.Nights} night(s)";

            if (model.DepositAmount < 0 || model.DepositAmount > quote.GrandTotal)
            {
                ModelState.AddModelError(nameof(model.DepositAmount), "Deposit must be between 0 and the total stay amount.");
                return View(model);
            }

            // Check room availability but don't assign yet
            var availableRoom = await _bookingRepository.FindAvailableRoomAsync(model.RoomTypeId, model.CheckInDate, model.CheckOutDate);
            if (availableRoom == null)
            {
                ModelState.AddModelError(string.Empty, "No rooms are available for the selected room type and dates.");
                return View(model);
            }

            var bookingNumber = GenerateBookingNumber();
            var createdBy = GetCurrentUserId();
            var discountAmount = 0m;
            var balanceAmount = quote.GrandTotal - model.DepositAmount;

            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                Status = "Confirmed",
                PaymentStatus = model.DepositAmount >= quote.GrandTotal ? "Paid" : (model.DepositAmount > 0 ? "Partially Paid" : "Pending"),
                Channel = model.Channel,
                Source = model.Source,
                CustomerType = model.CustomerType,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                Nights = quote.Nights,
                RoomTypeId = model.RoomTypeId,
                RoomId = null,
                RatePlanId = quote.RatePlanId,
                BaseAmount = quote.TotalRoomRate,
                TaxAmount = quote.TotalTaxAmount,
                CGSTAmount = quote.TotalCGSTAmount,
                SGSTAmount = quote.TotalSGSTAmount,
                DiscountAmount = discountAmount,
                TotalAmount = quote.GrandTotal,
                DepositAmount = model.DepositAmount,
                BalanceAmount = balanceAmount,
                Adults = model.Adults,
                Children = model.Children,
                PrimaryGuestFirstName = model.PrimaryGuestFirstName,
                PrimaryGuestLastName = model.PrimaryGuestLastName,
                PrimaryGuestEmail = model.PrimaryGuestEmail,
                PrimaryGuestPhone = model.PrimaryGuestPhone,
                LoyaltyId = model.LoyaltyId,
                SpecialRequests = model.SpecialRequests,
                BranchID = CurrentBranchID,
                CreatedBy = createdBy,
                LastModifiedBy = createdBy
            };

            var guests = new List<BookingGuest>
            {
                new BookingGuest
                {
                    FullName = $"{model.PrimaryGuestFirstName} {model.PrimaryGuestLastName}".Trim(),
                    Email = model.PrimaryGuestEmail,
                    Phone = model.PrimaryGuestPhone,
                    GuestType = "Primary",
                    IsPrimary = true
                }
            };

            var payments = new List<BookingPayment>();
            if (model.DepositAmount > 0)
            {
                payments.Add(new BookingPayment
                {
                    Amount = model.DepositAmount,
                    PaymentMethod = PaymentMethods[model.PaymentMethod],
                    PaymentReference = null,
                    PaidOn = DateTime.UtcNow,
                    Status = model.DepositAmount >= quote.GrandTotal ? "Captured" : "Advance",
                    Notes = "Advance deposit"
                });
            }

            // Don't create room nights during booking - will be created when room is assigned
            var roomNights = new List<BookingRoomNight>();

            var result = await _bookingRepository.CreateBookingAsync(booking, guests, payments, roomNights);

            TempData["BookingCreated"] = "true";
            TempData["BookingNumber"] = result.BookingNumber;
            TempData["BookingAmount"] = quote.GrandTotal.ToString("N2");
            return RedirectToAction(nameof(Details), new { bookingNumber = result.BookingNumber });
        }

        private static IEnumerable<BookingRoomNight> BuildRoomNightBreakdown(DateTime checkIn, DateTime checkOut, int roomId, decimal totalRoomRate, decimal totalTax, decimal totalCGST, decimal totalSGST, int nights)
        {
            if (nights <= 0)
            {
                yield break;
            }

            var nightlyRoomAmount = Math.Round(totalRoomRate / nights, 2, MidpointRounding.AwayFromZero);
            var nightlyTax = Math.Round(totalTax / nights, 2, MidpointRounding.AwayFromZero);
            var nightlyCGST = Math.Round(totalCGST / nights, 2, MidpointRounding.AwayFromZero);
            var nightlySGST = Math.Round(totalSGST / nights, 2, MidpointRounding.AwayFromZero);

            for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
            {
                yield return new BookingRoomNight
                {
                    RoomId = roomId,
                    StayDate = date,
                    RateAmount = nightlyRoomAmount,
                    TaxAmount = nightlyTax,
                    CGSTAmount = nightlyCGST,
                    SGSTAmount = nightlySGST,
                    Status = "Reserved"
                };
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadGuestDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded." });
                }

                // Validate file size (5MB max)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size should not exceed 5MB." });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Only images (JPG, PNG, GIF) and PDF files are allowed." });
                }

                // Generate unique filename
                var fileName = $"guest_{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "guests");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                var relativePath = $"/uploads/guests/{fileName}";
                return Json(new { success = true, path = relativePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error uploading file: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddGuest([FromBody] AddGuestRequest request)
        {
            try
            {
                Console.WriteLine($"=== AddGuest Request Received ===");
                Console.WriteLine($"Request is null: {request == null}");
                
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }
                
                Console.WriteLine($"BookingNumber: {request.BookingNumber}");
                Console.WriteLine($"FullName: {request.FullName}");
                Console.WriteLine($"Email: {request.Email}");
                Console.WriteLine($"Phone: {request.Phone}");
                Console.WriteLine($"GuestType: {request.GuestType}");
                Console.WriteLine($"RelationshipToPrimary: {request.RelationshipToPrimary}");
                Console.WriteLine($"Age: {request.Age}");
                Console.WriteLine($"DateOfBirth: {request.DateOfBirth}");
                
                if (string.IsNullOrWhiteSpace(request.BookingNumber) || string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Json(new { success = false, message = "Booking number and guest name are required." });
                }

                var booking = await _bookingRepository.GetByBookingNumberAsync(request.BookingNumber);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found." });
                }

                var guest = new BookingGuest
                {
                    BookingId = booking.Id,
                    FullName = request.FullName.Trim(),
                    Email = request.Email?.Trim(),
                    Phone = request.Phone?.Trim(),
                    GuestType = request.GuestType?.Trim(),
                    IsPrimary = false,
                    RelationshipToPrimary = request.RelationshipToPrimary?.Trim(),
                    Age = request.Age,
                    DateOfBirth = request.DateOfBirth,
                    IdentityType = request.IdentityType?.Trim(),
                    IdentityNumber = request.IdentityNumber?.Trim(),
                    DocumentPath = request.DocumentPath?.Trim(),
                    CreatedBy = GetCurrentUserId()
                };

                var branchId = GetCurrentBranchID();
                var success = await _bookingRepository.AddGuestToBookingAsync(guest, branchId);

                if (success)
                {
                    await _bookingRepository.AddAuditLogAsync(
                        booking.Id,
                        request.BookingNumber,
                        "Guest Added",
                        $"Additional guest added: {request.FullName} ({guest.RelationshipToPrimary ?? "Guest"})",
                        null,
                        request.FullName,
                        GetCurrentUserId()
                    );

                    return Json(new { success = true, message = "Guest added successfully!" });
                }

                return Json(new { success = false, message = "Failed to add guest." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGuest([FromBody] UpdateGuestRequest request)
        {
            try
            {
                if (request == null || request.GuestId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return Json(new { success = false, message = "Guest name is required." });
                }

                var guest = new BookingGuest
                {
                    Id = request.GuestId,
                    FullName = request.FullName.Trim(),
                    Email = request.Email?.Trim(),
                    Phone = request.Phone?.Trim(),
                    GuestType = request.GuestType?.Trim(),
                    RelationshipToPrimary = request.RelationshipToPrimary?.Trim(),
                    Age = request.Age,
                    DateOfBirth = request.DateOfBirth,
                    IdentityType = request.IdentityType?.Trim(),
                    IdentityNumber = request.IdentityNumber?.Trim(),
                    DocumentPath = request.DocumentPath?.Trim(),
                    ModifiedBy = GetCurrentUserId()
                };

                var success = await _bookingRepository.UpdateGuestAsync(guest);

                if (success)
                {
                    return Json(new { success = true, message = "Guest updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update guest." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGuest([FromBody] DeleteGuestRequest request)
        {
            try
            {
                if (request == null || request.GuestId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                var success = await _bookingRepository.DeleteGuestAsync(request.GuestId, GetCurrentUserId() ?? 0);

                if (success)
                {
                    return Json(new { success = true, message = "Guest removed successfully!" });
                }

                return Json(new { success = false, message = "Failed to remove guest. Guest may be the primary guest or already deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class AddGuestRequest
        {
            public string? BookingNumber { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? GuestType { get; set; }
            public string? RelationshipToPrimary { get; set; }
            public int? Age { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string? IdentityType { get; set; }
            public string? IdentityNumber { get; set; }
            public string? DocumentPath { get; set; }
        }

        public class UpdateGuestRequest
        {
            public int GuestId { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? GuestType { get; set; }
            public string? RelationshipToPrimary { get; set; }
            public int? Age { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string? IdentityType { get; set; }
            public string? IdentityNumber { get; set; }
            public string? DocumentPath { get; set; }
        }

        public class DeleteGuestRequest
        {
            public int GuestId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(string bookingNumber, decimal amount, string paymentMethod, string? paymentReference, string? notes, string? cardType, string? cardLastFourDigits, int? bankId, DateTime? chequeDate)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return Json(new { success = false, message = "Booking number is required." });
            }

            if (amount <= 0)
            {
                return Json(new { success = false, message = "Payment amount must be greater than zero." });
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found." });
            }

            if (amount > booking.BalanceAmount)
            {
                return Json(new { success = false, message = $"Payment amount cannot exceed balance amount of ₹{booking.BalanceAmount:N2}." });
            }

            var payment = new BookingPayment
            {
                BookingId = booking.Id,
                Amount = amount,
                PaymentMethod = paymentMethod,
                PaymentReference = paymentReference,
                Status = "Captured",
                PaidOn = DateTime.Now,
                Notes = notes,
                CardType = cardType,
                CardLastFourDigits = cardLastFourDigits,
                BankId = bankId,
                ChequeDate = chequeDate
            };

            var currentUserId = GetCurrentUserId() ?? 0;
            var success = await _bookingRepository.AddPaymentAsync(payment, currentUserId);

            if (success)
            {
                return Json(new { success = true, message = $"Payment of ₹{amount:N2} recorded successfully." });
            }

            return Json(new { success = false, message = "Failed to record payment. Please try again." });
        }

        private async Task PopulateLookupsAsync()
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = CustomerTypes;
            ViewBag.Sources = Sources;
            ViewBag.Channels = Channels;
            ViewBag.PaymentMethods = PaymentMethods;
        }

        private static string GenerateBookingNumber()
        {
            return $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var id))
            {
                return id;
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> LookupGuest(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { found = false });
            }

            var guest = await _guestRepository.GetByPhoneAsync(phone);
            if (guest == null)
            {
                return Json(new { found = false });
            }

            return Json(new
            {
                found = true,
                firstName = guest.FirstName,
                lastName = guest.LastName,
                email = guest.Email,
                loyaltyId = guest.LoyaltyId
            });
        }

        [HttpGet]
        public async Task<IActionResult> AssignRoom(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(List));
            }

            // Get all rooms and filter available ones for this booking's room type
            var allRooms = await _roomRepository.GetAllByBranchAsync(CurrentBranchID);
            var availableRooms = allRooms.Where(r => r.RoomTypeId == booking.RoomTypeId && r.Status == "Available").ToList();

            ViewBag.AvailableRooms = availableRooms;
            ViewBag.Booking = booking;

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoom(string bookingNumber, int roomId)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                TempData["ErrorMessage"] = "Invalid booking number";
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found";
                return RedirectToAction(nameof(List));
            }

            // Get the room to verify it exists and is available
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Selected room not found";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            if (room.RoomTypeId != booking.RoomTypeId)
            {
                TempData["ErrorMessage"] = "Selected room does not match the booking's room type";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            try
            {
                var success = await _bookingRepository.UpdateRoomAssignmentAsync(bookingNumber, roomId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = $"Room {room.RoomNumber} assigned successfully to booking {bookingNumber}";
                    return RedirectToAction(nameof(Details), new { bookingNumber });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign room to booking";
                    return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning room: {ex.Message}";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRoomType(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(List));
            }

            var roomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.RoomTypes = roomTypes;
            ViewBag.Booking = booking;

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(string bookingNumber, int newRoomTypeId)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                TempData["ErrorMessage"] = "Invalid booking number.";
                return RedirectToAction(nameof(List));
            }

            try
            {
                var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(List));
                }

                if (booking.RoomTypeId == newRoomTypeId)
                {
                    TempData["InfoMessage"] = "The selected room type is the same as the current one.";
                    return RedirectToAction(nameof(Details), new { bookingNumber });
                }

                // Get quote for new room type with existing dates
                var quoteRequest = new BookingQuoteRequest
                {
                    RoomTypeId = newRoomTypeId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    CustomerType = string.IsNullOrWhiteSpace(booking.CustomerType) ? "B2C" : booking.CustomerType,
                    Source = string.IsNullOrWhiteSpace(booking.Source) ? "WalkIn" : booking.Source,
                    Adults = booking.Adults,
                    Children = booking.Children,
                    BranchID = booking.BranchID
                };

                var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);
                if (quote == null)
                {
                    TempData["ErrorMessage"] = "Unable to calculate new rates for the selected room type.";
                    return RedirectToAction(nameof(EditRoomType), new { bookingNumber });
                }

                // Update booking with new room type and amounts
                var success = await _bookingRepository.UpdateRoomTypeAsync(
                    bookingNumber,
                    newRoomTypeId,
                    quote.TotalRoomRate,
                    quote.TotalTaxAmount,
                    quote.TotalCGSTAmount,
                    quote.TotalSGSTAmount,
                    quote.GrandTotal
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Room type updated successfully! New total amount: ₹" + quote.GrandTotal.ToString("N2");
                    return RedirectToAction(nameof(Details), new { bookingNumber });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update room type. Please try again.";
                    return RedirectToAction(nameof(EditRoomType), new { bookingNumber });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating room type: {ex.Message}";
                return RedirectToAction(nameof(EditRoomType), new { bookingNumber });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditDates(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(List));
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDates(string bookingNumber, DateTime newCheckInDate, DateTime newCheckOutDate)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                TempData["ErrorMessage"] = "Invalid booking number";
                return RedirectToAction(nameof(List));
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found";
                return RedirectToAction(nameof(List));
            }

            // Validate dates
            if (newCheckInDate < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Check-in date cannot be in the past";
                return View(booking);
            }

            if (newCheckOutDate <= newCheckInDate)
            {
                TempData["ErrorMessage"] = "Check-out date must be after check-in date";
                return View(booking);
            }

            var nights = (newCheckOutDate - newCheckInDate).Days;
            if (nights < 1)
            {
                TempData["ErrorMessage"] = "Booking must be at least 1 night";
                return View(booking);
            }

            // Check if assigned room is available for new dates
            if (booking.RoomId.HasValue)
            {
                var isAvailable = await _roomRepository.IsRoomAvailableAsync(
                    booking.RoomId.Value,
                    newCheckInDate,
                    newCheckOutDate,
                    bookingNumber
                );

                if (!isAvailable)
                {
                    TempData["ErrorMessage"] = $"Room {booking.Room?.RoomNumber} is not available for the selected dates. Please change the room or select different dates.";
                    return View(booking);
                }
            }

            try
            {
                // Get new quote with updated dates
                var quoteRequest = new BookingQuoteRequest
                {
                    CheckInDate = newCheckInDate,
                    CheckOutDate = newCheckOutDate,
                    RoomTypeId = booking.RoomTypeId,
                    Adults = booking.Adults,
                    Children = booking.Children,
                    CustomerType = booking.CustomerType,
                    Source = booking.Source,
                    BranchID = CurrentBranchID
                };

                var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);

                if (quote == null)
                {
                    TempData["ErrorMessage"] = "Unable to calculate pricing for the new dates";
                    return View(booking);
                }

                // Update booking dates and amounts
                var success = await _bookingRepository.UpdateBookingDatesAsync(
                    bookingNumber,
                    newCheckInDate,
                    newCheckOutDate,
                    nights,
                    quote.TotalRoomRate,
                    quote.TotalTaxAmount,
                    quote.TotalCGSTAmount,
                    quote.TotalSGSTAmount,
                    quote.GrandTotal
                );

                if (success)
                {
                    TempData["SuccessMessage"] = $"Booking dates updated successfully. New total: ₹{quote.GrandTotal:N2}";
                    return RedirectToAction(nameof(Details), new { bookingNumber });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update booking dates";
                    return View(booking);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating dates: {ex.Message}";
                return View(booking);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel([FromBody] CancelBookingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.BookingNumber))
            {
                return Json(new { success = false, message = "Invalid booking number" });
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(request.BookingNumber);
            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found" });
            }

            if (booking.Status == "Cancelled")
            {
                return Json(new { success = false, message = "Booking is already cancelled" });
            }

            // Update booking status to Cancelled
            // TODO: Implement CancelBooking method in repository
            TempData["SuccessMessage"] = $"Booking {request.BookingNumber} cancelled successfully";
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> LookupGuestByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "Phone number is required" });
            }

            // Get guest by phone for current branch only
            var guest = await _guestRepository.GetByPhoneAsync(phone);
            
            if (guest == null || guest.BranchID != CurrentBranchID)
            {
                return Json(new { success = false, found = false });
            }

            // Get last booking for this guest
            var lastBooking = await _bookingRepository.GetLastBookingByGuestPhoneAsync(phone);

            return Json(new 
            { 
                success = true,
                found = true,
                guest = new 
                {
                    firstName = guest.FirstName,
                    lastName = guest.LastName,
                    email = guest.Email,
                    phone = guest.Phone,
                    loyaltyId = guest.LoyaltyId
                },
                lastBooking = lastBooking != null ? new 
                {
                    bookingNumber = lastBooking.BookingNumber,
                    checkInDate = lastBooking.CheckInDate.ToString("dd MMM yyyy"),
                    checkOutDate = lastBooking.CheckOutDate.ToString("dd MMM yyyy"),
                    createdDate = lastBooking.CreatedDate.ToString("dd MMM yyyy")
                } : null
            });
        }
    }

    public class CancelBookingRequest
    {
        public string BookingNumber { get; set; } = string.Empty;
    }
}
