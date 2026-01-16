using System.ComponentModel.DataAnnotations;

namespace HotelApp.Web.Models
{
    public enum AssetItemCategory
    {
        Asset = 1,
        Reusable = 2,
        Consumable = 3
    }

    public enum AssetMovementType
    {
        OpeningStockIn = 1,
        ReturnIn = 2,
        TransferIn = 3,
        DamageRecoveryIn = 4,

        DepartmentIssueOut = 10,
        RoomAllocationOut = 11,
        GuestIssueOut = 12,
        ConsumableUsageOut = 13,
        TransferOut = 14,

        AutoCheckoutConsumableOut = 20
    }

    public enum AssetAllocationType
    {
        Department = 1,
        Room = 2,
        Guest = 3
    }

    public enum AssetAllocationStatus
    {
        Open = 1,
        Closed = 2
    }

    public enum AssetIssueType
    {
        Damage = 1,
        Loss = 2
    }

    public enum AssetDamageLossStatus
    {
        Pending = 1,
        Approved = 2,
        Recovered = 3,
        Closed = 4
    }

    public enum AssetRecoveryType
    {
        Cash = 1,
        BillPosting = 2,
        Replacement = 3,
        StaffDeduction = 4
    }

    public class AssetDepartment
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class AssetUnit
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Required, StringLength(30)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class AssetMaker
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class AssetItem
    {
        public int Id { get; set; }
        public int BranchID { get; set; }

        [Required, StringLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public AssetItemCategory Category { get; set; } = AssetItemCategory.Asset;

        [Display(Name = "Maker")]
        public int? MakerId { get; set; }

        [Display(Name = "Barcode"), StringLength(60)]
        public string? Barcode { get; set; }

        [Display(Name = "Asset Tag"), StringLength(60)]
        public string? AssetTag { get; set; }

        [Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Display(Name = "Room Eligible")]
        public bool IsRoomEligible { get; set; }

        [Display(Name = "Chargeable")]
        public bool IsChargeable { get; set; }

        [Display(Name = "Threshold Qty")]
        public decimal? ThresholdQty { get; set; }

        [Display(Name = "Requires Custodian")]
        public bool RequiresCustodian { get; set; } = true;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation helpers (filled by queries)
        public string? UnitName { get; set; }
        public string? MakerName { get; set; }
        public List<int> EligibleDepartmentIds { get; set; } = new();
    }

    public class AssetConsumableStandard
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public int ItemId { get; set; }

        [Display(Name = "Per Room/Day")]
        public decimal PerRoomPerDayQty { get; set; }

        [Display(Name = "Per Stay")]
        public decimal PerStayQty { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }

        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
    }

    public class AssetMovement
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public AssetMovementType MovementType { get; set; }
        public DateTime MovementDate { get; set; }

        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public int? RoomId { get; set; }
        public int? FromDepartmentId { get; set; }
        public int? ToDepartmentId { get; set; }
        public string? GuestName { get; set; }
        public string? CustodianName { get; set; }

        public string? Notes { get; set; }
        public bool AllowNegativeOverride { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }

        public List<AssetMovementLine> Lines { get; set; } = new();
    }

    public class AssetMovementLine
    {
        public int Id { get; set; }
        public int MovementId { get; set; }
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public string? SerialNumber { get; set; }
        public string? LineNote { get; set; }

        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? UnitName { get; set; }
    }

    public class AssetAllocation
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public AssetAllocationType AllocationType { get; set; }
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public int? DepartmentId { get; set; }
        public int? RoomId { get; set; }
        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public string? GuestName { get; set; }
        public string CustodianName { get; set; } = string.Empty;
        public bool IsFixed { get; set; }
        public DateTime IssuedOn { get; set; }
        public DateTime? ReturnedOn { get; set; }
        public AssetAllocationStatus Status { get; set; }
        public int? SourceMovementId { get; set; }

        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? DepartmentName { get; set; }
        public string? RoomNumber { get; set; }
    }

    public class AssetDamageLossRecord
    {
        public int Id { get; set; }
        public int BranchID { get; set; }
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public AssetIssueType IssueType { get; set; }
        public string Reason { get; set; } = string.Empty;

        public int? BookingId { get; set; }
        public string? BookingNumber { get; set; }
        public int? RoomId { get; set; }
        public int? DepartmentId { get; set; }
        public string? GuestName { get; set; }

        public AssetDamageLossStatus Status { get; set; }
        public DateTime ReportedOn { get; set; }
        public int? ReportedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }

        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? DepartmentName { get; set; }
        public string? RoomNumber { get; set; }

        public List<AssetDamageLossRecovery> Recoveries { get; set; } = new();
    }

    public class AssetDamageLossRecovery
    {
        public int Id { get; set; }
        public int DamageLossId { get; set; }
        public AssetRecoveryType RecoveryType { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public int? BookingOtherChargeId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
    }
}
