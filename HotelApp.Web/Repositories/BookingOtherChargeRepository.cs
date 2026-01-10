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
                SELECT boc.Id,
                       boc.OtherChargeId,
                       boc.ChargeDate,
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
                 ORDER BY boc.ChargeDate, boc.Id";

            return await _connection.QueryAsync<BookingOtherChargeDetailRow>(sql, new { BookingId = bookingId });
        }

        public async Task UpsertForBookingAsync(int bookingId, IReadOnlyList<BookingOtherChargeUpsertRow> rows, int? performedBy)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            using var tx = _connection.BeginTransaction();

            var existingIds = (await _connection.QueryAsync<int>(
                "SELECT Id FROM BookingOtherCharges WHERE BookingId = @BookingId",
                new { BookingId = bookingId },
                tx)).ToHashSet();

            var retainedIds = new List<int>();

            foreach (var row in rows)
            {
                if (row.Id.HasValue && existingIds.Contains(row.Id.Value))
                {
                    const string updateSql = @"
                        UPDATE BookingOtherCharges
                           SET OtherChargeId = @OtherChargeId,
                               ChargeDate = @ChargeDate,
                               Qty = @Qty,
                               Rate = @Rate,
                               Note = @Note,
                               GSTAmount = @GSTAmount,
                               CGSTAmount = @CGSTAmount,
                               SGSTAmount = @SGSTAmount,
                               UpdatedDate = SYSUTCDATETIME(),
                               UpdatedBy = @UpdatedBy
                         WHERE Id = @Id AND BookingId = @BookingId";

                    await _connection.ExecuteAsync(updateSql, new
                    {
                        BookingId = bookingId,
                        row.Id,
                        row.OtherChargeId,
                        ChargeDate = row.ChargeDate.Date,
                        row.Qty,
                        row.Rate,
                        row.Note,
                        row.GSTAmount,
                        row.CGSTAmount,
                        row.SGSTAmount,
                        UpdatedBy = performedBy
                    }, tx);

                    retainedIds.Add(row.Id.Value);
                }
                else
                {
                    const string insertSql = @"
                        INSERT INTO BookingOtherCharges
                            (BookingId, OtherChargeId, ChargeDate, Qty, Rate, Note, GSTAmount, CGSTAmount, SGSTAmount, CreatedDate, CreatedBy)
                        VALUES
                            (@BookingId, @OtherChargeId, @ChargeDate, @Qty, @Rate, @Note, @GSTAmount, @CGSTAmount, @SGSTAmount, SYSUTCDATETIME(), @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var newId = await _connection.ExecuteScalarAsync<int>(insertSql, new
                    {
                        BookingId = bookingId,
                        row.OtherChargeId,
                        ChargeDate = row.ChargeDate.Date,
                        row.Qty,
                        row.Rate,
                        row.Note,
                        row.GSTAmount,
                        row.CGSTAmount,
                        row.SGSTAmount,
                        CreatedBy = performedBy
                    }, tx);

                    retainedIds.Add(newId);
                }
            }

            if (retainedIds.Count == 0)
            {
                await _connection.ExecuteAsync(
                    "DELETE FROM BookingOtherCharges WHERE BookingId = @BookingId",
                    new { BookingId = bookingId },
                    tx);
            }
            else
            {
                await _connection.ExecuteAsync(
                    "DELETE FROM BookingOtherCharges WHERE BookingId = @BookingId AND Id NOT IN @Ids",
                    new { BookingId = bookingId, Ids = retainedIds },
                    tx);
            }

            tx.Commit();
        }
    }
}
