using System.Data;
using Dapper;

namespace HotelApp.Web.Repositories
{
    // ── Interface ─────────────────────────────────────────────────────────────

    public interface IBanquetReportsRepository
    {
        Task<BanquetCollectionReportData> GetCollectionRegisterAsync(int branchId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<BanquetGSTLineRow>> GetGSTRegisterAsync(int branchId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<BanquetVenueUtilizationRow>> GetVenueUtilizationAsync(int branchId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<BanquetEventTypePerformanceRow>> GetEventTypePerformanceAsync(int branchId, DateOnly fromDate, DateOnly toDate);
        Task<IEnumerable<BanquetOutstandingRow>> GetOutstandingBalanceAsync(int branchId);
    }

    // ── Data rows returned by stored procedures ───────────────────────────────

    public class BanquetCollectionSummaryRow
    {
        public int TotalReceipts { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    public class BanquetCollectionDailyRow
    {
        public DateTime CollectionDate { get; set; }
        public int ReceiptCount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
    }

    public class BanquetCollectionDetailRow
    {
        public DateTime CollectionDate { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string BilledTo { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventTypeName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsAdvancePayment { get; set; }
        public bool IsRefund { get; set; }
        public string CollectedBy { get; set; } = string.Empty;
    }

    public class BanquetCollectionReportData
    {
        public BanquetCollectionSummaryRow Summary { get; set; } = new();
        public IEnumerable<BanquetCollectionDailyRow> DailyTotals { get; set; } = new List<BanquetCollectionDailyRow>();
        public IEnumerable<BanquetCollectionDetailRow> Details { get; set; } = new List<BanquetCollectionDetailRow>();
    }

    public class BanquetGSTLineRow
    {
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string SupplyType { get; set; } = string.Empty;
        public string LineType { get; set; } = string.Empty;
        public string? SACCode { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal TotalGST { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public decimal TaxCharged { get; set; }
    }

    public class BanquetVenueUtilizationRow
    {
        public string VenueName { get; set; } = string.Empty;
        public string VenueType { get; set; } = string.Empty;
        public int CapacitySeated { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedEvents { get; set; }
        public int CancelledEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal BaseRevenue { get; set; }
        public decimal TotalGST { get; set; }
        public double AvgAttendees { get; set; }
    }

    public class BanquetEventTypePerformanceRow
    {
        public string EventTypeName { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int CompletedEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AvgAttendees { get; set; }
    }

    public class BanquetOutstandingRow
    {
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string? GuestEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int CreditDays { get; set; }
        public int DaysElapsed { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // ── Implementation ────────────────────────────────────────────────────────

    public class BanquetReportsRepository : IBanquetReportsRepository
    {
        private readonly IDbConnection _db;

        public BanquetReportsRepository(IDbConnection db) => _db = db;

        public async Task<BanquetCollectionReportData> GetCollectionRegisterAsync(int branchId, DateOnly fromDate, DateOnly toDate)
        {
            var p = new { BranchID = branchId, FromDate = fromDate.ToDateTime(TimeOnly.MinValue), ToDate = toDate.ToDateTime(TimeOnly.MinValue) };
            using var multi = await _db.QueryMultipleAsync("dbo.sp_GetBanquetCollectionRegister", p, commandType: System.Data.CommandType.StoredProcedure);
            var summary   = await multi.ReadFirstOrDefaultAsync<BanquetCollectionSummaryRow>() ?? new();
            var daily     = await multi.ReadAsync<BanquetCollectionDailyRow>();
            var details   = await multi.ReadAsync<BanquetCollectionDetailRow>();
            return new BanquetCollectionReportData { Summary = summary, DailyTotals = daily, Details = details };
        }

        public async Task<IEnumerable<BanquetGSTLineRow>> GetGSTRegisterAsync(int branchId, DateOnly fromDate, DateOnly toDate)
        {
            var p = new { BranchID = branchId, FromDate = fromDate.ToDateTime(TimeOnly.MinValue), ToDate = toDate.ToDateTime(TimeOnly.MinValue) };
            return await _db.QueryAsync<BanquetGSTLineRow>("dbo.sp_GetBanquetGSTRegister", p, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<BanquetVenueUtilizationRow>> GetVenueUtilizationAsync(int branchId, DateOnly fromDate, DateOnly toDate)
        {
            var p = new { BranchID = branchId, FromDate = fromDate.ToDateTime(TimeOnly.MinValue), ToDate = toDate.ToDateTime(TimeOnly.MinValue) };
            return await _db.QueryAsync<BanquetVenueUtilizationRow>("dbo.sp_GetVenueUtilizationReport", p, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<BanquetEventTypePerformanceRow>> GetEventTypePerformanceAsync(int branchId, DateOnly fromDate, DateOnly toDate)
        {
            var p = new { BranchID = branchId, FromDate = fromDate.ToDateTime(TimeOnly.MinValue), ToDate = toDate.ToDateTime(TimeOnly.MinValue) };
            return await _db.QueryAsync<BanquetEventTypePerformanceRow>("dbo.sp_GetEventTypePerformance", p, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<BanquetOutstandingRow>> GetOutstandingBalanceAsync(int branchId)
        {
            return await _db.QueryAsync<BanquetOutstandingRow>("dbo.sp_GetBanquetOutstandingBalance",
                new { BranchID = branchId }, commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
