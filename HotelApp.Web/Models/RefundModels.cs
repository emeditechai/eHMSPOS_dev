namespace HotelApp.Web.Models;

/// <summary>Row shown in the pending-refund list.</summary>
public class RefundListItem
{
    public int CancellationId { get; set; }
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? RoomType { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal RefundPercent { get; set; }
    public string ApprovalStatus { get; set; } = "None";
    public DateTime CancelledOn { get; set; }
    public string? Reason { get; set; }
    public int DaysSinceCancellation { get; set; }
}

/// <summary>Full detail loaded when a pending refund is selected.</summary>
public class RefundDetailViewModel
{
    public int CancellationId { get; set; }
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }
    public string? RoomType { get; set; }
    public string? RoomNumber { get; set; }

    // Stay dates
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }

    // Financial
    public decimal BookingTotalAmount { get; set; }
    public decimal BookingTaxAmount { get; set; }
    public decimal BookingCGSTAmount { get; set; }
    public decimal BookingBaseAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal RefundAmount { get; set; }

    // GST proportional breakdown on refund amount (informational / credit note)
    public decimal RefundBaseAmount { get; set; }
    public decimal RefundCGSTAmount { get; set; }
    public decimal RefundSGSTAmount { get; set; }
    public decimal RefundTaxAmount => RefundCGSTAmount + RefundSGSTAmount;

    // Cancellation info
    public string ApprovalStatus { get; set; } = "None";
    public string? Reason { get; set; }
    public bool IsOverride { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime CancelledOn { get; set; }
    public int HoursBeforeCheckIn { get; set; }

    // Payment modes available
    public List<string> PaymentMethods { get; set; } = new()
    {
        "Cash", "Card", "UPI", "Bank Transfer", "Cheque", "Online"
    };
}

/// <summary>Posted when staff processes the refund.</summary>
public class ProcessRefundRequest
{
    public int CancellationId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string? Remarks { get; set; }
    public int? BankId { get; set; }
    public string? CardType { get; set; }
    public string? CardLastFourDigits { get; set; }
    public DateTime? ChequeDate { get; set; }
}

/// <summary>Returned by the process-refund endpoint.</summary>
public class ProcessRefundResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string? ReceiptNumber { get; set; }
}
