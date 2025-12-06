using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface ILocationRepository
    {
        Task<IEnumerable<Country>> GetCountriesAsync();
        Task<IEnumerable<State>> GetStatesByCountryAsync(int countryId);
        Task<IEnumerable<City>> GetCitiesByStateAsync(int stateId);
        Task<Country?> GetCountryByIdAsync(int countryId);
        Task<State?> GetStateByIdAsync(int stateId);
        Task<City?> GetCityByIdAsync(int cityId);
    }
}
