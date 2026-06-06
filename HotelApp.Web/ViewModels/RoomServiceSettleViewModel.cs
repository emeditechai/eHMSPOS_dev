using System;
using System.Collections.Generic;

namespace HotelApp.Web.ViewModels
{
    public class RoomServiceSettleViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? BookingNo { get; set; }
        public List<RoomServiceSettleBookingRow> Rows { get; set; } = new();

        // NetAmount is a per-item (line-level) field — sum every row directly.
        public decimal TotalNetAmount => GetAllItemsSum(r => r.NetAmount);

        // These are order-level totals stored identically on every item row of the order,
        // so deduplicate by OrderId to avoid double-counting.
        public decimal TotalActualBill => GetUniqueOrderSum(r => r.ActualBillAmount);
        public decimal TotalCGST      => GetUniqueOrderSum(r => r.CGSTAmount);
        public decimal TotalSGST      => GetUniqueOrderSum(r => r.SGSTAmount);
        public decimal TotalGST       => GetUniqueOrderSum(r => r.GSTAmount);

        /// <summary>Sum every item row across all bookings (for per-item fields like NetAmount).</summary>
        private decimal GetAllItemsSum(Func<RoomServiceSettleOrderRow, decimal> selector)
        {
            decimal total = 0m;
            foreach (var booking in Rows)
                foreach (var order in booking.Orders)
                    total += selector(order);
            return total;
        }

        /// <summary>Sum only the first item per OrderId (for order-level fields like ActualBillAmount/GST).</summary>
        private decimal GetUniqueOrderSum(Func<RoomServiceSettleOrderRow, decimal> selector)
        {
            decimal total = 0m;
            foreach (var booking in Rows)
            {
                var seenOrders = new HashSet<int>();
                foreach (var order in booking.Orders)
                {
                    if (seenOrders.Add(order.OrderId))
                    {
                        total += selector(order);
                    }
                }
            }
            return total;
        }
    }

    public class RoomServiceSettleBookingRow
    {
        public int BookingId { get; set; }
        public string BookingNo { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string RoomNo { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public List<RoomServiceSettleOrderRow> Orders { get; set; } = new();

        // Per-booking summary (unique per OrderId)
        public decimal BookingTotalActualBill
        {
            get
            {
                var seenOrders = new HashSet<int>();
                var total = 0m;
                foreach (var order in Orders)
                {
                    if (seenOrders.Add(order.OrderId))
                        total += order.ActualBillAmount;
                }
                return total;
            }
        }

        public decimal BookingTotalGST
        {
            get
            {
                var seenOrders = new HashSet<int>();
                var total = 0m;
                foreach (var order in Orders)
                {
                    if (seenOrders.Add(order.OrderId))
                        total += order.GSTAmount;
                }
                return total;
            }
        }
    }

    public class RoomServiceSettleOrderRow
    {
        public int OrderId { get; set; }
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
        public DateTime OrderDate { get; set; }
        public bool IsSettled { get; set; }
        public DateTime? SettleDate { get; set; }
    }
}
