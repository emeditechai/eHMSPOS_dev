using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class RateMasterRepository : IRateMasterRepository
    {
        private readonly IDbConnection _dbConnection;

        public RateMasterRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<RateMaster>> GetAllAsync()
        {
            var sql = @"
                SELECT rm.*, rt.TypeName, rt.Description, rt.BaseRate as RoomTypeBaseRate, rt.MaxOccupancy, rt.Amenities
                FROM RateMaster rm
                INNER JOIN RoomTypes rt ON rm.RoomTypeId = rt.Id
                WHERE rm.IsActive = 1
                ORDER BY rm.StartDate DESC, rt.TypeName";

            var rates = await _dbConnection.QueryAsync<RateMaster, RoomType, RateMaster>(
                sql,
                (rate, roomType) =>
                {
                    rate.RoomType = roomType;
                    return rate;
                },
                splitOn: "TypeName"
            );

            return rates;
        }

        public async Task<RateMaster?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT rm.*, rt.TypeName, rt.Description, rt.BaseRate as RoomTypeBaseRate, rt.MaxOccupancy, rt.Amenities
                FROM RateMaster rm
                INNER JOIN RoomTypes rt ON rm.RoomTypeId = rt.Id
                WHERE rm.Id = @Id AND rm.IsActive = 1";

            var rates = await _dbConnection.QueryAsync<RateMaster, RoomType, RateMaster>(
                sql,
                (rate, roomType) =>
                {
                    rate.RoomType = roomType;
                    return rate;
                },
                new { Id = id },
                splitOn: "TypeName"
            );

            return rates.FirstOrDefault();
        }

        public async Task<int> CreateAsync(RateMaster rate)
        {
            var sql = @"
                INSERT INTO RateMaster (RoomTypeId, CustomerType, Source, BaseRate, ExtraPaxRate, TaxPercentage, 
                                       StartDate, EndDate, IsWeekdayRate, ApplyDiscount, IsDynamicRate, 
                                       IsActive, CreatedDate, CreatedBy, LastModifiedDate)
                VALUES (@RoomTypeId, @CustomerType, @Source, @BaseRate, @ExtraPaxRate, @TaxPercentage,
                        @StartDate, @EndDate, @IsWeekdayRate, @ApplyDiscount, @IsDynamicRate,
                        @IsActive, GETDATE(), @CreatedBy, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, rate);
            return id;
        }

        public async Task<bool> UpdateAsync(RateMaster rate)
        {
            var sql = @"
                UPDATE RateMaster
                SET RoomTypeId = @RoomTypeId,
                    CustomerType = @CustomerType,
                    Source = @Source,
                    BaseRate = @BaseRate,
                    ExtraPaxRate = @ExtraPaxRate,
                    TaxPercentage = @TaxPercentage,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    IsWeekdayRate = @IsWeekdayRate,
                    ApplyDiscount = @ApplyDiscount,
                    IsDynamicRate = @IsDynamicRate,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE()
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, rate);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var sql = @"
                UPDATE RateMaster
                SET IsActive = 0,
                    LastModifiedDate = GETDATE()
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        public async Task<IEnumerable<RoomType>> GetRoomTypesAsync()
        {
            var sql = @"
                SELECT Id, TypeName, Description, BaseRate, MaxOccupancy, Amenities
                FROM RoomTypes
                WHERE IsActive = 1
                ORDER BY TypeName";

            return await _dbConnection.QueryAsync<RoomType>(sql);
        }

        public async Task<IEnumerable<string>> GetCustomerTypesAsync()
        {
            var sql = "SELECT DISTINCT CustomerType FROM RateTypes WHERE IsActive = 1 ORDER BY CustomerType";
            return await _dbConnection.QueryAsync<string>(sql);
        }

        public async Task<IEnumerable<string>> GetSourcesAsync()
        {
            var sql = "SELECT DISTINCT Source FROM RateTypes WHERE IsActive = 1 ORDER BY Source";
            return await _dbConnection.QueryAsync<string>(sql);
        }
    }
}
