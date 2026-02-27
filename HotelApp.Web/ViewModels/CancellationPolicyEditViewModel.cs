using System.ComponentModel.DataAnnotations;
using HotelApp.Web.Models;

namespace HotelApp.Web.ViewModels;

public sealed class CancellationPolicyEditViewModel
{
    public CancellationPolicyEditViewModel() { }

    public CancellationPolicyEditViewModel(CancellationPolicy policy, IEnumerable<CancellationPolicyRule> rules)
    {
        Id = policy.Id;
        BranchID = policy.BranchID;
        PolicyName = policy.PolicyName;
        BookingSource = policy.BookingSource;
        CustomerType = policy.CustomerType;
        RateType = policy.RateType;
        ValidFrom = policy.ValidFrom;
        ValidTo = policy.ValidTo;
        NoShowRefundAllowed = policy.NoShowRefundAllowed;
        ApprovalRequired = policy.ApprovalRequired;
        GatewayFeeDeductionPercent = policy.GatewayFeeDeductionPercent;
        IsActive = policy.IsActive;

        Rules = rules?.Select(r => new CancellationPolicyRuleEditRow
        {
            MinHoursBeforeCheckIn = r.MinHoursBeforeCheckIn,
            MaxHoursBeforeCheckIn = r.MaxHoursBeforeCheckIn,
            RefundPercent = r.RefundPercent,
            FlatDeduction = r.FlatDeduction,
            GatewayFeeDeductionPercent = r.GatewayFeeDeductionPercent,
            IsActive = r.IsActive,
            SortOrder = r.SortOrder
        }).ToList() ?? new List<CancellationPolicyRuleEditRow>();
    }

    public int Id { get; set; }
    public int BranchID { get; set; }

    [Required]
    [StringLength(150)]
    public string PolicyName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string BookingSource { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string CustomerType { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string RateType { get; set; } = "Standard";

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidTo { get; set; }

    public bool NoShowRefundAllowed { get; set; }
    public bool ApprovalRequired { get; set; }

    [Range(0, 100)]
    public decimal? GatewayFeeDeductionPercent { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CancellationPolicyRuleEditRow> Rules { get; set; } = new();

    public CancellationPolicy ToPolicy() => new()
    {
        Id = Id,
        BranchID = BranchID,
        PolicyName = PolicyName?.Trim() ?? string.Empty,
        BookingSource = BookingSource?.Trim() ?? string.Empty,
        CustomerType = CustomerType?.Trim() ?? string.Empty,
        RateType = RateType?.Trim() ?? "Standard",
        ValidFrom = ValidFrom,
        ValidTo = ValidTo,
        NoShowRefundAllowed = NoShowRefundAllowed,
        ApprovalRequired = ApprovalRequired,
        GatewayFeeDeductionPercent = GatewayFeeDeductionPercent,
        IsActive = IsActive
    };

    public IReadOnlyList<CancellationPolicyRule> ToRules() => Rules
        .Select(r => new CancellationPolicyRule
        {
            MinHoursBeforeCheckIn = r.MinHoursBeforeCheckIn,
            MaxHoursBeforeCheckIn = r.MaxHoursBeforeCheckIn,
            RefundPercent = r.RefundPercent,
            FlatDeduction = r.FlatDeduction,
            GatewayFeeDeductionPercent = r.GatewayFeeDeductionPercent,
            IsActive = r.IsActive,
            SortOrder = r.SortOrder
        })
        .ToList();
}

public sealed class CancellationPolicyRuleEditRow
{
    public int MinHoursBeforeCheckIn { get; set; }
    public int MaxHoursBeforeCheckIn { get; set; }

    [Range(0, 100)]
    public decimal RefundPercent { get; set; }

    [Range(0, 1000000000)]
    public decimal FlatDeduction { get; set; }

    [Range(0, 100)]
    public decimal? GatewayFeeDeductionPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
