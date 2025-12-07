using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Controllers;

[Authorize]
public class RoomsController : BaseController
{
    private readonly IRoomRepository _roomRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly IBookingRepository _bookingRepository;

    public RoomsController(IRoomRepository roomRepository, IFloorRepository floorRepository, IBookingRepository bookingRepository)
    {
        _roomRepository = roomRepository;
        _floorRepository = floorRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<IActionResult> Dashboard()
    {
        var rooms = await _roomRepository.GetAllByBranchAsync(CurrentBranchID);
        var floors = await _floorRepository.GetByBranchAsync(CurrentBranchID);
        
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
                PrimaryGuestName = r.PrimaryGuestName
            }).ToList(),
            Floors = floors.ToList(),
            RoomTypeAvailabilities = availability.Select(kvp => new RoomTypeAvailability
            {
                RoomTypeId = kvp.Key,
                RoomTypeName = kvp.Value.roomTypeName,
                TotalRooms = kvp.Value.totalRooms,
                AvailableRooms = kvp.Value.availableRooms,
                OccupiedRooms = kvp.Value.totalRooms - kvp.Value.availableRooms,
                BaseRate = kvp.Value.baseRate,
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
                return Json(new { success = false, message = "No active booking found for this room" });
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

            var result = availability.Select(kvp => new
            {
                roomTypeId = kvp.Key,
                roomTypeName = kvp.Value.roomTypeName,
                totalRooms = kvp.Value.totalRooms,
                availableRooms = kvp.Value.availableRooms,
                occupiedRooms = kvp.Value.totalRooms - kvp.Value.availableRooms,
                baseRate = kvp.Value.baseRate,
                maxOccupancy = kvp.Value.maxOccupancy,
                availableRoomNumbers = kvp.Value.availableRoomNumbers
            }).ToList();

            return Json(new { success = true, data = result, startDate = start, endDate = end });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error fetching availability: {ex.Message}" });
        }
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
