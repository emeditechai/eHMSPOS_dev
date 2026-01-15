using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class GuestFeedbackRepository : IGuestFeedbackRepository
    {
        private readonly IDbConnection _dbConnection;

        public GuestFeedbackRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> CreateAsync(GuestFeedback feedback)
        {
            const string sql = @"
                INSERT INTO GuestFeedback (
                    BranchID, BookingId, BookingNumber, RoomNumber, VisitDate,
                    GuestName, Email, Phone, Birthday, Anniversary, IsFirstVisit,
                    OverallRating, RoomCleanlinessRating, StaffBehaviorRating, ServiceRating, RoomComfortRating,
                    AmenitiesRating, FoodRating, ValueForMoneyRating, CheckInExperienceRating,
                    QuickTags, Comments, CreatedBy
                )
                VALUES (
                    @BranchID, @BookingId, @BookingNumber, @RoomNumber, @VisitDate,
                    @GuestName, @Email, @Phone, @Birthday, @Anniversary, @IsFirstVisit,
                    @OverallRating, @RoomCleanlinessRating, @StaffBehaviorRating, @ServiceRating, @RoomComfortRating,
                    @AmenitiesRating, @FoodRating, @ValueForMoneyRating, @CheckInExperienceRating,
                    @QuickTags, @Comments, @CreatedBy
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _dbConnection.ExecuteScalarAsync<int>(sql, feedback);
        }

        public async Task<GuestFeedback?> GetByIdAsync(int id, int branchId)
        {
            const string sql = @"
                SELECT *
                FROM GuestFeedback
                WHERE Id = @Id AND BranchID = @BranchID";

            return await _dbConnection.QueryFirstOrDefaultAsync<GuestFeedback>(sql, new { Id = id, BranchID = branchId });
        }

        public async Task<IEnumerable<GuestFeedback>> GetByBranchAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            const string sql = @"
                SELECT *
                FROM GuestFeedback
                WHERE BranchID = @BranchID
                  AND (@FromDate IS NULL OR VisitDate >= @FromDate)
                  AND (@ToDate IS NULL OR VisitDate <= @ToDate)
                ORDER BY VisitDate DESC, Id DESC";

            return await _dbConnection.QueryAsync<GuestFeedback>(sql, new
            {
                BranchID = branchId,
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date
            });
        }
    }
}
