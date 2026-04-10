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
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string? statusFilter)
        {
            // Redirect to the standard Booking List with B2B filter — same UI, same flows
            return RedirectToAction("List", "Booking", new { fromDate, toDate, statusFilter, customerType = "B2B" });
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

            // Validate that at least one room type line was selected
            var selectedLines = model.RoomLines?.Where(l => l.RoomTypeId > 0 && l.RequiredRooms > 0).ToList()
                                ?? new List<B2BRoomLineItem>();
            if (!selectedLines.Any())
            {
                ModelState.AddModelError(string.Empty, "Please select at least one room type and enter the number of rooms required.");
                return View(model);
            }

            // Process each selected room type: compute quote, check capacity, resolve GST
            var roomLineEntities = new List<B2BBookingRoomLine>();
            var allRoomNights    = new List<BookingRoomNight>();
            decimal grandBase = 0, grandTax = 0, grandTotal = 0, grandDiscount = 0;
            int totalRooms = 0, totalAdults = 0, totalChildren = 0;
            int primaryRoomTypeId = selectedLines[0].RoomTypeId;
            int? primaryGstMasterId = null;
            GstSlabBand? primaryGstSlab = null;
            DateTime globalCheckIn = DateTime.MaxValue, globalCheckOut = DateTime.MinValue;

            foreach (var line in selectedLines)
            {
                // Per-line dates fall back to global dates from the form
                var lineCheckIn  = line.CheckInDate?.Date  ?? model.CheckInDate.Date;
                var lineCheckOut = line.CheckOutDate?.Date ?? model.CheckOutDate.Date;
                if (lineCheckOut <= lineCheckIn)
                {
                    lineCheckIn  = model.CheckInDate.Date;
                    lineCheckOut = model.CheckOutDate.Date;
                }
                if (lineCheckIn < globalCheckIn)   globalCheckIn  = lineCheckIn;
                if (lineCheckOut > globalCheckOut)  globalCheckOut = lineCheckOut;

                var (computed, lineErr) = ComputeB2BAgreementQuote(agreement, line.RoomTypeId,
                    lineCheckIn, lineCheckOut, line.RequiredRooms);
                if (computed == null)
                {
                    ModelState.AddModelError(string.Empty,
                        lineErr ?? $"No active room rate found for room type '{line.RoomTypeName}' and the selected dates.");
                    return View(model);
                }

                totalRooms    += line.RequiredRooms;
                totalAdults   += Math.Max(1, line.Adults);
                totalChildren += line.Children;

                var lineCapacity = await _bookingRepository.CheckRoomCapacityAvailabilityAsync(
                    line.RoomTypeId, CurrentBranchID, lineCheckIn, lineCheckOut, line.RequiredRooms);
                if (!lineCapacity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Not enough inventory for '{line.RoomTypeName}' ({line.RequiredRooms} room(s)) on the selected dates.");
                    return View(model);
                }

                var lineGstMasterId = ResolveApplicableGstMasterId(agreement, line.RoomTypeId, lineCheckIn);
                var lineGstSlab    = await ResolveGstSlabAsync(computed.RateAfterDiscount, lineCheckIn, lineGstMasterId);
                decimal lineGstPct = lineGstSlab?.GstPercent ?? 0m;
                decimal lineTax    = Math.Round(computed.TotalBase * (lineGstPct / 100m), 2, MidpointRounding.AwayFromZero);
                decimal lineGrand  = computed.TotalBase + lineTax;

                grandBase     += computed.TotalBase;
                grandTax      += lineTax;
                grandDiscount += computed.TotalDiscount;
                grandTotal    += lineGrand;

                if (line.RoomTypeId == primaryRoomTypeId)
                {
                    primaryGstMasterId = lineGstMasterId;
                    primaryGstSlab     = lineGstSlab;
                }

                // Resolve meal plan from agreement for this room type
                var lineMealPlan = !string.IsNullOrWhiteSpace(line.MealPlan) ? line.MealPlan
                    : agreement.RoomRates?.FirstOrDefault(r => r.IsActive && r.RoomTypeId == line.RoomTypeId)?.MealPlan
                    ?? agreement.MealPlan;

                roomLineEntities.Add(new B2BBookingRoomLine
                {
                    RoomTypeId    = line.RoomTypeId,
                    RoomTypeName  = line.RoomTypeName,
                    RequiredRooms = line.RequiredRooms,
                    RatePerNight  = computed.RateAfterDiscount,
                    Nights        = computed.Nights,
                    BaseAmount    = computed.TotalBase,
                    TaxAmount     = lineTax,
                    GrandTotal    = lineGrand,
                    CheckInDate   = lineCheckIn,
                    CheckOutDate  = lineCheckOut,
                    Adults        = Math.Max(1, line.Adults),
                    Children      = line.Children,
                    MealPlan      = lineMealPlan
                });

                // Generate BookingRoomNights for each stay date × each room (for calendar/availability)
                int lineNights = computed.Nights;
                decimal nightRate = computed.RateAfterDiscount;
                decimal nightTax  = lineNights > 0 && line.RequiredRooms > 0
                    ? Math.Round(lineTax / (lineNights * line.RequiredRooms), 2, MidpointRounding.AwayFromZero)
                    : 0m;
                decimal nightCGST = Math.Round(nightTax / 2, 2, MidpointRounding.AwayFromZero);
                decimal nightSGST = nightTax - nightCGST;

                for (int d = 0; d < lineNights; d++)
                {
                    var stayDate = lineCheckIn.AddDays(d);
                    for (int r = 0; r < line.RequiredRooms; r++)
                    {
                        allRoomNights.Add(new BookingRoomNight
                        {
                            RoomId         = null,
                            StayDate       = stayDate,
                            RateAmount     = nightRate,
                            ActualBaseRate = nightRate,
                            DiscountAmount = 0m,
                            TaxAmount      = nightTax,
                            CGSTAmount     = nightCGST,
                            SGSTAmount     = nightSGST,
                            Status         = "Reserved"
                        });
                    }
                }
            }

            grandBase     = Math.Round(grandBase,     2, MidpointRounding.AwayFromZero);
            grandTax      = Math.Round(grandTax,      2, MidpointRounding.AwayFromZero);
            grandDiscount = Math.Round(grandDiscount, 2, MidpointRounding.AwayFromZero);
            grandTotal    = Math.Round(grandTotal,    2, MidpointRounding.AwayFromZero);

            int nights = Math.Max(1, (globalCheckOut.Date - globalCheckIn.Date).Days);

            model.QuotedBaseAmount = grandBase;
            model.QuotedTaxAmount  = grandTax;
            model.QuotedGrandTotal = grandTotal;
            model.QuoteMessage     = $"Rate locked for {nights} night(s) — {selectedLines.Count} room type(s)";


            model.GstSlabId   = primaryGstMasterId;
            model.GstSlabCode = primaryGstSlab?.SlabCode;
            model.GstSlabName = primaryGstSlab == null
                ? null
                : $"{primaryGstSlab.SlabName} | {primaryGstSlab.TariffFrom:N2} - {(primaryGstSlab.TariffTo.HasValue ? primaryGstSlab.TariffTo.Value.ToString("N2") : "Open")}";

            var derivedRateType = agreement.DiscountPercent > 0 ? "Discounted" : "Standard";
            var (policyId, snapshotJson) = await _cancellationPolicyRepository.GetApplicablePolicySnapshotAsync(
                CurrentBranchID,
                model.Source,
                "B2B",
                derivedRateType,
                model.CheckInDate);

            var bookingNumber = GenerateB2BBookingNumber();
            var createdBy = GetCurrentUserId();

            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                Status = "Confirmed",
                PaymentStatus = "Pending",
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
                GstSlabId = primaryGstMasterId,
                GstSlabCode = primaryGstSlab?.SlabCode,
                GstSlabName = primaryGstSlab?.SlabName,
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
                CheckInDate = globalCheckIn,
                CheckOutDate = globalCheckOut,
                Nights = nights,
                RoomTypeId = primaryRoomTypeId,
                RequiredRooms = totalRooms,
                RatePlanId = null,
                BaseAmount = grandBase,
                TaxAmount = grandTax,
                CGSTAmount = Math.Round(grandTax / 2, 2, MidpointRounding.AwayFromZero),
                SGSTAmount = Math.Round(grandTax / 2, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = grandDiscount,
                TotalAmount = grandTotal,
                DepositAmount = 0m,
                BalanceAmount = grandTotal,
                Adults = totalAdults,
                Children = totalChildren,
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

            // Generate unique B2B invoice number: INV/{FY}/{5-digit seq}
            booking.InvoiceNumber = await _bookingRepository.GenerateInvoiceNumberAsync(CurrentBranchID);

            var result = await _bookingRepository.CreateBookingAsync(booking, guests, payments, allRoomNights);
            await _bookingRepository.SaveB2BRoomLinesAsync(result.BookingId, roomLineEntities);
            TempData["SuccessMessage"] = $"B2B booking {result.BookingNumber} created successfully.";

            // If advance payment requested, redirect to standard Details page (which has payment modal)
            if (model.CollectAdvancePayment)
            {
                TempData["BookingCreated"] = "true";
                TempData["BookingNumber"] = result.BookingNumber;
                TempData["BookingAmount"] = booking.TotalAmount.ToString("N2");
                TempData["ShowAdvancePaymentModal"] = "true";
                return RedirectToAction("Details", "Booking", new { bookingNumber = result.BookingNumber });
            }

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
                    },
                roomTypes = agreement == null
                    ? Array.Empty<object>()
                    : agreement.RoomRates
                        .Where(r => r.IsActive)
                        .GroupBy(r => r.RoomTypeId)
                        .Select(g =>
                        {
                            // Prefer the rate row whose validity window includes today
                            var today = DateTime.Today;
                            var row = g.FirstOrDefault(r => r.ValidFrom.Date <= today && r.ValidTo.Date >= today)
                                      ?? g.OrderByDescending(r => r.ValidFrom).First();
                            return (object)new
                            {
                                id = g.Key,
                                name = row.RoomTypeName,
                                ratePerNight = row.ContractRate,
                                mealPlan = row.MealPlan,
                                season = row.SeasonLabel
                            };
                        })
                        .OrderBy(r => ((dynamic)r).name)
                        .ToArray()
            });
        }

        // DTO for multi-room-type quote request body
        public sealed class QuoteLineDto
        {
            public int RoomTypeId { get; set; }
            public int RequiredRooms { get; set; } = 1;
            public DateTime? CheckInDate { get; set; }
            public DateTime? CheckOutDate { get; set; }
        }

        public sealed class MultiRoomQuoteRequest
        {
            public int AgreementId { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public List<QuoteLineDto> Lines { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> GetQuote([FromBody] MultiRoomQuoteRequest request)
        {
            if (request == null || request.CheckOutDate <= request.CheckInDate)
                return Json(new { success = false, message = "Enter valid check-in and check-out dates." });

            if (request.AgreementId <= 0)
                return Json(new { success = false, message = "Select a B2B client with an assigned agreement first." });

            var validLines = request.Lines?.Where(l => l.RoomTypeId > 0 && l.RequiredRooms > 0).ToList();
            if (validLines == null || validLines.Count == 0)
                return Json(new { success = false, message = "Select at least one room type with a quantity of 1 or more." });

            var agreement = await _agreementRepository.GetByIdAsync(request.AgreementId);
            if (agreement == null || agreement.BranchID != CurrentBranchID || !agreement.IsActive)
                return Json(new { success = false, message = "The selected agreement is invalid or inactive." });

            var lineResults = new List<object>();
            decimal grandBase = 0, grandTax = 0, grandTotal = 0;
            var availabilityIssues = new List<string>();

            foreach (var line in validLines)
            {
                // Per-line dates fall back to the global request dates
                var lineCheckIn  = line.CheckInDate?.Date  ?? request.CheckInDate.Date;
                var lineCheckOut = line.CheckOutDate?.Date ?? request.CheckOutDate.Date;
                if (lineCheckOut <= lineCheckIn)
                {
                    lineCheckIn  = request.CheckInDate.Date;
                    lineCheckOut = request.CheckOutDate.Date;
                }

                var (computed, err) = ComputeB2BAgreementQuote(agreement, line.RoomTypeId, lineCheckIn, lineCheckOut, line.RequiredRooms);
                if (computed == null)
                    return Json(new { success = false, message = err });

                var gstMasterId = ResolveApplicableGstMasterId(agreement, line.RoomTypeId, lineCheckIn);
                var gstSlab = await ResolveGstSlabAsync(computed.RateAfterDiscount, lineCheckIn, gstMasterId);
                decimal gstPct = gstSlab?.GstPercent ?? 0m;
                decimal lineTax = Math.Round(computed.TotalBase * (gstPct / 100m), 2, MidpointRounding.AwayFromZero);
                decimal lineGrand = computed.TotalBase + lineTax;

                var hasCapacity = await _bookingRepository.CheckRoomCapacityAvailabilityAsync(
                    line.RoomTypeId, CurrentBranchID, lineCheckIn, lineCheckOut, line.RequiredRooms);

                var roomTypeName = agreement.RoomRates
                    .FirstOrDefault(r => r.RoomTypeId == line.RoomTypeId)?.RoomTypeName ?? $"Room Type #{line.RoomTypeId}";

                if (!hasCapacity)
                    availabilityIssues.Add($"{roomTypeName} ({line.RequiredRooms} room(s))");

                grandBase  += computed.TotalBase;
                grandTax   += lineTax;
                grandTotal += lineGrand;

                lineResults.Add(new
                {
                    roomTypeId   = line.RoomTypeId,
                    roomTypeName,
                    requiredRooms = line.RequiredRooms,
                    nights        = computed.Nights,
                    ratePerNight  = computed.RateAfterDiscount,
                    baseAmount    = computed.TotalBase,
                    taxAmount     = lineTax,
                    grandTotal    = lineGrand,
                    available     = hasCapacity,
                    gstSlabName   = gstSlab?.SlabName,
                    checkInDate   = lineCheckIn.ToString("yyyy-MM-dd"),
                    checkOutDate  = lineCheckOut.ToString("yyyy-MM-dd")
                });
            }

            bool allAvailable = availabilityIssues.Count == 0;
            string availMsg = allAvailable
                ? $"All rooms available for {validLines.Sum(l => l.RequiredRooms)} room(s) over {(request.CheckOutDate.Date - request.CheckInDate.Date).Days} night(s)."
                : $"Insufficient inventory for: {string.Join(", ", availabilityIssues)}.";

            return Json(new
            {
                success = true,
                roomsAvailable = allAvailable,
                availabilityMessage = availMsg,
                lines = lineResults,
                totals = new
                {
                    baseAmount = Math.Round(grandBase,  2, MidpointRounding.AwayFromZero),
                    taxAmount  = Math.Round(grandTax,   2, MidpointRounding.AwayFromZero),
                    grandTotal = Math.Round(grandTotal, 2, MidpointRounding.AwayFromZero),
                    nights     = (request.CheckOutDate.Date - request.CheckInDate.Date).Days
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

        private sealed record B2BAgreementQuoteResult(
            int Nights,
            decimal RateAfterDiscount,
            decimal TotalBase,
            decimal TotalDiscount);

        private static (B2BAgreementQuoteResult? result, string? error) ComputeB2BAgreementQuote(
            B2BAgreement agreement, int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int requiredRooms)
        {
            var rate = agreement.RoomRates?
                .Where(r => r.IsActive && r.RoomTypeId == roomTypeId
                    && r.ValidFrom.Date <= checkInDate.Date && r.ValidTo.Date >= checkOutDate.Date.AddDays(-1))
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault()
                ?? agreement.RoomRates?
                .Where(r => r.IsActive && r.RoomTypeId == roomTypeId
                    && r.ValidFrom.Date <= checkInDate.Date && r.ValidTo.Date >= checkInDate.Date)
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault();

            if (rate == null)
                return (null, $"No active room rate found in the agreement for the selected room type and dates ({checkInDate:dd MMM} – {checkOutDate:dd MMM}).");

            int nights = Math.Max(1, (checkOutDate.Date - checkInDate.Date).Days);
            int rooms = Math.Max(1, requiredRooms);
            decimal discountPct = string.Equals(agreement.RatePlanType, "Discount", StringComparison.OrdinalIgnoreCase)
                ? agreement.DiscountPercent : 0m;
            decimal discountPerNight = Math.Round(rate.ContractRate * (discountPct / 100m), 2, MidpointRounding.AwayFromZero);
            decimal rateAfterDiscount = rate.ContractRate - discountPerNight;
            decimal totalBase = Math.Round(rateAfterDiscount * nights * rooms, 2, MidpointRounding.AwayFromZero);
            decimal totalDiscount = Math.Round(discountPerNight * nights * rooms, 2, MidpointRounding.AwayFromZero);
            return (new B2BAgreementQuoteResult(nights, rateAfterDiscount, totalBase, totalDiscount), null);
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