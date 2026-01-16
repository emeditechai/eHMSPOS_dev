using System.ComponentModel.DataAnnotations;
using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels
{
    public class AssetDashboardSummary
    {
        public int ActiveItemsCount { get; set; }
        public int LowStockItemsCount { get; set; }
        public int NegativeStockItemsCount { get; set; }

        public int MovementsCount { get; set; }
        public int MovementsInCount { get; set; }
        public int MovementsOutCount { get; set; }
        public decimal TotalInQty { get; set; }
        public decimal TotalOutQty { get; set; }

        public int DamageLossCount { get; set; }
        public int DamageLossPendingCount { get; set; }

        public int RecoveriesCount { get; set; }
        public decimal RecoveryAmount { get; set; }
    }

    public class AssetDashboardViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public AssetDashboardSummary Summary { get; set; } = new();

        public List<AssetStockReportRow> LowStockItems { get; set; } = new();
        public List<AssetStockReportRow> NegativeStockItems { get; set; } = new();
        public List<AssetMovementListRow> RecentMovements { get; set; } = new();
    }

    public class AssetItemEditViewModel
    {
        public AssetItem Item { get; set; } = new();
        public List<AssetDepartment> Departments { get; set; } = new();
        public List<AssetUnit> Units { get; set; } = new();
        public List<AssetMaker> Makers { get; set; } = new();
        public List<int> SelectedDepartmentIds { get; set; } = new();
    }

    public class AssetMovementCreateLineViewModel
    {
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public string? SerialNumber { get; set; }
        public string? LineNote { get; set; }
    }

    public class AssetMovementCreateViewModel
    {
        public AssetMovementType MovementType { get; set; }

        [Display(Name = "Booking Number")]
        public string? BookingNumber { get; set; }

        [Display(Name = "Room")]
        public int? RoomId { get; set; }

        [Display(Name = "From Department")]
        public int? FromDepartmentId { get; set; }

        [Display(Name = "To Department")]
        public int? ToDepartmentId { get; set; }

        [Display(Name = "Guest Name")]
        public string? GuestName { get; set; }

        [Display(Name = "Custodian")]
        public string? CustodianName { get; set; }

        public string? Notes { get; set; }

        [Display(Name = "Allow Negative (Admin Override)")]
        public bool AllowNegativeOverride { get; set; }

        public List<AssetMovementCreateLineViewModel> Lines { get; set; } = new();

        // Lookups
        public List<AssetItemLookupRow> Items { get; set; } = new();
        public List<AssetDepartment> Departments { get; set; } = new();
        public List<RoomLookupRow> Rooms { get; set; } = new();
    }

    public record AssetItemLookupRow(
        int Id,
        string Code,
        string Name,
        string UnitName,
        AssetItemCategory Category,
        bool RequiresCustodian,
        bool IsChargeable,
        string? MakerName,
        string? Barcode,
        string? AssetTag);
    public record RoomLookupRow(int Id, string RoomNumber);

    public class AssetDamageLossCreateViewModel
    {
        public AssetIssueType IssueType { get; set; }
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public string Reason { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
        public int? RoomId { get; set; }
        public string? BookingNumber { get; set; }
        public string? GuestName { get; set; }

        // Lookups
        public List<AssetItemLookupRow> Items { get; set; } = new();
        public List<AssetDepartment> Departments { get; set; } = new();
        public List<RoomLookupRow> Rooms { get; set; } = new();
    }

    public class AssetDamageLossApproveViewModel
    {
        public int Id { get; set; }
        public bool Approved { get; set; }
    }

    public class AssetDamageLossRecoveryCreateViewModel
    {
        public int DamageLossId { get; set; }
        public AssetRecoveryType RecoveryType { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        // Bill posting
        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
    }

    public class AssetStockReportRow
    {
        public int ItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OnHandQty { get; set; }
        public decimal? ThresholdQty { get; set; }
        public bool IsLowStock => ThresholdQty.HasValue && OnHandQty < ThresholdQty.Value;
    }

    public class AssetMovementListRow
    {
        public int Id { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public decimal NetQty { get; set; }
        public string? BookingNumber { get; set; }
        public string? GuestName { get; set; }
        public string? FromDepartment { get; set; }
        public string? ToDepartment { get; set; }
        public string? Notes { get; set; }
    }
}
