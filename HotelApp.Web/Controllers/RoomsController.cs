using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using HotelApp.Web.Models;

namespace HotelApp.Web.Controllers;

[Authorize]
public class RoomsController : BaseController
{
    private readonly IRoomRepository _roomRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRateMasterRepository _rateMasterRepository;

    public RoomsController(
        IRoomRepository roomRepository, 
        IFloorRepository floorRepository, 
        IBookingRepository bookingRepository,
        IRateMasterRepository rateMasterRepository)
    {
        _roomRepository = roomRepository;
        _floorRepository = floorRepository;
        _bookingRepository = bookingRepository;
        _rateMasterRepository = rateMasterRepository;
    }

    public async Task<IActionResult> Dashboard()
    {
        var rooms = await _roomRepository.GetAllByBranchAsync(CurrentBranchID);
        var allFloors = await _floorRepository.GetByBranchAsync(CurrentBranchID);
        
        // Filter floors to only include those that have rooms
        var floorsWithRooms = allFloors.Where(f => rooms.Any(r => r.Floor == f.Id)).ToList();
        
        // Get current status counts
        var statusCounts = await _roomRepository.GetRoomStatusCountsAsync(CurrentBranchID);
        var yesterdayStatusCounts = await _roomRepository.GetYesterdayRoomStatusCountsAsync(CurrentBranchID);

        // Get room availability for today by default
        var startDate = DateTime.Today;
        var endDate = DateTime.Today;
        var availability = await _roomRepository.GetRoomAvailabilityByDateRangeAsync(CurrentBranchID, startDate, endDate);

        var viewModel = new RoomDashboardViewModel
        {
            AvailableCount = statusCounts["Available"],
            OccupiedCount = statusCounts["Occupied"],
            MaintenanceCount = statusCounts["Maintenance"],
            CleaningCount = statusCounts["Cleaning"],
            AvailableChange = statusCounts["Available"] - yesterdayStatusCounts["Available"],
            OccupiedChange = statusCounts["Occupied"] - yesterdayStatusCounts["Occupied"],
            MaintenanceChange = statusCounts["Maintenance"] - yesterdayStatusCounts["Maintenance"],
            CleaningChange = statusCounts["Cleaning"] - yesterdayStatusCounts["Cleaning"],
            Rooms = rooms.Select(r => new RoomDashboardItem
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                RoomTypeName = r.RoomType?.TypeName ?? "Unknown",
                Status = r.Status,
                Floor = r.Floor,
                FloorName = r.FloorName,
                BaseRate = r.RoomType?.BaseRate ?? 0,
                MaxOccupancy = r.RoomType?.MaxOccupancy ?? 0,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                BalanceAmount = r.BalanceAmount,
                BookingNumber = r.BookingNumber,
                PrimaryGuestName = r.PrimaryGuestName,
                GuestCount = r.GuestCount
            }).ToList(),
            Floors = floorsWithRooms,
            RoomTypeAvailabilities = availability.Select(kvp => new RoomTypeAvailability
            {
                RoomTypeId = kvp.Key,
                RoomTypeName = kvp.Value.roomTypeName,
                TotalRooms = kvp.Value.totalRooms,
                AvailableRooms = kvp.Value.availableRooms,
                OccupiedRooms = kvp.Value.totalRooms - kvp.Value.availableRooms,
                BaseRate = kvp.Value.baseRate,
                ApplyDiscount = kvp.Value.discount,
                MaxOccupancy = kvp.Value.maxOccupancy,
                AvailableRoomNumbers = kvp.Value.availableRoomNumbers
            }).ToList(),
            StartDate = startDate,
            EndDate = endDate
        };

        ViewData["Title"] = "Room Dashboard";
        return View(viewModel);
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> UpdateRoomStatus([FromBody] UpdateRoomStatusRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Status))
        {
            return Json(new { success = false, message = "Invalid status" });
        }

        var validStatuses = new[] { "Available", "Occupied", "Maintenance", "Cleaning" };
        if (!validStatuses.Contains(request.Status))
        {
            return Json(new { success = false, message = "Invalid status value" });
        }

        var currentUserId = GetCurrentUserId();
        var success = await _roomRepository.UpdateRoomStatusAsync(request.RoomId, request.Status, currentUserId);

        if (success)
        {
            return Json(new { success = true, message = $"Room status updated to {request.Status}" });
        }

        return Json(new { success = false, message = "Failed to update room status" });
    }

    [HttpGet]
    public async Task<IActionResult> GetRoomDetails(int roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return Json(new { success = false, message = "Room not found" });
        }

        return Json(new
        {
            success = true,
            room = new
            {
                id = room.Id,
                roomNumber = room.RoomNumber,
                roomType = room.RoomType?.TypeName ?? "Unknown",
                status = room.Status,
                floor = room.FloorName ?? $"Floor {room.Floor}",
                baseRate = room.RoomType?.BaseRate ?? 0,
                maxOccupancy = room.RoomType?.MaxOccupancy ?? 0,
                amenities = room.RoomType?.Amenities ?? ""
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CheckoutRoom([FromBody] CheckoutRequest request)
    {
        try
        {
            if (request == null || request.RoomId <= 0)
            {
                return Json(new { success = false, message = "Invalid room ID" });
            }

            Console.WriteLine($"CheckoutRoom called for roomId: {request.RoomId}");
            
            // Check if room has active booking
            var (hasActiveBooking, bookingNumber, balanceAmount) = await _roomRepository.GetActiveBookingForRoomAsync(request.RoomId);

            Console.WriteLine($"Active booking check: hasActiveBooking={hasActiveBooking}, bookingNumber={bookingNumber}, balanceAmount={balanceAmount}");

            if (!hasActiveBooking)
            {
                var anyBooking = await _roomRepository.GetAnyBookingForRoomAsync(request.RoomId);
                if (anyBooking.hasBooking && !string.IsNullOrWhiteSpace(anyBooking.bookingNumber))
                {
                    var detectedCheckOutDate = anyBooking.checkOutDate;
                    var today = DateTime.Today;
                    var isExpired = detectedCheckOutDate.HasValue && detectedCheckOutDate.Value.Date <= today;
                    return Json(new
                    {
                        success = false,
                        message = "No active booking found for this room",
                        reason = isExpired ? "Expired" : "NoActiveBooking",
                        isExpired,
                        bookingNumber = anyBooking.bookingNumber,
                        detectedCheckOutDate = detectedCheckOutDate
                    });
                }

                return Json(new { success = false, message = "No active booking found for this room", reason = "NoBooking" });
            }

            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                Console.WriteLine("Active booking detected but booking number is missing");
                return Json(new { success = false, message = "Unable to determine booking reference for this room. Please open the booking and try again." });
            }

            // Check if payment is cleared
            if (balanceAmount > 0)
            {
                Console.WriteLine($"Payment not cleared. Balance: {balanceAmount}");
                return Json(new 
                { 
                    success = false, 
                    message = "Payment Not Cleared",
                    requiresPayment = true,
                    bookingNumber = bookingNumber,
                    balanceAmount = balanceAmount
                });
            }

            // Payment is cleared, proceed with checkout
            var currentUserId = GetCurrentUserId();
            Console.WriteLine($"Updating room status to Cleaning. UserId: {currentUserId}");
            
            // Update actual checkout date/time
            var actualCheckOutDate = DateTime.Now;
            await _bookingRepository.UpdateActualCheckOutDateAsync(bookingNumber, actualCheckOutDate, currentUserId);
            Console.WriteLine($"Actual checkout date updated: {actualCheckOutDate}");
            
            // Get all rooms assigned to this booking and update their status
            var assignedRoomIds = await _bookingRepository.GetAssignedRoomIdsAsync(bookingNumber);
            Console.WriteLine($"Found {assignedRoomIds.Count()} rooms assigned to booking {bookingNumber}");
            
            bool allSuccess = true;
            foreach (var roomId in assignedRoomIds)
            {
                var success = await _roomRepository.UpdateRoomStatusAsync(roomId, "Cleaning", currentUserId);
                if (!success)
                {
                    Console.WriteLine($"Failed to update room {roomId} status");
                    allSuccess = false;
                }
                else
                {
                    Console.WriteLine($"Room {roomId} status updated to Cleaning");
                }
            }

            if (allSuccess)
            {
                Console.WriteLine($"All {assignedRoomIds.Count()} room(s) checked out successfully");
                return Json(new 
                { 
                    success = true, 
                    message = assignedRoomIds.Count() > 1 
                        ? $"All {assignedRoomIds.Count()} rooms checked out successfully. Status changed to Cleaning."
                        : "Room checked out successfully. Status changed to Cleaning.",
                    bookingNumber = bookingNumber
                });
            }

            Console.WriteLine("Failed to update some room statuses");
            return Json(new { success = false, message = "Failed to update some room statuses" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CheckoutRoom: {ex.Message}\n{ex.StackTrace}");
            return Json(new { success = false, message = $"Error during checkout: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ForceCheckoutRoom([FromBody] CheckoutRequest request)
    {
        try
        {
            if (request == null || request.RoomId <= 0)
            {
                return Json(new { success = false, message = "Invalid room ID" });
            }

            Console.WriteLine($"ForceCheckoutRoom called for roomId: {request.RoomId}");
            
            // Check if room has ANY booking (including expired ones)
            var (hasBooking, bookingNumber, balanceAmount, checkOutDate) = await _roomRepository.GetAnyBookingForRoomAsync(request.RoomId);

            Console.WriteLine($"Booking check: hasBooking={hasBooking}, bookingNumber={bookingNumber}, balanceAmount={balanceAmount}, checkOutDate={checkOutDate}");

            if (!hasBooking)
            {
                return Json(new { success = false, message = "No booking found for this room" });
            }

            if (string.IsNullOrWhiteSpace(bookingNumber))
            {
                Console.WriteLine("Booking detected but booking number is missing (force checkout)");
                return Json(new { success = false, message = "Unable to determine booking reference for this room. Please open the booking and try again." });
            }

            // Check if payment is cleared
            if (balanceAmount > 0)
            {
                Console.WriteLine($"Payment not cleared. Balance: {balanceAmount}");
                return Json(new 
                { 
                    success = false, 
                    message = "Payment Not Cleared",
                    requiresPayment = true,
                    bookingNumber = bookingNumber,
                    balanceAmount = balanceAmount,
                    checkOutDate = checkOutDate
                });
            }

            // Payment is cleared, proceed with force checkout
            var currentUserId = GetCurrentUserId();
            Console.WriteLine($"Force updating room status to Cleaning. UserId: {currentUserId}");
            
            // Update actual checkout date/time
            var actualCheckOutDate = DateTime.Now;
            await _bookingRepository.UpdateActualCheckOutDateAsync(bookingNumber, actualCheckOutDate, currentUserId);
            Console.WriteLine($"Actual checkout date updated (Force): {actualCheckOutDate}");
            
            // Get all rooms assigned to this booking and update their status
            var assignedRoomIds = await _bookingRepository.GetAssignedRoomIdsAsync(bookingNumber);
            Console.WriteLine($"Found {assignedRoomIds.Count()} rooms assigned to booking {bookingNumber} (Force Checkout)");
            
            bool allSuccess = true;
            foreach (var roomId in assignedRoomIds)
            {
                var success = await _roomRepository.UpdateRoomStatusAsync(roomId, "Cleaning", currentUserId);
                if (!success)
                {
                    Console.WriteLine($"Failed to update room {roomId} status (Force)");
                    allSuccess = false;
                }
                else
                {
                    Console.WriteLine($"Room {roomId} status updated to Cleaning (Force)");
                }
            }

            if (allSuccess)
            {
                Console.WriteLine($"All {assignedRoomIds.Count()} room(s) force checked out successfully");
                return Json(new 
                { 
                    success = true, 
                    message = assignedRoomIds.Count() > 1 
                        ? $"All {assignedRoomIds.Count()} rooms force checked out successfully. Status changed to Cleaning."
                        : "Room force checked out successfully. Status changed to Cleaning.",
                    bookingNumber = bookingNumber
                });
            }

            Console.WriteLine("Failed to update room status");
            return Json(new { success = false, message = "Failed to update room status" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ForceCheckoutRoom: {ex.Message}\n{ex.StackTrace}");
            return Json(new { success = false, message = $"Error during force checkout: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailabilityByDateRange(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;

            if (end < start)
            {
                return Json(new { success = false, message = "End date must be after start date" });
            }

            var availability = await _roomRepository.GetRoomAvailabilityByDateRangeAsync(CurrentBranchID, start, end);

            // Get all rate masters for this branch with weekend and special day rates
            var allRates = await _rateMasterRepository.GetByBranchAsync(CurrentBranchID);
            
            var result = availability.Select(kvp => 
            {
                var roomTypeId = kvp.Key;
                var roomData = kvp.Value;
                
                // B2C: Get the effective rate for this date and room type (WITHOUT discount applied yet)
                var rateInfo = GetEffectiveRateForDate(allRates, roomTypeId, start, "B2C");
                var b2cTaxPercentage = rateInfo.taxPercentage;

                // B2B: Optional rate (same rate-type logic). Only include if a B2B entry exists.
                var b2bRateInfo = GetEffectiveRateForDate(allRates, roomTypeId, start, "B2B");
                var hasB2BRate = b2bRateInfo.baseRate > 0;
                var b2bTaxPercentage = b2bRateInfo.taxPercentage;
                
                // Calculate discount
                decimal originalRate = rateInfo.baseRate;
                decimal discountPercent = 0;
                decimal discountedRate = originalRate;
                decimal discountAmount = 0;

                decimal b2bOriginalRate = b2bRateInfo.baseRate;
                decimal b2bDiscountedRate = b2bOriginalRate;
                decimal b2bDiscountAmount = 0;
                
                if (!string.IsNullOrEmpty(roomData.discount) && decimal.TryParse(roomData.discount, out var discount))
                {
                    discountPercent = discount;
                    discountedRate = Math.Round(originalRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                    discountAmount = Math.Round(originalRate - discountedRate, 2, MidpointRounding.AwayFromZero);

                    if (hasB2BRate)
                    {
                        b2bDiscountedRate = Math.Round(b2bOriginalRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                        b2bDiscountAmount = Math.Round(b2bOriginalRate - b2bDiscountedRate, 2, MidpointRounding.AwayFromZero);
                    }
                }
                
                return new
                {
                    roomTypeId = roomTypeId,
                    roomTypeName = roomData.roomTypeName,
                    totalRooms = roomData.totalRooms,
                    availableRooms = roomData.availableRooms,
                    occupiedRooms = roomData.totalRooms - roomData.availableRooms,
                    originalRate = originalRate,
                    baseRate = discountedRate,  // This is the discounted rate
                    discountPercent = discountPercent,
                    discountAmount = discountAmount,
                    taxPercentage = b2cTaxPercentage,
                    extraPaxRate = rateInfo.extraPaxRate,
                    rateType = rateInfo.rateType,
                    eventName = rateInfo.eventName,
                    b2bOriginalRate = hasB2BRate ? b2bOriginalRate : (decimal?)null,
                    b2bRate = hasB2BRate ? b2bDiscountedRate : (decimal?)null,
                    b2bDiscountPercent = hasB2BRate ? discountPercent : (decimal?)null,
                    b2bDiscountAmount = hasB2BRate ? b2bDiscountAmount : (decimal?)null,
                    b2bTaxPercentage = hasB2BRate ? b2bTaxPercentage : (decimal?)null,
                    b2bExtraPaxRate = hasB2BRate ? b2bRateInfo.extraPaxRate : (decimal?)null,
                    b2bRateType = hasB2BRate ? b2bRateInfo.rateType : null,
                    b2bEventName = hasB2BRate ? b2bRateInfo.eventName : null,
                    applyDiscount = roomData.discount,
                    maxOccupancy = roomData.maxOccupancy,
                    availableRoomNumbers = roomData.availableRoomNumbers
                };
            }).ToList();

            return Json(new { success = true, data = result, startDate = start, endDate = end });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error fetching availability: {ex.Message}" });
        }
    }

    private (decimal baseRate, decimal extraPaxRate, string rateType, string? eventName, decimal taxPercentage) GetEffectiveRateForDate(
        IEnumerable<RateMaster> allRates, int roomTypeId, DateTime date, string customerType)
    {
        static decimal EffectiveTaxPercentage(RateMaster r)
        {
            if (r.TaxPercentage > 0m) return r.TaxPercentage;
            var split = r.CGSTPercentage + r.SGSTPercentage;
            return split > 0m ? split : r.TaxPercentage;
        }

        // Get rates for this room type
        var roomTypeRates = allRates
            .Where(r => r.RoomTypeId == roomTypeId && r.IsActive && r.CustomerType == customerType)
            .ToList();
        
        if (!roomTypeRates.Any())
        {
            return (0, 0, "No Rate", null, 0m);
        }

        // Priority 1: Check for Special Day Rates
        foreach (var rate in roomTypeRates)
        {
            var specialDayRatesTask = _rateMasterRepository.GetSpecialDayRatesByRateMasterIdAsync(rate.Id);
            specialDayRatesTask.Wait();
            var specialDayRates = specialDayRatesTask.Result;
            
            var specialRate = specialDayRates.FirstOrDefault(s => 
                s.IsActive && 
                date.Date >= s.FromDate.Date && 
                date.Date <= s.ToDate.Date);
            
            if (specialRate != null)
            {
                return (specialRate.BaseRate, specialRate.ExtraPaxRate, "Special Day", specialRate.EventName, EffectiveTaxPercentage(rate));
            }
        }

        // Priority 2: Check for Weekend Rates
        // WeekendRates.DayOfWeek values are stored in English; avoid culture-specific day names.
        var dayOfWeek = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => ""
        };
        foreach (var rate in roomTypeRates)
        {
            var weekendRatesTask = _rateMasterRepository.GetWeekendRatesByRateMasterIdAsync(rate.Id);
            weekendRatesTask.Wait();
            var weekendRates = weekendRatesTask.Result;
            
            var weekendRate = weekendRates.FirstOrDefault(w => 
                w.IsActive && 
                w.DayOfWeek.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));
            
            if (weekendRate != null)
            {
                return (weekendRate.BaseRate, weekendRate.ExtraPaxRate, "Weekend", null, EffectiveTaxPercentage(rate));
            }
        }

        // Priority 3: Default Rate
        var defaultRate = roomTypeRates.FirstOrDefault(r => 
            r.StartDate.Date <= date.Date && 
            r.EndDate.Date >= date.Date);
        
        if (defaultRate != null)
        {
            return (defaultRate.BaseRate, defaultRate.ExtraPaxRate, "Standard", null, EffectiveTaxPercentage(defaultRate));
        }

        // Fallback to first rate if no date match
        var fallbackRate = roomTypeRates.First();
        return (fallbackRate.BaseRate, fallbackRate.ExtraPaxRate, "Standard", null, EffectiveTaxPercentage(fallbackRate));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }
}

public class CheckoutRequest
{
    public int RoomId { get; set; }
}

public class UpdateRoomStatusRequest
{
    public int RoomId { get; set; }
    public string Status { get; set; } = string.Empty;
}
