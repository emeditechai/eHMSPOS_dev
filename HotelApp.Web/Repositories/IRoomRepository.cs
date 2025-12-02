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
        Task<bool> RoomNumberExistsAsync(string roomNumber, int branchId, int? excludeId = null);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, string? excludeBookingNumber = null);
    }
}
