using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IBillingHeadRepository
{
    Task<IReadOnlyList<BillingHead>> GetActiveAsync();
}
