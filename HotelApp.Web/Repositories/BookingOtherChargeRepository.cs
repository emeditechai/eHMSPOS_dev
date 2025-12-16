using System.Data;
using Dapper;

namespace HotelApp.Web.Repositories
{
    public class BookingOtherChargeRepository : IBookingOtherChargeRepository
    {
        private readonly IDbConnection _connection;

        public BookingOtherChargeRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<BookingOtherChargeDetailRow>> GetDetailsByBookingIdAsync(int bookingId)
        {
            const string sql = @"
                SELECT boc.OtherChargeId,
                       oc.Code,
                       oc.[Name],
                       oc.[Type],
                       oc.GSTPercent,
                       oc.CGSTPercent,
                       oc.SGSTPercent,
                      boc.Qty,
                       boc.Rate,
                      boc.Note,
                       boc.GSTAmount,
                       boc.CGSTAmount,
                       boc.SGSTAmount
                  FROM BookingOtherCharges boc
                  JOIN OtherCharges oc ON oc.Id = boc.OtherChargeId
                 WHERE boc.BookingId = @BookingId
                 ORDER BY oc.[Name], oc.Code";

            return await _connection.QueryAsync<BookingOtherChargeDetailRow>(sql, new { BookingId = bookingId });
        }

        public async Task UpsertForBookingAsync(int bookingId, IReadOnlyList<BookingOtherChargeUpsertRow> rows, int? performedBy)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            using var tx = _connection.BeginTransaction();

            var existing = (await _connection.QueryAsync<int>(
                "SELECT OtherChargeId FROM BookingOtherCharges WHERE BookingId = @BookingId",
                new { BookingId = bookingId },
                tx)).ToHashSet();

            var incomingIds = rows.Select(r => r.OtherChargeId).Distinct().ToList();

            foreach (var row in rows)
            {
                if (existing.Contains(row.OtherChargeId))
                {
                    const string updateSql = @"
                        UPDATE BookingOtherCharges
                           SET Qty = @Qty,
                               Rate = @Rate,
                               Note = @Note,
                               GSTAmount = @GSTAmount,
                               CGSTAmount = @CGSTAmount,
                               SGSTAmount = @SGSTAmount,
                               UpdatedDate = SYSUTCDATETIME(),
                               UpdatedBy = @UpdatedBy
                         WHERE BookingId = @BookingId AND OtherChargeId = @OtherChargeId";

                    await _connection.ExecuteAsync(updateSql, new
                    {
                        BookingId = bookingId,
                        row.OtherChargeId,
                        row.Qty,
                        row.Rate,
                        row.Note,
                        row.GSTAmount,
                        row.CGSTAmount,
                        row.SGSTAmount,
                        UpdatedBy = performedBy
                    }, tx);
                }
                else
                {
                    const string insertSql = @"
                        INSERT INTO BookingOtherCharges
                            (BookingId, OtherChargeId, Qty, Rate, Note, GSTAmount, CGSTAmount, SGSTAmount, CreatedDate, CreatedBy)
                        VALUES
                            (@BookingId, @OtherChargeId, @Qty, @Rate, @Note, @GSTAmount, @CGSTAmount, @SGSTAmount, SYSUTCDATETIME(), @CreatedBy);";

                    await _connection.ExecuteAsync(insertSql, new
                    {
                        BookingId = bookingId,
                        row.OtherChargeId,
                        row.Qty,
                        row.Rate,
                        row.Note,
                        row.GSTAmount,
                        row.CGSTAmount,
                        row.SGSTAmount,
                        CreatedBy = performedBy
                    }, tx);
                }
            }

            if (incomingIds.Count == 0)
            {
                await _connection.ExecuteAsync(
                    "DELETE FROM BookingOtherCharges WHERE BookingId = @BookingId",
                    new { BookingId = bookingId },
                    tx);
            }
            else
            {
                await _connection.ExecuteAsync(
                    "DELETE FROM BookingOtherCharges WHERE BookingId = @BookingId AND OtherChargeId NOT IN @Ids",
                    new { BookingId = bookingId, Ids = incomingIds },
                    tx);
            }

            tx.Commit();
        }
    }
}
