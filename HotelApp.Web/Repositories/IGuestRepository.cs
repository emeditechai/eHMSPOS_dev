using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IGuestRepository
    {
        Task<Guest?> GetByPhoneAsync(string phone);
        Task<Guest?> GetByEmailAsync(string email);
        Task<Guest?> GetByIdAsync(int id);
        Task<int> CreateAsync(Guest guest);
        Task<bool> UpdateAsync(Guest guest);
        Task<IEnumerable<Guest>> GetChildGuestsAsync(int parentGuestId);
        Task<Guest?> FindOrCreateGuestAsync(string firstName, string lastName, string email, string phone, string guestType, int? parentGuestId = null);
        Task<IEnumerable<Guest>> GetAllAsync();
        Task<IEnumerable<Guest>> GetAllByBranchAsync(int branchId);
    }
}
