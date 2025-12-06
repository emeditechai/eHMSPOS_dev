using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly IDbConnection _dbConnection;

        public LocationRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync()
        {
            const string sql = "SELECT Id, Name, Code, IsActive FROM Countries WHERE IsActive = 1 ORDER BY Name";
            return await _dbConnection.QueryAsync<Country>(sql);
        }

        public async Task<IEnumerable<State>> GetStatesByCountryAsync(int countryId)
        {
            const string sql = "SELECT Id, CountryId, Name, Code, IsActive FROM States WHERE CountryId = @CountryId AND IsActive = 1 ORDER BY Name";
            return await _dbConnection.QueryAsync<State>(sql, new { CountryId = countryId });
        }

        public async Task<IEnumerable<City>> GetCitiesByStateAsync(int stateId)
        {
            const string sql = "SELECT Id, StateId, Name, IsActive FROM Cities WHERE StateId = @StateId AND IsActive = 1 ORDER BY Name";
            return await _dbConnection.QueryAsync<City>(sql, new { StateId = stateId });
        }

        public async Task<Country?> GetCountryByIdAsync(int countryId)
        {
            const string sql = "SELECT Id, Name, Code, IsActive FROM Countries WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<Country>(sql, new { Id = countryId });
        }

        public async Task<State?> GetStateByIdAsync(int stateId)
        {
            const string sql = "SELECT Id, CountryId, Name, Code, IsActive FROM States WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<State>(sql, new { Id = stateId });
        }

        public async Task<City?> GetCityByIdAsync(int cityId)
        {
            const string sql = "SELECT Id, StateId, Name, IsActive FROM Cities WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<City>(sql, new { Id = cityId });
        }
    }
}
