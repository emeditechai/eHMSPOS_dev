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
