using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class NationalityRepository : INationalityRepository
    {
        private readonly IDbConnection _dbConnection;

        public NationalityRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Nationality>> GetAllActiveAsync()
        {
            const string sql = "SELECT Id, Name, Code FROM Nationalities WHERE IsActive = 1 ORDER BY Name";
            return await _dbConnection.QueryAsync<Nationality>(sql);
        }

        public async Task<Nationality?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Nationalities WHERE Id = @Id AND IsActive = 1";
            return await _dbConnection.QueryFirstOrDefaultAsync<Nationality>(sql, new { Id = id });
        }
    }
}
