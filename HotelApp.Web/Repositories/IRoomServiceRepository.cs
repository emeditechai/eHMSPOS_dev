using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelApp.Web.Repositories
{
    public interface IRoomServiceRepository
    {
        Task<IReadOnlyList<RoomServiceSettlementLineRow>> GetPendingSettlementLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        );

        Task<IReadOnlyList<RoomServiceSettlementLineRow>> GetRoomServiceLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        );

        Task<int> SyncRoomServiceLinesAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        );

        Task<decimal> GetPendingSettlementGrandTotalAsync(
            int bookingId,
            IEnumerable<int> roomIds,
            int branchId
        );

        Task<int> SettleRoomServicesAsync(
            int bookingId,
            int branchId,
            decimal? settleAmountOverride = null
        );
    }

    public sealed class RoomServiceSettlementLineRow
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string MenuItem { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ActualBillAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal GSTAmount { get; set; }

        // Note: GST/CGST/SGST are order-level amounts (as returned by the SP),
        // not per-item allocations.
    }
}
