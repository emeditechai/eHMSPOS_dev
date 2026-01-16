using HotelApp.Web.Models;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Repositories
{
    public interface IAssetManagementRepository
    {
        // Dashboard
        Task<AssetDashboardSummary> GetDashboardSummaryAsync(int branchId, DateTime fromDate, DateTime toDate);

        // Masters
        Task<IEnumerable<AssetDepartment>> GetDepartmentsAsync(int branchId);
        Task<int> CreateDepartmentAsync(AssetDepartment row);
        Task<bool> UpdateDepartmentAsync(AssetDepartment row);

        Task<IEnumerable<AssetUnit>> GetUnitsAsync(int branchId);
        Task<int> CreateUnitAsync(AssetUnit row);
        Task<bool> UpdateUnitAsync(AssetUnit row);

        Task<IEnumerable<AssetMaker>> GetMakersAsync(int branchId);
        Task<int> CreateMakerAsync(AssetMaker row);
        Task<bool> UpdateMakerAsync(AssetMaker row);
        Task<bool> MakerNameExistsAsync(int branchId, string name, int? excludeMakerId = null);

        Task<IEnumerable<AssetItemLookupRow>> GetItemLookupAsync(int branchId);
        Task<AssetItem?> GetItemByIdAsync(int id, int branchId);
        Task<bool> ItemCodeExistsAsync(int branchId, string code, int? excludeItemId = null);
        Task<bool> ItemBarcodeExistsAsync(int branchId, string barcode, int? excludeItemId = null);
        Task<bool> ItemAssetTagExistsAsync(int branchId, string assetTag, int? excludeItemId = null);
        Task<int> CreateItemAsync(AssetItem item);
        Task<bool> UpdateItemAsync(AssetItem item);
        Task SetItemDepartmentsAsync(int itemId, IReadOnlyCollection<int> departmentIds, int? performedBy);

        Task<IEnumerable<AssetConsumableStandard>> GetConsumableStandardsAsync(int branchId);
        Task<int> UpsertConsumableStandardAsync(AssetConsumableStandard row);

        // Rooms lookup
        Task<IEnumerable<RoomLookupRow>> GetRoomsLookupAsync(int branchId);

        // Stock + Movements
        Task<(bool ok, string? errorMessage, int movementId)> CreateMovementAsync(AssetMovement movement);
        Task<IEnumerable<AssetMovementListRow>> GetMovementListAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AssetMovement?> GetMovementByIdAsync(int id, int branchId);

        Task<IEnumerable<AssetStockReportRow>> GetStockReportAsync(int branchId);

        // Allocations
        Task<IEnumerable<AssetAllocation>> GetOpenAllocationsAsync(int branchId, AssetAllocationType? type = null);

        // Damage/Loss
        Task<int> CreateDamageLossAsync(AssetDamageLossRecord record);
        Task<IEnumerable<AssetDamageLossRecord>> GetDamageLossListAsync(int branchId, AssetDamageLossStatus? status = null);
        Task<AssetDamageLossRecord?> GetDamageLossByIdAsync(int id, int branchId);
        Task<bool> ApproveDamageLossAsync(int id, int branchId, int approvedBy);
        Task<int> CreateRecoveryAsync(AssetDamageLossRecovery recovery);
    }
}
