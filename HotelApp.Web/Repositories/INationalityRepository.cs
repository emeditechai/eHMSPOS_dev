using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface INationalityRepository
    {
        Task<IEnumerable<Nationality>> GetAllActiveAsync();
        Task<Nationality?> GetByIdAsync(int id);
    }
}
