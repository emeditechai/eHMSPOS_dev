using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class B2BBookingController : BaseController
    {
        private static readonly IReadOnlyList<string> AllowedSources = new[] { "Corporate", "Travel Agent", "OTA" };
        private static readonly IReadOnlyList<string> AllowedChannels = new[] { "Corporate Desk", "Travel Desk", "OTA Desk" };
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
        private readonly IB2BClientRepository _clientRepository;
        private readonly IB2BAgreementRepository _agreementRepository;
        private readonly IGstSlabRepository _gstSlabRepository;
        private readonly IRateMasterRepository _rateMasterRepository;
        private readonly ICancellationPolicyRepository _cancellationPolicyRepository;

        public B2BBookingController(
            IBookingRepository bookingRepository,
            IRoomRepository roomRepository,
            IB2BClientRepository clientRepository,
            IB2BAgreementRepository agreementRepository,
            IGstSlabRepository gstSlabRepository,
            IRateMasterRepository rateMasterRepository,
            ICancellationPolicyRepository cancellationPolicyRepository)
        {
            _bookingRepository = bookingRepository;
            _roomRepository = roomRepository;
            _clientRepository = clientRepository;
            _agreementRepository = agreementRepository;
            _gstSlabRepository = gstSlabRepository;
            _rateMasterRepository = rateMasterRepository;
            _cancellationPolicyRepository = cancellationPolicyRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? statusFilter)
        {
            var isUpcomingFilter = string.Equals(statusFilter, "upcoming", StringComparison.OrdinalIgnoreCase);

            if (!isUpcomingFilter && !fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = DateTime.Today.AddDays(-7);
                toDate = DateTime.Today;
            }

            var bookings = isUpcomingFilter
                ? await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, null, null, 5000)
                : await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, fromDate, toDate, 5000);

            var b2bBookings = bookings
                .Where(IsB2BBooking)
                .ToList();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                b2bBookings = statusFilter.ToLowerInvariant() switch
                {
                    "upcoming" => b2bBookings.Where(b =>
                        !string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        && !b.ActualCheckInDate.HasValue
                        && b.CheckInDate.Date > DateTime.Today).ToList(),
                    "assigned" => b2bBookings.Where(b => b.Room != null || (b.AssignedRooms != null && b.AssignedRooms.Any())).ToList(),
                    "notassigned" => b2bBookings.Where(b => b.Room == null && (b.AssignedRooms == null || !b.AssignedRooms.Any())).ToList(),
                    "checkedin" => b2bBookings.Where(b => b.ActualCheckInDate.HasValue && !b.ActualCheckOutDate.HasValue).ToList(),
                    "checkedout" => b2bBookings.Where(b => b.ActualCheckOutDate.HasValue).ToList(),
                    "cancelled" => b2bBookings.Where(b => string.Equals(b.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "due" => b2bBookings.Where(b => b.BalanceAmount > 0).ToList(),
                    "fullypaid" => b2bBookings.Where(b => b.BalanceAmount <= 0 || string.Equals(b.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)).ToList(),
                    _ => b2bBookings
                };
            }

            var todayB2BBookings = (await _bookingRepository
                    .GetByBranchAndDateRangeAsync(CurrentBranchID, DateTime.Today, DateTime.Today, 5000))
                .Where(IsB2BBooking)
                .ToList();

            var activeClients = (await _clientRepository.GetActiveByBranchAsync(CurrentBranchID)).ToList();
            var activeAgreements = (await _agreementRepository.GetByBranchAsync(CurrentBranchID))
                .Count(a => a.IsActive && a.EffectiveFrom.Date <= DateTime.Today && a.EffectiveTo.Date >= DateTime.Today);

            var viewModel = new B2BBookingDashboardViewModel
            {
                TodayBookingCount = todayB2BBookings.Count,
                TodayAdvanceAmount = todayB2BBookings.Sum(x => x.DepositAmount),
                TotalOutstandingAmount = b2bBookings
                    .Where(x => !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.BalanceAmount),
                ActiveClientCount = activeClients.Count,
                ActiveAgreementCount = activeAgreements,
                FromDate = fromDate,
                ToDate = toDate,
                StatusFilter = statusFilter,
                Bookings = b2bBookings
            };

            ViewData["Title"] = "B2B Booking Dashboard";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new B2BBookingCreateViewModel
            {
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                Adults = 1,
                Source = AllowedSources.First(),
                Channel = AllowedChannels.First(),
                BillingTo = "Company"
            };

            await PopulateLookupsAsync(model);
            ViewData["Title"] = "Create B2B Booking";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(B2BBookingCreateViewModel model)
        {
            await PopulateLookupsAsync(model);
            Normalize(model);

            if (!AllowedSources.Contains(model.Source, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Source), "Select a valid B2B source.");
            }

            if (!AllowedChannels.Contains(model.Channel, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Channel), "Select a valid B2B channel.");
            }

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out date must be after check-in date.");
            }

            if (model.DepositAmount > 0 && !model.CollectAdvancePayment)
            {
                ModelState.AddModelError(nameof(model.CollectAdvancePayment), "Enable advance collection when an advance amount is entered.");
            }

            if (model.CollectAdvancePayment && model.DepositAmount > 0 && string.IsNullOrWhiteSpace(model.AdvancePaymentMethod))
            {
                ModelState.AddModelError(nameof(model.AdvancePaymentMethod), "Advance payment method is required.");
            }

            var client = await _clientRepository.GetByIdAsync(model.ClientId);
            if (client == null || client.BranchID != CurrentBranchID || !client.IsActive)
            {
                ModelState.AddModelError(nameof(model.ClientId), "Select a valid active B2B client.");
            }

            B2BAgreement? agreement = null;
            if (client != null)
            {
                if (!client.AgreementId.HasValue || client.AgreementId.Value <= 0)
                {
                    ModelState.AddModelError(nameof(model.ClientId), "Selected B2B client is not linked to an agreement. Assign Agreement Master in B2B Client Master.");
                }
                else
                {
                    model.AgreementId = client.AgreementId.Value;
                    agreement = await _agreementRepository.GetByIdAsync(client.AgreementId.Value);

                    if (agreement == null || agreement.BranchID != CurrentBranchID || !agreement.IsActive)
                    {
                        ModelState.AddModelError(nameof(model.ClientId), "The assigned agreement for this B2B client is invalid or inactive.");
                    }
                    else if (model.CheckInDate.Date < agreement.EffectiveFrom.Date || model.CheckInDate.Date > agreement.EffectiveTo.Date)
                    {
                        ModelState.AddModelError(nameof(model.AgreementId), "Assigned agreement is not effective for the selected stay dates.");
                    }
                }
            }

            if (!ModelState.IsValid || client == null || agreement == null)
            {
                return View(model);
            }

            var quoteRequest = new BookingQuoteRequest
            {
                RoomTypeId = model.RoomTypeId,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                CustomerType = "B2B",
                Source = model.Source,
                Adults = model.Adults,
                Children = model.Children,
                BranchID = CurrentBranchID,
                RequiredRooms = model.RequiredRooms
            };

            var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);
            if (quote == null)
            {
                ModelState.AddModelError(string.Empty, "No active B2B rate is configured for the selected source and room type.");
                return View(model);
            }

            model.QuotedBaseAmount = quote.TotalRoomRate;
            model.QuotedTaxAmount = quote.TotalTaxAmount;
            model.QuotedGrandTotal = quote.GrandTotal;
            model.QuoteMessage = $"Rate locked for {quote.Nights} night(s)";

            if (model.DepositAmount < 0 || model.DepositAmount > quote.GrandTotal)
            {
                ModelState.AddModelError(nameof(model.DepositAmount), "Advance amount must be between 0 and the quoted grand total.");
                return View(model);
            }

            var hasCapacity = await _bookingRepository.CheckRoomCapacityAvailabilityAsync(
                model.RoomTypeId,
                CurrentBranchID,
                model.CheckInDate,
                model.CheckOutDate,
                model.RequiredRooms);

            if (!hasCapacity)
            {
                ModelState.AddModelError(string.Empty, "The selected room type does not have enough inventory for this B2B stay.");
                return View(model);
            }

            var applicableGstMasterId = ResolveApplicableGstMasterId(agreement, model.RoomTypeId, model.CheckInDate);
            var gstSlab = await ResolveGstSlabAsync(GetPerNightTariff(quote, model), model.CheckInDate, applicableGstMasterId);
            model.GstSlabId = applicableGstMasterId;
            model.GstSlabCode = gstSlab?.SlabCode;
            model.GstSlabName = gstSlab == null
                ? null
                : $"{gstSlab.SlabName} | {gstSlab.TariffFrom:N2} - {(gstSlab.TariffTo.HasValue ? gstSlab.TariffTo.Value.ToString("N2") : "Open")}";

            var derivedRateType = agreement.DiscountPercent > 0 ? "Discounted" : "Standard";
            var (policyId, snapshotJson) = await _cancellationPolicyRepository.GetApplicablePolicySnapshotAsync(
                CurrentBranchID,
                model.Source,
                "B2B",
                derivedRateType,
                model.CheckInDate);

            var bookingNumber = GenerateB2BBookingNumber();
            var createdBy = GetCurrentUserId();
            var totalAmount = Math.Round(quote.TotalRoomRate + quote.TotalTaxAmount, 2, MidpointRounding.AwayFromZero);

            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                Status = "Confirmed",
                PaymentStatus = model.DepositAmount >= quote.GrandTotal
                    ? "Paid"
                    : (model.DepositAmount > 0 ? "Partially Paid" : "Pending"),
                CustomerType = "B2B",
                Source = model.Source,
                Channel = model.Channel,
                RateType = derivedRateType,
                B2BClientId = client.Id,
                B2BClientCode = client.ClientCode,
                B2BClientName = client.ClientName,
                B2BAgreementId = agreement.Id,
                AgreementCode = agreement.AgreementCode,
                AgreementName = agreement.AgreementName,
                GstSlabId = applicableGstMasterId,
                GstSlabCode = gstSlab?.SlabCode,
                GstSlabName = gstSlab?.SlabName,
                CompanyContactPerson = client.ContactPerson,
                CompanyContactNo = client.ContactNo,
                CompanyEmail = client.CorporateEmail,
                CompanyGstNo = client.GstNo,
                BillingAddress = client.BillingAddressDisplay,
                BillingStateName = client.StateName,
                BillingPincode = client.Pincode,
                BillingType = agreement.BillingType,
                BillingTo = model.BillingTo,
                CreditDays = agreement.CreditDays,
                MealPlan = agreement.MealPlan,
                CorporateDiscountPercent = agreement.DiscountPercent,
                CompanyCreditLimit = client.CreditAmount ?? 0m,
                IsCreditAllowed = client.IsCreditAllowed,
                CancellationPolicyId = policyId,
                CancellationPolicySnapshot = snapshotJson,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                Nights = quote.Nights,
                RoomTypeId = model.RoomTypeId,
                RequiredRooms = model.RequiredRooms,
                RatePlanId = quote.RatePlanId,
                BaseAmount = quote.TotalRoomRate,
                TaxAmount = quote.TotalTaxAmount,
                CGSTAmount = quote.TotalCGSTAmount,
                SGSTAmount = quote.TotalSGSTAmount,
                DiscountAmount = quote.DiscountAmount,
                TotalAmount = totalAmount,
                DepositAmount = model.DepositAmount,
                BalanceAmount = totalAmount - model.DepositAmount,
                Adults = model.Adults,
                Children = model.Children,
                PrimaryGuestFirstName = model.PrimaryGuestFirstName,
                PrimaryGuestLastName = model.PrimaryGuestLastName,
                PrimaryGuestEmail = model.PrimaryGuestEmail ?? string.Empty,
                PrimaryGuestPhone = model.PrimaryGuestPhone,
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
                    Email = string.IsNullOrWhiteSpace(model.PrimaryGuestEmail) ? null : model.PrimaryGuestEmail.Trim(),
                    Phone = model.PrimaryGuestPhone,
                    GuestType = "Primary",
                    IsPrimary = true,
                    Address = client.BillingAddressDisplay,
                    State = client.StateName,
                    Pincode = client.Pincode
                }
            };

            var payments = new List<BookingPayment>();
            if (model.CollectAdvancePayment && model.DepositAmount > 0)
            {
                payments.Add(new BookingPayment
                {
                    Amount = model.DepositAmount,
                    PaymentMethod = model.AdvancePaymentMethod ?? string.Empty,
                    PaymentReference = string.IsNullOrWhiteSpace(model.AdvancePaymentReference) ? null : model.AdvancePaymentReference.Trim(),
                    Status = "Captured",
                    PaidOn = DateTime.Now,
                    Notes = "Advance collected during B2B booking creation.",
                    IsAdvancePayment = true
                });
            }

            var result = await _bookingRepository.CreateBookingAsync(booking, guests, payments, new List<BookingRoomNight>());
            TempData["SuccessMessage"] = $"B2B booking {result.BookingNumber} created successfully.";
            return RedirectToAction(nameof(Details), new { bookingNumber = result.BookingNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                return NotFound();
            }

            var booking = await _bookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null || booking.BranchID != CurrentBranchID || !IsB2BBooking(booking))
            {
                return NotFound();
            }

            ViewData["Title"] = "B2B Booking Details";
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetClientContext(int clientId)
        {
            var client = await _clientRepository.GetByIdAsync(clientId);
            if (client == null || client.BranchID != CurrentBranchID || !client.IsActive)
            {
                return Json(new { success = false, message = "Client not found." });
            }

            B2BAgreement? agreement = null;
            if (client.AgreementId.HasValue && client.AgreementId.Value > 0)
            {
                agreement = await _agreementRepository.GetByIdAsync(client.AgreementId.Value);
                if (agreement != null && (agreement.BranchID != CurrentBranchID || !agreement.IsActive))
                {
                    agreement = null;
                }
            }

            return Json(new
            {
                success = true,
                message = agreement == null ? "No active agreement is assigned to this client." : null,
                client = new
                {
                    id = client.Id,
                    code = client.ClientCode,
                    name = client.ClientName,
                    contactPerson = client.ContactPerson,
                    contactNo = client.ContactNo,
                    corporateEmail = client.CorporateEmail,
                    gstNo = client.GstNo,
                    address = client.BillingAddressDisplay,
                    stateName = client.StateName,
                    pincode = client.Pincode,
                    isCreditAllowed = client.IsCreditAllowed,
                    creditAmount = client.CreditAmount ?? 0m
                },
                agreement = agreement == null
                    ? null
                    : new
                    {
                        id = agreement.Id,
                        code = agreement.AgreementCode,
                        name = agreement.AgreementName,
                        billingType = agreement.BillingType,
                        creditDays = agreement.CreditDays,
                        ratePlanType = agreement.RatePlanType,
                        discountPercent = agreement.DiscountPercent,
                        mealPlan = agreement.MealPlan,
                        effectiveFrom = agreement.EffectiveFrom.ToString("yyyy-MM-dd"),
                        effectiveTo = agreement.EffectiveTo.ToString("yyyy-MM-dd")
                    }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuote(int roomTypeId, int? agreementId, string source, DateTime checkInDate, DateTime checkOutDate, int adults, int children, int requiredRooms)
        {
            if (roomTypeId <= 0 || string.IsNullOrWhiteSpace(source) || checkOutDate <= checkInDate)
            {
                return Json(new { success = false, message = "Enter valid stay details." });
            }

            if (!agreementId.HasValue || agreementId.Value <= 0)
            {
                return Json(new { success = false, message = "Select a B2B client with an assigned agreement first." });
            }

            B2BAgreement? agreement = null;
            if (agreementId.Value > 0)
            {
                agreement = await _agreementRepository.GetByIdAsync(agreementId.Value);
                if (agreement == null || agreement.BranchID != CurrentBranchID || !agreement.IsActive)
                {
                    return Json(new { success = false, message = "Select a valid active agreement." });
                }
            }

            var quote = await _bookingRepository.GetQuoteAsync(new BookingQuoteRequest
            {
                RoomTypeId = roomTypeId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                CustomerType = "B2B",
                Source = source,
                Adults = adults,
                Children = children,
                BranchID = CurrentBranchID,
                RequiredRooms = requiredRooms
            });

            if (quote == null)
            {
                return Json(new { success = false, message = "No B2B rate configured for the selected stay." });
            }

            var applicableGstMasterId = ResolveApplicableGstMasterId(agreement, roomTypeId, checkInDate);
            var gstSlab = await ResolveGstSlabAsync(GetPerNightTariff(quote, requiredRooms, quote.Nights), checkInDate, applicableGstMasterId);

            return Json(new
            {
                success = true,
                quote = new
                {
                    nights = quote.Nights,
                    baseAmount = quote.TotalRoomRate,
                    taxAmount = quote.TotalTaxAmount,
                    grandTotal = quote.GrandTotal,
                    discountAmount = quote.DiscountAmount,
                    taxPercentage = quote.TaxPercentage,
                    message = $"{quote.Nights} night(s) quoted for B2B stay."
                },
                gstSlab = gstSlab == null
                    ? null
                    : new
                    {
                        id = gstSlab.GstSlabId,
                        code = gstSlab.SlabCode,
                        name = gstSlab.SlabName,
                        gstPercent = gstSlab.GstPercent,
                        tariffFrom = gstSlab.TariffFrom,
                        tariffTo = gstSlab.TariffTo
                    }
            });
        }

        private async Task PopulateLookupsAsync(B2BBookingCreateViewModel model)
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.Clients = await _clientRepository.GetActiveByBranchAsync(CurrentBranchID);

            var rateSources = await _rateMasterRepository.GetSourcesAsync();
            var filteredSources = rateSources
                .Where(source => AllowedSources.Contains(source, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            ViewBag.Sources = filteredSources.Count > 0 ? filteredSources : AllowedSources.ToList();

            ViewBag.Channels = AllowedChannels;
            ViewBag.PaymentMethods = PaymentMethods;

            if (model.ClientId <= 0)
            {
                return;
            }

            var client = await _clientRepository.GetByIdAsync(model.ClientId);
            if (client == null || client.BranchID != CurrentBranchID)
            {
                return;
            }

            model.ClientCode = client.ClientCode;
            model.ClientName = client.ClientName;
            model.CompanyContactPerson = client.ContactPerson;
            model.CompanyContactNo = client.ContactNo;
            model.CompanyEmail = client.CorporateEmail;
            model.CompanyGstNo = client.GstNo;
            model.BillingAddress = client.BillingAddressDisplay;
            model.BillingStateName = client.StateName;
            model.BillingPincode = client.Pincode;
            model.CompanyCreditLimit = client.CreditAmount ?? 0m;
            model.IsCreditAllowed = client.IsCreditAllowed;

            if (!client.AgreementId.HasValue || client.AgreementId.Value <= 0)
            {
                model.AgreementId = 0;
                model.AgreementCode = string.Empty;
                model.AgreementName = string.Empty;
                model.BillingType = string.Empty;
                model.CreditDays = 0;
                model.MealPlan = string.Empty;
                return;
            }

            var agreement = await _agreementRepository.GetByIdAsync(client.AgreementId.Value);
            if (agreement == null || agreement.BranchID != CurrentBranchID || !agreement.IsActive)
            {
                model.AgreementId = 0;
                model.AgreementCode = string.Empty;
                model.AgreementName = string.Empty;
                model.BillingType = string.Empty;
                model.CreditDays = 0;
                model.MealPlan = string.Empty;
                return;
            }

            model.AgreementId = agreement.Id;
            model.AgreementCode = agreement.AgreementCode;
            model.AgreementName = agreement.AgreementName;
            model.BillingType = agreement.BillingType;
            model.CreditDays = agreement.CreditDays;
            model.MealPlan = agreement.MealPlan ?? string.Empty;
        }

        private static void Normalize(B2BBookingCreateViewModel model)
        {
            model.Source = (model.Source ?? string.Empty).Trim();
            model.Channel = (model.Channel ?? string.Empty).Trim();
            model.PrimaryGuestFirstName = (model.PrimaryGuestFirstName ?? string.Empty).Trim();
            model.PrimaryGuestLastName = (model.PrimaryGuestLastName ?? string.Empty).Trim();
            model.PrimaryGuestPhone = (model.PrimaryGuestPhone ?? string.Empty).Trim();
            model.PrimaryGuestEmail = string.IsNullOrWhiteSpace(model.PrimaryGuestEmail) ? null : model.PrimaryGuestEmail.Trim();
            model.AdvancePaymentMethod = string.IsNullOrWhiteSpace(model.AdvancePaymentMethod) ? null : model.AdvancePaymentMethod.Trim();
            model.AdvancePaymentReference = string.IsNullOrWhiteSpace(model.AdvancePaymentReference) ? null : model.AdvancePaymentReference.Trim();
            model.SpecialRequests = string.IsNullOrWhiteSpace(model.SpecialRequests) ? null : model.SpecialRequests.Trim();
        }

        private async Task<GstSlabBand?> ResolveGstSlabAsync(decimal perNightTariff, DateTime stayDate, int? gstSlabId)
        {
            return await _gstSlabRepository.ResolveBandAsync(perNightTariff, stayDate, gstSlabId);
        }

        private static int? ResolveApplicableGstMasterId(B2BAgreement? agreement, int roomTypeId, DateTime stayDate)
        {
            if (agreement == null)
            {
                return null;
            }

            var roomRateGst = agreement.RoomRates?
                .Where(row => row.IsActive
                    && row.RoomTypeId == roomTypeId
                    && row.ValidFrom.Date <= stayDate.Date
                    && row.ValidTo.Date >= stayDate.Date
                    && row.GstSlabId.HasValue)
                .OrderByDescending(row => row.ValidFrom)
                .Select(row => row.GstSlabId)
                .FirstOrDefault();

            return roomRateGst ?? agreement.GstSlabId;
        }

        private static decimal GetPerNightTariff(BookingQuoteResult quote, B2BBookingCreateViewModel model)
        {
            return GetPerNightTariff(quote, model.RequiredRooms, quote.Nights);
        }

        private static decimal GetPerNightTariff(BookingQuoteResult quote, int requiredRooms, int nights)
        {
            var safeRooms = Math.Max(1, requiredRooms);
            var safeNights = Math.Max(1, nights);
            return Math.Round(quote.TotalRoomRate / (safeRooms * safeNights), 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsB2BBooking(Booking booking)
        {
            return string.Equals(booking.CustomerType, "B2B", StringComparison.OrdinalIgnoreCase);
        }

        private static string GenerateB2BBookingNumber()
        {
            return $"B2B-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }
    }
}