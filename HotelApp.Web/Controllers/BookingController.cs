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
        private readonly ILocationRepository _locationRepository;

        public BookingController(
            IBookingRepository bookingRepository,
            IRoomRepository roomRepository,
            IGuestRepository guestRepository,
            IBankRepository bankRepository,
            IHotelSettingsRepository hotelSettingsRepository,
            ILocationRepository locationRepository)
        {
            _bookingRepository = bookingRepository;
            _roomRepository = roomRepository;
            _guestRepository = guestRepository;
            _bankRepository = bankRepository;
            _hotelSettingsRepository = hotelSettingsRepository;
            _locationRepository = locationRepository;
        }

        public async Task<IActionResult> List(DateTime? fromDate, DateTime? toDate, string? statusFilter)
        {
            var isUpcomingFilter = string.Equals(statusFilter, "upcoming", StringComparison.OrdinalIgnoreCase);

            // Default to last 3 days if no dates provided (but NOT for Upcoming filter)
            if (!isUpcomingFilter && !fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = DateTime.Today.AddDays(-3);
                toDate = DateTime.Today;
            }

            // For Upcoming filter, ignore date range completely.
            // Fetch a broad set and apply an explicit upcoming predicate.
            var bookings = isUpcomingFilter
                ? await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, null, null)
                : await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, fromDate, toDate);

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bookings = statusFilter.ToLower() switch
                {
                    "upcoming" => bookings.Where(b =>
                        (b.Status == null || b.Status.ToLower() != "cancelled")
                        && !b.ActualCheckInDate.HasValue
                        && b.CheckInDate > DateTime.Now),
                    "assigned" => bookings.Where(b => b.Room != null || (b.AssignedRooms != null && b.AssignedRooms.Any())),
                    "notassigned" => bookings.Where(b => b.Room == null && (b.AssignedRooms == null || !b.AssignedRooms.Any())),
                    "checkedin" => bookings.Where(b => b.ActualCheckInDate.HasValue && !b.ActualCheckOutDate.HasValue),
                    "checkedout" => bookings.Where(b => b.ActualCheckOutDate.HasValue),
                    "cancelled" => bookings.Where(b => b.Status?.ToLower() == "cancelled"),
                    "due" => bookings.Where(b => b.BalanceAmount > 0),
                    "fullypaid" => bookings.Where(b => b.BalanceAmount <= 0 || b.PaymentStatus?.ToLower() == "paid"),
                    _ => bookings
                };
            }

            var viewModel = new BookingDashboardViewModel
            {
                TodayBookingCount = await _bookingRepository.GetTodayBookingCountAsync(),
                TodayAdvanceAmount = await _bookingRepository.GetTodayAdvanceAmountAsync(),
                TodayCheckInCount = await _bookingRepository.GetTodayCheckInCountAsync(),
                TodayCheckOutCount = await _bookingRepository.GetTodayCheckOutCountAsync(),
                FromDate = fromDate,
                ToDate = toDate,
                StatusFilter = statusFilter,
                Bookings = bookings
            };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult PaymentDashboard(DateTime? fromDate, DateTime? toDate)
        {
            // Default to today
            var from = (fromDate ?? DateTime.Today).Date;
            var to = (toDate ?? DateTime.Today).Date;

            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.FromDateDisplay = from.ToString("dd/MM/yyyy");
            ViewBag.ToDateDisplay = to.ToString("dd/MM/yyyy");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentDashboardData(DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_bookingRepository.ConnectionString);
                await connection.OpenAsync();

                var command = new Microsoft.Data.SqlClient.SqlCommand("sp_GetPaymentDashboardData", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@BranchID", CurrentBranchID);
                command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                command.Parameters.AddWithValue("@ToDate", toDate.Date);

                decimal totalCollection = 0m;
                decimal totalGst = 0m;
                int totalPaymentCount = 0;
                decimal totalDueAmount = 0m;

                var paymentMethods = new List<object>();
                var payments = new List<object>();

                using var reader = await command.ExecuteReaderAsync();

                // 1) Summary
                if (await reader.ReadAsync())
                {
                    totalCollection = reader.GetDecimal(reader.GetOrdinal("TotalPayments"));
                    totalGst = reader.GetDecimal(reader.GetOrdinal("TotalGST"));
                    totalPaymentCount = reader.GetInt32(reader.GetOrdinal("PaymentCount"));
                }

                // 2) Payment methods
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        paymentMethods.Add(new
                        {
                            paymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? "Unknown" : reader.GetString(reader.GetOrdinal("PaymentMethod")),
                            totalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                            transactionCount = reader.GetInt32(reader.GetOrdinal("TransactionCount"))
                        });
                    }
                }

                // 3) Payment details
                var bookingDueMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var bookingNumber = reader.IsDBNull(reader.GetOrdinal("BookingNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("BookingNumber"));
                        var receiptNo = reader.IsDBNull(reader.GetOrdinal("ReceiptNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("ReceiptNumber"));
                        var amount = reader.GetDecimal(reader.GetOrdinal("Amount"));

                        // Allocate GST proportionally to each payment amount.
                        decimal gstAmount = 0m;
                        if (totalCollection > 0)
                        {
                            gstAmount = Math.Round((amount / totalCollection) * totalGst, 2);
                        }

                        // Track due (balance) per booking from the result set.
                        var bookingBalanceOrdinal = reader.GetOrdinal("BookingBalance");
                        var bookingBalance = reader.IsDBNull(bookingBalanceOrdinal) ? 0m : reader.GetDecimal(bookingBalanceOrdinal);
                        if (!string.IsNullOrWhiteSpace(bookingNumber) && !bookingDueMap.ContainsKey(bookingNumber))
                        {
                            bookingDueMap[bookingNumber] = bookingBalance;
                        }

                        var createdByOrdinal = reader.GetOrdinal("CreatedBy");
                        var createdBy = reader.IsDBNull(createdByOrdinal) ? string.Empty : reader.GetString(createdByOrdinal);

                        payments.Add(new
                        {
                            receiptNo,
                            bookingNo = bookingNumber,
                            receiptAmount = amount,
                            gstAmount,
                            createdBy
                        });
                    }
                }

                // Due amount (sum of booking balances from the filtered result set)
                totalDueAmount = bookingDueMap.Values.Where(v => v > 0).Sum();

                return Json(new
                {
                    summary = new
                    {
                        totalCollection,
                        totalGst,
                        totalPaymentCount,
                        totalDueAmount
                    },
                    paymentMethods,
                    payments
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> ExportToExcel(DateTime? fromDate, DateTime? toDate, string? statusFilter)
        {
            var isUpcomingFilter = string.Equals(statusFilter, "upcoming", StringComparison.OrdinalIgnoreCase);

            // Default to last 3 days if no dates provided (but NOT for Upcoming filter)
            if (!isUpcomingFilter && !fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = DateTime.Today.AddDays(-3);
                toDate = DateTime.Today;
            }

            var bookings = isUpcomingFilter
                ? await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, null, null)
                : await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, fromDate, toDate);

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bookings = statusFilter.ToLower() switch
                {
                    "upcoming" => bookings.Where(b =>
                        (b.Status == null || b.Status.ToLower() != "cancelled")
                        && !b.ActualCheckInDate.HasValue
                        && b.CheckInDate > DateTime.Now),
                    "assigned" => bookings.Where(b => b.Room != null || (b.AssignedRooms != null && b.AssignedRooms.Any())),
                    "notassigned" => bookings.Where(b => b.Room == null && (b.AssignedRooms == null || !b.AssignedRooms.Any())),
                    "checkedin" => bookings.Where(b => b.ActualCheckInDate.HasValue && !b.ActualCheckOutDate.HasValue),
                    "checkedout" => bookings.Where(b => b.ActualCheckOutDate.HasValue),
                    "cancelled" => bookings.Where(b => b.Status?.ToLower() == "cancelled"),
                    "due" => bookings.Where(b => b.BalanceAmount > 0),
                    "fullypaid" => bookings.Where(b => b.BalanceAmount <= 0 || b.PaymentStatus?.ToLower() == "paid"),
                    _ => bookings
                };
            }

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Booking Number,Guest Name,Phone,Check-In Date,Check-Out Date,Nights,Room Type,Room Number,Total Amount,Deposit,Balance,Status,Payment Status,Source,Channel,Created Date");

            foreach (var booking in bookings)
            {
                var guestName = $"{booking.PrimaryGuestFirstName} {booking.PrimaryGuestLastName}";
                var roomNumber = booking.Room?.RoomNumber ?? "Not Assigned";
                if (booking.RequiredRooms > 1)
                {
                    roomNumber += $" (+{booking.RequiredRooms - 1} more)";
                }
                var balance = booking.TotalAmount - booking.DepositAmount;

                csv.AppendLine($"\"{booking.BookingNumber}\",\"{guestName}\",\"{booking.PrimaryGuestPhone}\"," +
                    $"\"{booking.CheckInDate:dd-MMM-yyyy}\",\"{booking.CheckOutDate:dd-MMM-yyyy}\"," +
                    $"\"{booking.Nights}\",\"{booking.RoomType?.TypeName}\",\"{roomNumber}\"," +
                    $"\"{booking.TotalAmount}\",\"{booking.DepositAmount}\",\"{balance}\"," +
                    $"\"{booking.Status}\",\"{booking.PaymentStatus}\",\"{booking.Source}\",\"{booking.Channel}\"," +
                    $"\"{booking.CreatedDate:dd-MMM-yyyy HH:mm}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Bookings_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> ExportToPDF(DateTime? fromDate, DateTime? toDate, string? statusFilter)
        {
            var isUpcomingFilter = string.Equals(statusFilter, "upcoming", StringComparison.OrdinalIgnoreCase);

            // Default to last 3 days if no dates provided (but NOT for Upcoming filter)
            if (!isUpcomingFilter && !fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = DateTime.Today.AddDays(-3);
                toDate = DateTime.Today;
            }

            var bookings = isUpcomingFilter
                ? await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, null, null)
                : await _bookingRepository.GetByBranchAndDateRangeAsync(CurrentBranchID, fromDate, toDate);

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                bookings = statusFilter.ToLower() switch
                {
                    "upcoming" => bookings.Where(b =>
                        (b.Status == null || b.Status.ToLower() != "cancelled")
                        && !b.ActualCheckInDate.HasValue
                        && b.CheckInDate > DateTime.Now),
                    "assigned" => bookings.Where(b => b.Room != null || (b.AssignedRooms != null && b.AssignedRooms.Any())),
                    "notassigned" => bookings.Where(b => b.Room == null && (b.AssignedRooms == null || !b.AssignedRooms.Any())),
                    "checkedin" => bookings.Where(b => b.ActualCheckInDate.HasValue && !b.ActualCheckOutDate.HasValue),
                    "checkedout" => bookings.Where(b => b.ActualCheckOutDate.HasValue),
                    "cancelled" => bookings.Where(b => b.Status?.ToLower() == "cancelled"),
                    "due" => bookings.Where(b => b.BalanceAmount > 0),
                    "fullypaid" => bookings.Where(b => b.BalanceAmount <= 0 || b.PaymentStatus?.ToLower() == "paid"),
                    _ => bookings
                };
            }

            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #333; text-align: center; margin-bottom: 30px; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 20px; }");
            html.AppendLine(".date-range { text-align: center; color: #666; margin-bottom: 20px; font-size: 14px; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; font-size: 11px; }");
            html.AppendLine("th { background-color: #667eea; color: white; padding: 10px; text-align: left; font-weight: bold; }");
            html.AppendLine("td { padding: 8px; border-bottom: 1px solid #ddd; }");
            html.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
            html.AppendLine(".text-right { text-align: right; }");
            html.AppendLine(".status-confirmed { color: #10b981; font-weight: bold; }");
            html.AppendLine(".status-checkedin { color: #0d6efd; font-weight: bold; }");
            html.AppendLine(".status-cancelled { color: #ef4444; font-weight: bold; }");
            html.AppendLine(".footer { margin-top: 30px; text-align: center; color: #666; font-size: 10px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Bookings Report</h1>");
            if (fromDate.HasValue || toDate.HasValue)
            {
                var dateRange = fromDate.HasValue && toDate.HasValue
                    ? $"{fromDate.Value:dd MMM yyyy} to {toDate.Value:dd MMM yyyy}"
                    : fromDate.HasValue
                        ? $"From {fromDate.Value:dd MMM yyyy}"
                        : $"Until {toDate.Value:dd MMM yyyy}";
                html.AppendLine($"<div class='date-range'>Date Range: {dateRange}</div>");
            }
            html.AppendLine($"<div class='date-range'>Generated: {DateTime.Now:dd MMM yyyy HH:mm}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th>Booking #</th><th>Guest</th><th>Phone</th><th>Check-In</th><th>Check-Out</th>");
            html.AppendLine("<th>Room Type</th><th>Room</th><th class='text-right'>Amount</th><th>Status</th>");
            html.AppendLine("</tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var booking in bookings)
            {
                var guestName = $"{booking.PrimaryGuestFirstName} {booking.PrimaryGuestLastName}";
                var roomNumber = booking.Room?.RoomNumber ?? "Not Assigned";
                if (booking.RequiredRooms > 1)
                {
                    roomNumber += $" (+{booking.RequiredRooms - 1})";
                }
                var statusClass = booking.Status?.ToLower() == "confirmed" ? "status-confirmed"
                    : booking.Status?.ToLower() == "checkedin" ? "status-checkedin"
                    : booking.Status?.ToLower() == "cancelled" ? "status-cancelled" : "";

                html.AppendLine("<tr>");
                html.AppendLine($"<td>{booking.BookingNumber}</td>");
                html.AppendLine($"<td>{guestName}</td>");
                html.AppendLine($"<td>{booking.PrimaryGuestPhone}</td>");
                html.AppendLine($"<td>{booking.CheckInDate:dd-MMM-yyyy}</td>");
                html.AppendLine($"<td>{booking.CheckOutDate:dd-MMM-yyyy}</td>");
                html.AppendLine($"<td>{booking.RoomType?.TypeName}</td>");
                html.AppendLine($"<td>{roomNumber}</td>");
                html.AppendLine($"<td class='text-right'>₹{booking.TotalAmount:N0}</td>");
                html.AppendLine($"<td class='{statusClass}'>{booking.Status}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody></table>");
            html.AppendLine($"<div class='footer'>Total Bookings: {bookings.Count()} | LuxStay Hotel Management System</div>");
            html.AppendLine("</body></html>");

            var bytes = System.Text.Encoding.UTF8.GetBytes(html.ToString());
            var fileName = $"Bookings_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            return File(bytes, "text/html", fileName);
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

            // Get rate master to determine actual tax percentage
            if (booking.RatePlanId.HasValue)
            {
                var (taxPercentage, cgstPercentage, sgstPercentage) = await _bookingRepository.GetRateMasterTaxPercentagesAsync(booking.RatePlanId.Value);
                ViewBag.TaxPercentage = taxPercentage;
                ViewBag.CGSTPercentage = cgstPercentage;
                ViewBag.SGSTPercentage = sgstPercentage;
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(string bookingNumber)
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

            // Get hotel settings
            var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);
            ViewBag.HotelSettings = hotelSettings;

            // Get payments for this booking
            var payments = await _bookingRepository.GetPaymentsAsync(booking.Id);
            ViewBag.Payments = payments;

            // Get assigned room numbers
            var assignedRooms = await _bookingRepository.GetAssignedRoomNumbersAsync(booking.Id);
            ViewBag.AssignedRooms = assignedRooms;

            // Get rate master to determine actual tax percentage (not recalculated from amounts)
            if (booking.RatePlanId.HasValue)
            {
                var (taxPercentage, cgstPercentage, sgstPercentage) = await _bookingRepository.GetRateMasterTaxPercentagesAsync(booking.RatePlanId.Value);
                ViewBag.TaxPercentage = taxPercentage;
                ViewBag.CGSTPercentage = cgstPercentage;
                ViewBag.SGSTPercentage = sgstPercentage;
            }

            return View(booking);
        }

        [HttpGet]
        public IActionResult RoomAvailabilityCalendar()
        {
            ViewData["Title"] = "Room Availability";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomTypeRate(int roomTypeId, string customerType, string source, DateTime? checkInDate = null, DateTime? checkOutDate = null)
        {
            try
            {
                var roomType = await _roomRepository.GetRoomTypeByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    return Json(new { success = false, message = "Room type not found" });
                }

                // Use provided dates or default to today
                var effectiveCheckIn = checkInDate ?? DateTime.Today;
                var effectiveCheckOut = checkOutDate ?? DateTime.Today.AddDays(1);

                // Try to get the rate from RateMaster
                var quoteRequest = new BookingQuoteRequest
                {
                    RoomTypeId = roomTypeId,
                    CheckInDate = effectiveCheckIn,
                    CheckOutDate = effectiveCheckOut,
                    CustomerType = customerType,
                    Source = source,
                    Adults = 2,
                    Children = 0,
                    BranchID = CurrentBranchID,
                    RequiredRooms = 1 // Show per-room rate
                };

                var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);
                
                if (quote != null)
                {
                    return Json(new { 
                        success = true, 
                        rate = quote.BaseRatePerNight,
                        formattedRate = $"₹{quote.BaseRatePerNight:N0}",
                        taxPercentage = quote.TaxPercentage,
                        hasRate = true
                    });
                }
                else
                {
                    // No rate found
                    return Json(new { 
                        success = true, 
                        rate = 0,
                        formattedRate = "Rate not configured",
                        hasRate = false,
                        message = $"No rate configured for {customerType} - {source}"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
                Channel = Channels.First()
            };

            await PopulateLookupsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            await PopulateLookupsAsync(model);

            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out date must be after check-in date.");
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
                BranchID = CurrentBranchID,
                RequiredRooms = model.RequiredRooms
            };

            var quote = await _bookingRepository.GetQuoteAsync(quoteRequest);
            if (quote == null)
            {
                var roomType = await _roomRepository.GetRoomTypeByIdAsync(model.RoomTypeId);
                var roomTypeName = roomType?.TypeName ?? "selected room type";
                ModelState.AddModelError(string.Empty, 
                    $"No active rate configured for {roomTypeName} with Customer Type: {model.CustomerType}, Source: {model.Source}. " +
                    $"Please configure the rate in Rate Master or select a different combination.");
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

            // Check room capacity availability (supports multi-room bookings)
            var hasCapacity = await _bookingRepository.CheckRoomCapacityAvailabilityAsync(
                model.RoomTypeId, 
                CurrentBranchID, 
                model.CheckInDate, 
                model.CheckOutDate, 
                model.RequiredRooms
            );
            
            if (!hasCapacity)
            {
                var roomType = await _roomRepository.GetRoomTypeByIdAsync(model.RoomTypeId);
                var roomTypeName = roomType?.TypeName ?? "selected room type";
                ModelState.AddModelError(string.Empty, 
                    $"Insufficient room capacity for {roomTypeName}. Only limited rooms are available for the selected dates. " +
                    $"Please reduce the number of required rooms or select different dates.");
                return View(model);
            }

            var bookingNumber = GenerateBookingNumber();
            var createdBy = GetCurrentUserId();
            var balanceAmount = quote.GrandTotal - model.DepositAmount;

            // Calculate original base amount before discount
            // quote.TotalRoomRate already contains the amount AFTER discount
            // We need to add back the discount to get the original amount
            decimal originalBaseAmount = quote.TotalRoomRate + quote.DiscountAmount;

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
                RequiredRooms = model.RequiredRooms,
                RoomId = null,
                RatePlanId = quote.RatePlanId,
                BaseAmount = originalBaseAmount, // Store original amount before discount
                TaxAmount = quote.TotalTaxAmount,
                CGSTAmount = quote.TotalCGSTAmount,
                SGSTAmount = quote.TotalSGSTAmount,
                DiscountAmount = quote.DiscountAmount, // Store actual discount amount
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

            var country = await _locationRepository.GetCountryByIdAsync(model.CountryId);
            var state = await _locationRepository.GetStateByIdAsync(model.StateId);
            var city = await _locationRepository.GetCityByIdAsync(model.CityId);

            var guests = new List<BookingGuest>
            {
                new BookingGuest
                {
                    FullName = $"{model.PrimaryGuestFirstName} {model.PrimaryGuestLastName}".Trim(),
                    Email = model.PrimaryGuestEmail,
                    Phone = model.PrimaryGuestPhone,
                    Gender = model.Gender?.Trim(),
                    GuestType = "Primary",
                    IsPrimary = true,
                    Address = model.AddressLine,
                    CountryId = model.CountryId,
                    StateId = model.StateId,
                    CityId = model.CityId,
                    Country = country?.Name,
                    State = state?.Name,
                    City = city?.Name,
                    Pincode = model.Pincode
                }
            };

            var payments = new List<BookingPayment>();
            // Payments will be collected via modal after redirect if CollectAdvancePayment is true

            // Don't create room nights during booking - will be created when room is assigned
            var roomNights = new List<BookingRoomNight>();

            var result = await _bookingRepository.CreateBookingAsync(booking, guests, payments, roomNights);

            TempData["BookingCreated"] = "true";
            TempData["BookingNumber"] = result.BookingNumber;
            TempData["BookingAmount"] = quote.GrandTotal.ToString("N2");
            TempData["ShowAdvancePaymentModal"] = model.CollectAdvancePayment ? "true" : "false";
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

                // Get location names from IDs if provided
                string? countryName = null;
                string? stateName = null;
                string? cityName = null;

                if (request.CountryId.HasValue)
                {
                    var country = await _locationRepository.GetCountryByIdAsync(request.CountryId.Value);
                    countryName = country?.Name;
                }

                if (request.StateId.HasValue)
                {
                    var state = await _locationRepository.GetStateByIdAsync(request.StateId.Value);
                    stateName = state?.Name;
                }

                if (request.CityId.HasValue)
                {
                    var city = await _locationRepository.GetCityByIdAsync(request.CityId.Value);
                    cityName = city?.Name;
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
                    Gender = request.Gender?.Trim(),
                    IdentityType = request.IdentityType?.Trim(),
                    IdentityNumber = request.IdentityNumber?.Trim(),
                    DocumentPath = request.DocumentPath?.Trim(),
                    Address = request.Address?.Trim(),
                    CountryId = request.CountryId,
                    StateId = request.StateId,
                    CityId = request.CityId,
                    Country = countryName,
                    State = stateName,
                    City = cityName,
                    Pincode = request.Pincode?.Trim(),
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

                // Get location names from IDs if provided
                string? countryName = null;
                string? stateName = null;
                string? cityName = null;

                if (request.CountryId.HasValue)
                {
                    var country = await _locationRepository.GetCountryByIdAsync(request.CountryId.Value);
                    countryName = country?.Name;
                }

                if (request.StateId.HasValue)
                {
                    var state = await _locationRepository.GetStateByIdAsync(request.StateId.Value);
                    stateName = state?.Name;
                }

                if (request.CityId.HasValue)
                {
                    var city = await _locationRepository.GetCityByIdAsync(request.CityId.Value);
                    cityName = city?.Name;
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
                    Gender = request.Gender?.Trim(),
                    IdentityType = request.IdentityType?.Trim(),
                    IdentityNumber = request.IdentityNumber?.Trim(),
                    DocumentPath = request.DocumentPath?.Trim(),
                    Address = request.Address?.Trim(),
                    CountryId = request.CountryId,
                    StateId = request.StateId,
                    CityId = request.CityId,
                    Country = countryName,
                    State = stateName,
                    City = cityName,
                    Pincode = request.Pincode?.Trim(),
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
            public string? Address { get; set; }
            public int? CountryId { get; set; }
            public int? StateId { get; set; }
            public int? CityId { get; set; }
            public string? Pincode { get; set; }
            public string? Gender { get; set; }
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
            public string? Address { get; set; }
            public int? CountryId { get; set; }
            public int? StateId { get; set; }
            public int? CityId { get; set; }
            public string? Pincode { get; set; }
            public string? Gender { get; set; }
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

        private async Task PopulateLookupsAsync(BookingCreateViewModel? model = null)
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = CustomerTypes;
            ViewBag.Sources = Sources;
            ViewBag.Channels = Channels;

            var countries = await _locationRepository.GetCountriesAsync();
            ViewBag.Countries = countries;

            if (model != null && model.CountryId > 0)
            {
                ViewBag.States = await _locationRepository.GetStatesByCountryAsync(model.CountryId);
            }
            else
            {
                ViewBag.States = Enumerable.Empty<State>();
            }

            if (model != null && model.StateId > 0)
            {
                ViewBag.Cities = await _locationRepository.GetCitiesByStateAsync(model.StateId);
            }
            else
            {
                ViewBag.Cities = Enumerable.Empty<City>();
            }
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
        public async Task<IActionResult> AssignRoom(string bookingNumber, int[] roomIds)
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

            // Validate room selection count matches RequiredRooms
            if (roomIds == null || roomIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select at least one room";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            if (roomIds.Length != booking.RequiredRooms)
            {
                TempData["ErrorMessage"] = $"Please select exactly {booking.RequiredRooms} room(s) as per the booking requirement";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            // Validate all selected rooms
            var allRooms = await _roomRepository.GetAllByBranchAsync(CurrentBranchID);
            var selectedRooms = allRooms.Where(r => roomIds.Contains(r.Id)).ToList();

            if (selectedRooms.Count != roomIds.Length)
            {
                TempData["ErrorMessage"] = "One or more selected rooms not found";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            // Check if all rooms match the booking's room type
            if (selectedRooms.Any(r => r.RoomTypeId != booking.RoomTypeId))
            {
                TempData["ErrorMessage"] = "All selected rooms must match the booking's room type";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            // Check if all rooms are available
            if (selectedRooms.Any(r => r.Status != "Available"))
            {
                TempData["ErrorMessage"] = "One or more selected rooms are not available";
                return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
            }

            try
            {
                // Assign all selected rooms to the booking
                var success = await _bookingRepository.AssignMultipleRoomsAsync(bookingNumber, roomIds, GetCurrentUserId());
                
                if (success)
                {
                    var roomNumbers = string.Join(", ", selectedRooms.Select(r => r.RoomNumber));
                    TempData["SuccessMessage"] = $"Room(s) {roomNumbers} assigned successfully to booking {bookingNumber}";
                    return RedirectToAction(nameof(Details), new { bookingNumber });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign rooms to booking";
                    return RedirectToAction(nameof(AssignRoom), new { bookingNumber });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning rooms: {ex.Message}";
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
                    BranchID = booking.BranchID,
                    RequiredRooms = booking.RequiredRooms
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
                    BranchID = CurrentBranchID,
                    RequiredRooms = booking.RequiredRooms
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
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _locationRepository.GetCountriesAsync();
            return Json(new
            {
                success = true,
                countries = countries.Select(c => new { c.Id, c.Name })
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetStates(int countryId)
        {
            if (countryId <= 0)
            {
                return Json(new { success = false, states = Array.Empty<object>() });
            }

            var states = await _locationRepository.GetStatesByCountryAsync(countryId);
            return Json(new
            {
                success = true,
                states = states.Select(s => new { s.Id, s.Name })
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(int stateId)
        {
            if (stateId <= 0)
            {
                return Json(new { success = false, cities = Array.Empty<object>() });
            }

            var cities = await _locationRepository.GetCitiesByStateAsync(stateId);
            return Json(new
            {
                success = true,
                cities = cities.Select(c => new { c.Id, c.Name })
            });
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
                    gender = guest.Gender,
                    loyaltyId = guest.LoyaltyId,
                    address = guest.Address,
                    countryId = guest.CountryId,
                    stateId = guest.StateId,
                    cityId = guest.CityId,
                    pincode = guest.Pincode
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
