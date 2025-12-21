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
            public DateTime CreatedAt { get; set; }
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
                    OrderDate = r.CreatedAt,
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
