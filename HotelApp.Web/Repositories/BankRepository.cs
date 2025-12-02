using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class BankRepository : IBankRepository
    {
        private readonly IDbConnection _dbConnection;

        public BankRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Bank>> GetAllActiveAsync()
        {
            const string sql = @"
                SELECT * FROM Banks 
                WHERE IsActive = 1 
                ORDER BY BankName";

            return await _dbConnection.QueryAsync<Bank>(sql);
        }

        public async Task<Bank?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Banks WHERE Id = @Id AND IsActive = 1";
            return await _dbConnection.QueryFirstOrDefaultAsync<Bank>(sql, new { Id = id });
        }
    }
}
