using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IRoomRepository
    {
        Task<IEnumerable<Room>> GetAllAsync();
        Task<Room?> GetByIdAsync(int id);
        Task<Room?> GetByRoomNumberAsync(string roomNumber);
        Task<int> CreateAsync(Room room);
        Task<bool> UpdateAsync(Room room);
        Task<IEnumerable<RoomType>> GetRoomTypesAsync();
        Task<bool> RoomNumberExistsAsync(string roomNumber, int? excludeId = null);
    }
}
