using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IRoomRepository
    {
        Task<IEnumerable<Room>> GetAllAsync();
        Task<IEnumerable<Room>> GetAllByBranchAsync(int branchId);
        Task<Room?> GetByIdAsync(int id);
        Task<Room?> GetByRoomNumberAsync(string roomNumber);
        Task<int> CreateAsync(Room room);
        Task<bool> UpdateAsync(Room room);
        Task<IEnumerable<RoomType>> GetRoomTypesAsync();
        Task<IEnumerable<RoomType>> GetRoomTypesByBranchAsync(int branchId);
        Task<RoomType?> GetRoomTypeByIdAsync(int id);
        Task<bool> RoomNumberExistsAsync(string roomNumber, int branchId, int? excludeId = null);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, string? excludeBookingNumber = null);
        Task<Dictionary<string, int>> GetRoomStatusCountsAsync(int branchId);
        Task<Dictionary<string, int>> GetYesterdayRoomStatusCountsAsync(int branchId);
        Task<string?> GetRoomStatusAsync(int roomId);
        Task<bool> UpdateRoomStatusAsync(int roomId, string status, int modifiedBy);
        Task<(bool success, int historyId, DateTime createdDate)> AddMaintenanceHistoryAsync(int roomId, string reason, int createdBy);
        Task<bool> CloseLatestMaintenanceAsync(int roomId);
        Task<(bool hasActiveBooking, string? bookingNumber, decimal balanceAmount)> GetActiveBookingForRoomAsync(int roomId);
        Task<(bool hasBooking, string? bookingNumber, decimal balanceAmount, DateTime? checkOutDate)> GetAnyBookingForRoomAsync(int roomId);
        Task<Dictionary<int, (string roomTypeName, int totalRooms, int availableRooms, decimal baseRate, int maxOccupancy, List<string> availableRoomNumbers, string? discount)>> GetRoomAvailabilityByDateRangeAsync(int branchId, DateTime startDate, DateTime endDate);
    }
}
