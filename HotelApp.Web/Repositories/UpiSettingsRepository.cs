using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class UpiSettingsRepository : IUpiSettingsRepository
    {
        private readonly IDbConnection _connection;

        public UpiSettingsRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<UpiSettings?> GetByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT TOP 1
                    Id,
                    BranchID,
                    UpiVpa,
                    PayeeName,
                    IsEnabled,
                    CreatedDate,
                    CreatedBy,
                    LastModifiedDate,
                    LastModifiedBy
                FROM UpiSettings
                WHERE BranchID = @BranchId";

            return await _connection.QueryFirstOrDefaultAsync<UpiSettings>(sql, new { BranchId = branchId });
        }

        public async Task<int> UpsertAsync(UpiSettings settings)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM UpiSettings WHERE BranchID = @BranchID)
                BEGIN
                    UPDATE UpiSettings
                    SET UpiVpa = @UpiVpa,
                        PayeeName = @PayeeName,
                        IsEnabled = @IsEnabled,
                        LastModifiedDate = GETDATE(),
                        LastModifiedBy = @LastModifiedBy
                    WHERE BranchID = @BranchID;

                    SELECT Id FROM UpiSettings WHERE BranchID = @BranchID;
                END
                ELSE
                BEGIN
                    INSERT INTO UpiSettings
                        (BranchID, UpiVpa, PayeeName, IsEnabled, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy)
                    VALUES
                        (@BranchID, @UpiVpa, @PayeeName, @IsEnabled, GETDATE(), @CreatedBy, GETDATE(), @LastModifiedBy);

                    SELECT CAST(SCOPE_IDENTITY() as int);
                END";

            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                settings.BranchID,
                settings.UpiVpa,
                settings.PayeeName,
                settings.IsEnabled,
                settings.CreatedBy,
                settings.LastModifiedBy
            });
        }
    }
}
