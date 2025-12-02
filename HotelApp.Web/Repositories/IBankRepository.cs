using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBankRepository
    {
        Task<IEnumerable<Bank>> GetAllActiveAsync();
        Task<Bank?> GetByIdAsync(int id);
    }
}
