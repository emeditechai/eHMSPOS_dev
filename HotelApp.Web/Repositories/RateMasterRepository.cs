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
                SELECT rm.*, rt.TypeName, rt.Description, rt.BaseRate as RoomTypeBaseRate, rt.MaxOccupancy, rt.Amenities, rt.BranchID
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
        
        public async Task<IEnumerable<RateMaster>> GetByBranchAsync(int branchId)
        {
            var sql = @"
                SELECT rm.*, rt.TypeName, rt.Description, rt.BaseRate as RoomTypeBaseRate, rt.MaxOccupancy, rt.Amenities, rt.BranchID
                FROM RateMaster rm
                INNER JOIN RoomTypes rt ON rm.RoomTypeId = rt.Id
                WHERE rm.IsActive = 1 AND rm.BranchID = @BranchId
                ORDER BY rm.StartDate DESC, rt.TypeName";

            var rates = await _dbConnection.QueryAsync<RateMaster, RoomType, RateMaster>(
                sql,
                (rate, roomType) =>
                {
                    rate.RoomType = roomType;
                    return rate;
                },
                new { BranchId = branchId },
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
                                       CGSTPercentage, SGSTPercentage, StartDate, EndDate, IsWeekdayRate, 
                                       ApplyDiscount, IsDynamicRate, BranchID, IsActive, CreatedDate, CreatedBy, LastModifiedDate)
                VALUES (@RoomTypeId, @CustomerType, @Source, @BaseRate, @ExtraPaxRate, @TaxPercentage,
                        @CGSTPercentage, @SGSTPercentage, @StartDate, @EndDate, @IsWeekdayRate, 
                        @ApplyDiscount, @IsDynamicRate, @BranchID, @IsActive, GETDATE(), @CreatedBy, GETDATE());
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
                    CGSTPercentage = @CGSTPercentage,
                    SGSTPercentage = @SGSTPercentage,
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

        // Delete removed per business rule

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
            var sql = "SELECT DISTINCT CustomerType FROM RateMaster WHERE IsActive = 1 AND CustomerType IS NOT NULL ORDER BY CustomerType";
            return await _dbConnection.QueryAsync<string>(sql);
        }

        public async Task<IEnumerable<string>> GetSourcesAsync()
        {
            var sql = "SELECT DISTINCT Source FROM RateMaster WHERE IsActive = 1 AND Source IS NOT NULL ORDER BY Source";
            return await _dbConnection.QueryAsync<string>(sql);
        }

        // Weekend Rates Implementation
        public async Task<int> CreateWeekendRateAsync(WeekendRate weekendRate)
        {
            var sql = @"
                INSERT INTO WeekendRates (RateMasterId, DayOfWeek, BaseRate, ExtraPaxRate, IsActive, CreatedDate, CreatedBy, LastModifiedDate)
                VALUES (@RateMasterId, @DayOfWeek, @BaseRate, @ExtraPaxRate, @IsActive, GETDATE(), @CreatedBy, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, weekendRate);
            return id;
        }

        public async Task<IEnumerable<WeekendRate>> GetWeekendRatesByRateMasterIdAsync(int rateMasterId)
        {
            var sql = @"
                SELECT Id, RateMasterId, DayOfWeek, BaseRate, ExtraPaxRate, IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM WeekendRates
                WHERE RateMasterId = @RateMasterId AND IsActive = 1
                ORDER BY CASE DayOfWeek
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'Sunday' THEN 7
                END";

            return await _dbConnection.QueryAsync<WeekendRate>(sql, new { RateMasterId = rateMasterId });
        }

        public async Task<bool> UpdateWeekendRateAsync(WeekendRate weekendRate)
        {
            var sql = @"
                UPDATE WeekendRates
                SET DayOfWeek = @DayOfWeek,
                    BaseRate = @BaseRate,
                    ExtraPaxRate = @ExtraPaxRate,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @LastModifiedBy
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, weekendRate);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteWeekendRateAsync(int id)
        {
            var sql = "UPDATE WeekendRates SET IsActive = 0, LastModifiedDate = GETDATE() WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        // Special Day Rates Implementation
        public async Task<int> CreateSpecialDayRateAsync(SpecialDayRate specialDayRate)
        {
            var sql = @"
                INSERT INTO SpecialDayRates (RateMasterId, FromDate, ToDate, EventName, BaseRate, ExtraPaxRate, IsActive, CreatedDate, CreatedBy, LastModifiedDate)
                VALUES (@RateMasterId, @FromDate, @ToDate, @EventName, @BaseRate, @ExtraPaxRate, @IsActive, GETDATE(), @CreatedBy, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, specialDayRate);
            return id;
        }

        public async Task<IEnumerable<SpecialDayRate>> GetSpecialDayRatesByRateMasterIdAsync(int rateMasterId)
        {
            var sql = @"
                SELECT Id, RateMasterId, FromDate, ToDate, EventName, BaseRate, ExtraPaxRate, IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM SpecialDayRates
                WHERE RateMasterId = @RateMasterId AND IsActive = 1
                ORDER BY FromDate";

            return await _dbConnection.QueryAsync<SpecialDayRate>(sql, new { RateMasterId = rateMasterId });
        }

        public async Task<bool> UpdateSpecialDayRateAsync(SpecialDayRate specialDayRate)
        {
            var sql = @"
                UPDATE SpecialDayRates
                SET FromDate = @FromDate,
                    ToDate = @ToDate,
                    EventName = @EventName,
                    BaseRate = @BaseRate,
                    ExtraPaxRate = @ExtraPaxRate,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @LastModifiedBy
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, specialDayRate);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteSpecialDayRateAsync(int id)
        {
            var sql = "UPDATE SpecialDayRates SET IsActive = 0, LastModifiedDate = GETDATE() WHERE Id = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }
    }
}
