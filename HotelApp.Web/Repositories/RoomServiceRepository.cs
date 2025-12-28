using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace HotelApp.Web.Repositories
{
    public sealed class RoomServiceRepository : IRoomServiceRepository
    {
        private readonly IDbConnection _dbConnection;

        public RoomServiceRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private sealed class RoomServicePendingSettlementRawRow
        {
            public int BookingID { get; set; }
            public string? BookingNo { get; set; }
            public string? GuestName { get; set; }
            public string? GuestPhoneNumber { get; set; }
            public int RoomID { get; set; }
            public string? RoomNo { get; set; }
            public int OrderType { get; set; }
            public int OrderID { get; set; }
            public string? OrderNo { get; set; }
            public DateTime? CreatedAt { get; set; }
            public decimal BillAmount { get; set; }
            public decimal GSTAmount { get; set; }
            public decimal CGSTAmount { get; set; }
            public decimal SGSTAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal PayableAmount { get; set; }
            public int BranchID { get; set; }
            public int MenuItemID { get; set; }
            public string? MenuItemName { get; set; }
            public int Quantity { get; set; }
            public decimal Rate { get; set; }
            public decimal ItemAmount { get; set; }
        }

        private static DateTime CoerceSqlDateTime(DateTime? value)
        {
            // SQL Server DATETIME valid range: 1753-01-01 through 9999-12-31
            // Dapper maps NULL datetime to default(DateTime) for non-nullable fields,
            // which can cause SqlDateTime overflow when inserting. Be defensive.
            var date = value ?? DateTime.Now;
            if (date < new DateTime(1753, 1, 1))
            {
                return DateTime.Now;
            }
            return date;
        }

        private sealed class RoomServiceCachedRow
        {
            public int BookingID { get; set; }
            public int RoomID { get; set; }
            public int BranchID { get; set; }
            public int OrderID { get; set; }
            public DateTime OrderDate { get; set; }
            public string? OrderNo { get; set; }
            public string? MenuItem { get; set; }
            public decimal Price { get; set; }
            public int Qty { get; set; }
            public decimal NetAmount { get; set; }
            public decimal CGSTAmount { get; set; }
            public decimal SGSTAmount { get; set; }
            public decimal GSTAmount { get; set; }
        }

        public async Task<IReadOnlyList<RoomServiceSettlementLineRow>> GetPendingSettlementLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        )
        {
            if (bookingId <= 0 || branchId <= 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            var distinctRoomIds = (roomIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            if (distinctRoomIds.Count == 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            var raw = new List<RoomServicePendingSettlementRawRow>();
            foreach (var roomId in distinctRoomIds)
            {
                var rows = await _dbConnection.QueryAsync<RoomServicePendingSettlementRawRow>(
                    "usp_GetRoomServicePendingSettlementDetails",
                    new
                    {
                        BookingID = bookingId,
                        RoomID = roomId,
                        BranchID = branchId
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (rows != null)
                {
                    raw.AddRange(rows);
                }
            }

            if (raw.Count == 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            // Some integrations/stored procedures may return an extra "summary" row with
            // missing OrderNo/MenuItem (and/or OrderID = 0). Ignore those rows.
            raw = raw
                .Where(r =>
                    r.OrderID > 0 &&
                    (!string.IsNullOrWhiteSpace(r.OrderNo) || !string.IsNullOrWhiteSpace(r.MenuItemName)))
                .ToList();

            if (raw.Count == 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            decimal GetLineNet(RoomServicePendingSettlementRawRow r)
            {
                var qty = r.Quantity <= 0 ? 1 : r.Quantity;
                var itemAmount = r.ItemAmount;
                if (itemAmount <= 0)
                {
                    itemAmount = r.Rate * qty;
                }
                return itemAmount;
            }

            // As per requirement: CGST/SGST/GST are OrderNo-wise (order-level totals)
            // and should match the stored procedure output.
            var result = raw.Select(r => new RoomServiceSettlementLineRow
                {
                    OrderId = r.OrderID,
                    OrderDate = CoerceSqlDateTime(r.CreatedAt),
                    OrderNo = (r.OrderNo ?? "-").Trim(),
                    MenuItem = (r.MenuItemName ?? "-").Trim(),
                    Price = Round2(r.Rate),
                    Qty = r.Quantity <= 0 ? 1 : r.Quantity,
                    NetAmount = Round2(GetLineNet(r)),
                    CGSTAmount = Round2(r.CGSTAmount),
                    SGSTAmount = Round2(r.SGSTAmount),
                    GSTAmount = Round2(r.GSTAmount)
                })
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.OrderNo)
                .ThenBy(x => x.MenuItem)
                .ToList();

            return result;
        }

        public async Task<IReadOnlyList<RoomServiceSettlementLineRow>> GetRoomServiceLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        )
        {
            if (bookingId <= 0 || branchId <= 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            var distinctRoomIds = (roomIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            if (distinctRoomIds.Count == 0)
            {
                return Array.Empty<RoomServiceSettlementLineRow>();
            }

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            const string sql = @"
SELECT
    BookingID,
    RoomID,
    BranchID,
    OrderID,
    OrderDate,
    OrderNo,
    MenuItem,
    Price,
    Qty,
    NetAmount,
    CGSTAmount,
    SGSTAmount,
    GSTAmount
FROM dbo.RoomServices
WHERE BookingID = @BookingID
  AND BranchID = @BranchID
  AND RoomID IN @RoomIDs;";

            var rows = await _dbConnection.QueryAsync<RoomServiceCachedRow>(
                sql,
                new
                {
                    BookingID = bookingId,
                    BranchID = branchId,
                    RoomIDs = distinctRoomIds
                }
            );

            var list = (rows ?? Array.Empty<RoomServiceCachedRow>())
                .Select(r => new RoomServiceSettlementLineRow
                {
                    OrderId = r.OrderID,
                    OrderDate = r.OrderDate,
                    OrderNo = (r.OrderNo ?? "-").Trim(),
                    MenuItem = (r.MenuItem ?? "-").Trim(),
                    Price = Round2(r.Price),
                    Qty = r.Qty <= 0 ? 1 : r.Qty,
                    NetAmount = Round2(r.NetAmount),
                    CGSTAmount = Round2(r.CGSTAmount),
                    SGSTAmount = Round2(r.SGSTAmount),
                    GSTAmount = Round2(r.GSTAmount)
                })
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.OrderNo)
                .ThenBy(x => x.MenuItem)
                .ToList();

            return list;
        }

        public async Task<int> SyncRoomServiceLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        )
        {
            if (bookingId <= 0 || branchId <= 0)
            {
                return 0;
            }

            var distinctRoomIds = (roomIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
            if (distinctRoomIds.Count == 0)
            {
                return 0;
            }

            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var tx = _dbConnection.BeginTransaction();
            try
            {
                var totalInserted = 0;

                foreach (var roomId in distinctRoomIds)
                {
                    // Always delete the cached snapshot for this booking+room+branch before inserting.
                    await _dbConnection.ExecuteAsync(
                        "DELETE FROM dbo.RoomServices WHERE BookingID = @BookingID AND RoomID = @RoomID AND BranchID = @BranchID",
                        new
                        {
                            BookingID = bookingId,
                            RoomID = roomId,
                            BranchID = branchId
                        },
                        transaction: tx
                    );

                    var rows = await _dbConnection.QueryAsync<RoomServicePendingSettlementRawRow>(
                        "usp_GetRoomServicePendingSettlementDetails",
                        new
                        {
                            BookingID = bookingId,
                            RoomID = roomId,
                            BranchID = branchId
                        },
                        transaction: tx,
                        commandType: CommandType.StoredProcedure
                    );

                    var rawList = (rows ?? Array.Empty<RoomServicePendingSettlementRawRow>()).ToList();
                    if (rawList.Count == 0)
                    {
                        continue;
                    }

                    // Filter out invalid/summary rows (missing OrderNo/MenuItem and/or OrderID).
                    rawList = rawList
                        .Where(r =>
                            r.OrderID > 0 &&
                            (!string.IsNullOrWhiteSpace(r.OrderNo) || !string.IsNullOrWhiteSpace(r.MenuItemName)))
                        .ToList();

                    if (rawList.Count == 0)
                    {
                        continue;
                    }

                    decimal GetLineNet(RoomServicePendingSettlementRawRow r)
                    {
                        var qty = r.Quantity <= 0 ? 1 : r.Quantity;
                        var itemAmount = r.ItemAmount;
                        if (itemAmount <= 0)
                        {
                            itemAmount = r.Rate * qty;
                        }
                        return itemAmount;
                    }

                    var insertRows = rawList.Select(r => new
                    {
                        BookingID = bookingId,
                        RoomID = roomId,
                        BranchID = branchId,
                        OrderID = r.OrderID,
                        OrderDate = CoerceSqlDateTime(r.CreatedAt),
                        OrderNo = (r.OrderNo ?? "-").Trim(),
                        MenuItem = (r.MenuItemName ?? "-").Trim(),
                        Price = Round2(r.Rate),
                        Qty = r.Quantity <= 0 ? 1 : r.Quantity,
                        NetAmount = Round2(GetLineNet(r)),
                        CGSTAmount = Round2(r.CGSTAmount),
                        SGSTAmount = Round2(r.SGSTAmount),
                        GSTAmount = Round2(r.GSTAmount)
                    });

                    const string insertSql = @"
INSERT INTO dbo.RoomServices
(
    BookingID,
    RoomID,
    BranchID,
    OrderID,
    OrderDate,
    OrderNo,
    MenuItem,
    Price,
    Qty,
    NetAmount,
    CGSTAmount,
    SGSTAmount,
    GSTAmount
)
VALUES
(
    @BookingID,
    @RoomID,
    @BranchID,
    @OrderID,
    @OrderDate,
    @OrderNo,
    @MenuItem,
    @Price,
    @Qty,
    @NetAmount,
    @CGSTAmount,
    @SGSTAmount,
    @GSTAmount
);";

                    var inserted = await _dbConnection.ExecuteAsync(insertSql, insertRows, transaction: tx);
                    totalInserted += inserted;
                }

                tx.Commit();
                return totalInserted;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<decimal> GetPendingSettlementGrandTotalAsync(int bookingId, IEnumerable<int> roomIds, int branchId)
        {
            var lines = await GetPendingSettlementLinesAsync(bookingId, roomIds, branchId);

            if (lines.Count == 0)
            {
                return 0m;
            }

            // Net is line-level; GST is order-level and is repeated across items.
            // So compute GST by distinct order.
            var netTotal = lines.Sum(x => x.NetAmount);

            var gstTotal = lines
                .GroupBy(x => x.OrderId > 0 ? $"oid:{x.OrderId}" : $"ono:{x.OrderNo}")
                .Sum(g => g.First().GSTAmount);

            return Round2(netTotal + gstTotal);
        }
    }
}
