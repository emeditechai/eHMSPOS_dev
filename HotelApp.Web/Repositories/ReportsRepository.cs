using System.Data;
using Dapper;

namespace HotelApp.Web.Repositories;

public interface IReportsRepository
{
    Task<IEnumerable<RoomPriceDetailRow>> GetRoomPriceDetailsAsync(
        int branchId,
        DateOnly? asOfDate = null,
        int? roomTypeId = null,
        string? roomStatus = null,
        int? floorId = null
    );

    Task<DailyCollectionRegisterReportData> GetDailyCollectionRegisterAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    );

    Task<GstReportData> GetGstReportAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    );

    Task<BusinessAnalyticsDashboardData> GetBusinessAnalyticsDashboardAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    );

    Task<RoomTypePerformanceReportData> GetRoomTypePerformanceReportAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    );

    Task<OutstandingBalanceReportData> GetOutstandingBalanceReportAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    );
}

public sealed class ReportsRepository : IReportsRepository
{
    private readonly IDbConnection _dbConnection;

    public ReportsRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<RoomPriceDetailRow>> GetRoomPriceDetailsAsync(
        int branchId,
        DateOnly? asOfDate = null,
        int? roomTypeId = null,
        string? roomStatus = null,
        int? floorId = null
    )
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        DateTime? asOfDateTime = asOfDate.HasValue
            ? asOfDate.Value.ToDateTime(TimeOnly.MinValue)
            : null;

        var rows = await _dbConnection.QueryAsync<RoomPriceDetailRow>(
            "sp_GetRoomPriceDetailsReport",
            new
            {
                BranchID = branchId,
                AsOfDate = asOfDateTime,
                RoomTypeId = roomTypeId,
                RoomStatus = roomStatus,
                FloorId = floorId
            },
            commandType: CommandType.StoredProcedure
        );

        return rows;
    }

    public async Task<DailyCollectionRegisterReportData> GetDailyCollectionRegisterAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    )
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        var from = fromDate.ToDateTime(TimeOnly.MinValue);
        var to = toDate.ToDateTime(TimeOnly.MinValue);

        using var grid = await _dbConnection.QueryMultipleAsync(
            "sp_GetDailyCollectionRegister",
            new
            {
                BranchID = branchId,
                FromDate = from,
                ToDate = to
            },
            commandType: CommandType.StoredProcedure
        );

        var summary = (await grid.ReadAsync<DailyCollectionRegisterSummaryRow>()).FirstOrDefault()
            ?? new DailyCollectionRegisterSummaryRow();

        var daily = (await grid.ReadAsync<DailyCollectionRegisterDailyRow>()).ToList();
        var rows = (await grid.ReadAsync<DailyCollectionRegisterDetailRow>()).ToList();

        return new DailyCollectionRegisterReportData
        {
            Summary = summary,
            DailyTotals = daily,
            Rows = rows
        };
    }

    public async Task<GstReportData> GetGstReportAsync(int branchId, DateOnly fromDate, DateOnly toDate)
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        var from = fromDate.ToDateTime(TimeOnly.MinValue);
        var to = toDate.ToDateTime(TimeOnly.MinValue);

        using var grid = await _dbConnection.QueryMultipleAsync(
            "sp_GetGstReport",
            new
            {
                BranchID = branchId,
                FromDate = from,
                ToDate = to
            },
            commandType: CommandType.StoredProcedure
        );

        var summary = (await grid.ReadAsync<GstReportSummaryRow>()).FirstOrDefault() ?? new GstReportSummaryRow();
        var rows = (await grid.ReadAsync<GstReportDetailRow>()).ToList();

        return new GstReportData
        {
            Summary = summary,
            Rows = rows
        };
    }

    public async Task<BusinessAnalyticsDashboardData> GetBusinessAnalyticsDashboardAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    )
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        var from = fromDate.ToDateTime(TimeOnly.MinValue);
        var to = toDate.ToDateTime(TimeOnly.MinValue);

        using var grid = await _dbConnection.QueryMultipleAsync(
            "sp_GetBusinessAnalyticsDashboard",
            new
            {
                BranchID = branchId,
                FromDate = from,
                ToDate = to
            },
            commandType: CommandType.StoredProcedure
        );

        var summary = (await grid.ReadAsync<BusinessAnalyticsDashboardSummaryRow>()).FirstOrDefault()
            ?? new BusinessAnalyticsDashboardSummaryRow();
        var daily = (await grid.ReadAsync<BusinessAnalyticsDashboardDailyRow>()).ToList();
        var methods = (await grid.ReadAsync<BusinessAnalyticsPaymentMethodRow>()).ToList();

        return new BusinessAnalyticsDashboardData
        {
            Summary = summary,
            Daily = daily,
            PaymentMethods = methods
        };
    }

    public async Task<RoomTypePerformanceReportData> GetRoomTypePerformanceReportAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    )
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        var from = fromDate.ToDateTime(TimeOnly.MinValue);
        var to = toDate.ToDateTime(TimeOnly.MinValue);

        using var grid = await _dbConnection.QueryMultipleAsync(
            "sp_GetRoomTypePerformanceReport",
            new
            {
                BranchID = branchId,
                FromDate = from,
                ToDate = to
            },
            commandType: CommandType.StoredProcedure
        );

        var summary = (await grid.ReadAsync<RoomTypePerformanceSummaryRow>()).FirstOrDefault()
            ?? new RoomTypePerformanceSummaryRow();
        var rows = (await grid.ReadAsync<RoomTypePerformanceRow>()).ToList();

        return new RoomTypePerformanceReportData
        {
            Summary = summary,
            Rows = rows
        };
    }

    public async Task<OutstandingBalanceReportData> GetOutstandingBalanceReportAsync(
        int branchId,
        DateOnly fromDate,
        DateOnly toDate
    )
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }

        var from = fromDate.ToDateTime(TimeOnly.MinValue);
        var to = toDate.ToDateTime(TimeOnly.MinValue);

        using var grid = await _dbConnection.QueryMultipleAsync(
            "sp_GetOutstandingBalanceReport",
            new
            {
                BranchID = branchId,
                FromDate = from,
                ToDate = to
            },
            commandType: CommandType.StoredProcedure
        );

        var summary = (await grid.ReadAsync<OutstandingBalanceSummaryRow>()).FirstOrDefault()
            ?? new OutstandingBalanceSummaryRow();
        var rows = (await grid.ReadAsync<OutstandingBalanceRow>()).ToList();

        return new OutstandingBalanceReportData
        {
            Summary = summary,
            Rows = rows
        };
    }
}

public sealed class BusinessAnalyticsDashboardData
{
    public BusinessAnalyticsDashboardSummaryRow Summary { get; set; } = new();
    public List<BusinessAnalyticsDashboardDailyRow> Daily { get; set; } = new();
    public List<BusinessAnalyticsPaymentMethodRow> PaymentMethods { get; set; } = new();
}

public sealed class BusinessAnalyticsDashboardSummaryRow
{
    public int TotalDays { get; set; }
    public int TotalRooms { get; set; }
    public int SoldRoomNights { get; set; }
    public int AvailableRoomNights { get; set; }
    public decimal OccupancyPercent { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalBalance { get; set; }
}

public sealed class BusinessAnalyticsDashboardDailyRow
{
    public DateTime ReportDate { get; set; }
    public int TotalBookings { get; set; }
    public int SoldRoomNights { get; set; }
    public decimal OccupancyPercent { get; set; }
    public int ReceiptCount { get; set; }
    public decimal CollectedAmount { get; set; }
}

public sealed class BusinessAnalyticsPaymentMethodRow
{
    public string? PaymentMethod { get; set; }
    public int ReceiptCount { get; set; }
    public decimal CollectedAmount { get; set; }
}

public sealed class RoomTypePerformanceReportData
{
    public RoomTypePerformanceSummaryRow Summary { get; set; } = new();
    public List<RoomTypePerformanceRow> Rows { get; set; } = new();
}

public sealed class RoomTypePerformanceSummaryRow
{
    public int SoldNights { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class RoomTypePerformanceRow
{
    public string? RoomType { get; set; }
    public int SoldNights { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgNightRevenue { get; set; }
}

public sealed class OutstandingBalanceReportData
{
    public OutstandingBalanceSummaryRow Summary { get; set; } = new();
    public List<OutstandingBalanceRow> Rows { get; set; } = new();
}

public sealed class OutstandingBalanceSummaryRow
{
    public int TotalBookings { get; set; }
    public decimal TotalBalance { get; set; }
}

public sealed class OutstandingBalanceRow
{
    public string? BookingNumber { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
}

public sealed class DailyCollectionRegisterReportData
{
    public DailyCollectionRegisterSummaryRow Summary { get; set; } = new();
    public List<DailyCollectionRegisterDailyRow> DailyTotals { get; set; } = new();
    public List<DailyCollectionRegisterDetailRow> Rows { get; set; } = new();
}

public sealed class DailyCollectionRegisterSummaryRow
{
    public int TotalReceipts { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalRoundOff { get; set; }
}

public sealed class DailyCollectionRegisterDailyRow
{
    public DateTime CollectionDate { get; set; }
    public int ReceiptCount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal GSTAmount { get; set; }
}

public sealed class DailyCollectionRegisterDetailRow
{
    public DateTime CollectionDate { get; set; }
    public DateTime PaidOn { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? BookingNumber { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? RoomType { get; set; }
    public string? BillingHead { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BankName { get; set; }
    public string? PaymentReference { get; set; }
    public decimal ReceiptAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal RoundOffAmount { get; set; }
    public bool IsRoundOffApplied { get; set; }
    public string? Status { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class GstReportData
{
    public GstReportSummaryRow Summary { get; set; } = new();
    public List<GstReportDetailRow> Rows { get; set; } = new();
}

public sealed class GstReportSummaryRow
{
    public int TotalBookings { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalTaxableValue { get; set; }
}

public sealed class GstReportDetailRow
{
    public DateTime PaymentDate { get; set; }
    public DateTime PaidOn { get; set; }
    public string? BookingNumber { get; set; }
    public string? GuestName { get; set; }
    public string? RoomType { get; set; }
    public string? BillingHead { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal TaxableValue { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal PaidAmount { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class RoomPriceDetailRow
{
    public int RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public string? RoomStatus { get; set; }
    public int? Floor { get; set; }
    public string? FloorName { get; set; }
    public string? Notes { get; set; }

    public int RoomTypeId { get; set; }
    public string? RoomType { get; set; }
    public string? RoomTypeDescription { get; set; }
    public int? MaxOccupancy { get; set; }
    public string? Amenities { get; set; }
    public decimal? DefaultBaseRate { get; set; }
    public int? RoomTypeCapacity { get; set; }

    public string? CurrentCustomerType { get; set; }
    public string? CurrentSource { get; set; }
    public decimal? CurrentBaseRate { get; set; }
    public decimal? CurrentExtraPaxRate { get; set; }
    public decimal? CurrentTaxPercentage { get; set; }
    public decimal? CurrentCGSTPercentage { get; set; }
    public decimal? CurrentSGSTPercentage { get; set; }
    public DateTime? CurrentRateStartDate { get; set; }
    public DateTime? CurrentRateEndDate { get; set; }
    public bool? IsWeekdayRate { get; set; }
    public string? ApplyDiscount { get; set; }
    public bool? IsDynamicRate { get; set; }
}
