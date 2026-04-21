namespace HotelApp.Web.Models;

public class BookingCancellationPreview
{
    public string BookingNumber { get; set; } = string.Empty;
    public int BookingId { get; set; }
    public int BranchID { get; set; }
    public DateTime CheckInAt { get; set; }
    public int HoursBeforeCheckIn { get; set; }
    public string RateType { get; set; } = "Standard";
    public bool IsNoShow { get; set; }
    public bool IsPartial { get; set; }
    public int[]? CancelledRoomLineIds { get; set; }

    public decimal AmountPaid { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal FlatDeduction { get; set; }
    public decimal GatewayFeeDeductionPercent { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal EligibleRefundAmount { get; set; }
    public decimal AdjustedAgainstDueAmount { get; set; }
    public decimal DueAfterAdjustment { get; set; }

    public int? PolicyId { get; set; }
    public string? PolicyName { get; set; }
    public string? PolicySnapshotJson { get; set; }
    /// <summary>Human-readable description of why the refund is what it is.</summary>
    public string? RefundBreakdownNote { get; set; }

    public decimal? ApprovalThreshold { get; set; }
    public string ApprovalStatus { get; set; } = "None";

    /// <summary>Per-line breakdown for partial cancellation preview.</summary>
    public List<PartialCancellationLinePreview>? Lines { get; set; }
}

public class BookingCancellationCommand
{
    public string BookingNumber { get; set; } = string.Empty;
    public string CancellationType { get; set; } = "Staff"; // Guest / Staff / AutoNoShow
    public string? Reason { get; set; }
    public bool IsOverride { get; set; }
    public string? OverrideReason { get; set; }
    public bool IsNoShow { get; set; }
    /// <summary>When set, only these room lines are cancelled (partial). Null = full cancellation.</summary>
    public int[]? RoomLineIds { get; set; }
}

public class BookingCancellationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BookingCancellationPreview? Preview { get; set; }
    /// <summary>True when the booking is currently checked-in and needs an override to force-cancel.</summary>
    public bool RequiresOverride { get; set; }
}

/// <summary>Saved cancellation record from BookingCancellations table — shown on Details page.</summary>
public class BookingCancellationRecord
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal FlatDeduction { get; set; }
    public decimal GatewayFeeDeductionPercent { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsRefunded { get; set; }
    public string ApprovalStatus { get; set; } = "None";
    public string? Reason { get; set; }
    public string CancellationType { get; set; } = "Staff";
    public bool IsOverride { get; set; }
    public string? OverrideReason { get; set; }
    public int? HoursBeforeCheckIn { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsPartial { get; set; }
    public string? CancelledRoomLineIds { get; set; }
}

public class CancellationPolicy
{
    public int Id { get; set; }
    public int BranchID { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string BookingSource { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string RateType { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool NoShowRefundAllowed { get; set; }
    public bool ApprovalRequired { get; set; }
    public decimal? GatewayFeeDeductionPercent { get; set; }
    public bool IsActive { get; set; }
}

public class CancellationPolicyRule
{
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public int MinHoursBeforeCheckIn { get; set; }
    public int MaxHoursBeforeCheckIn { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal FlatDeduction { get; set; }
    public decimal? GatewayFeeDeductionPercent { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Per-line breakdown shown in the partial cancellation preview UI.</summary>
public class PartialCancellationLinePreview
{
    public int RoomLineId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int RequiredRooms { get; set; }
    public decimal LineGrandTotal { get; set; }
    public decimal ProportionalAmountPaid { get; set; }
    public decimal RefundAmount { get; set; }
}
