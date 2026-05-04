namespace HotelApp.Web.ViewModels;

public sealed class B2BEInvoiceDashboardRow
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string BookingNo { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string GenerationType { get; set; } = string.Empty;
    public int BranchID { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedBy { get; set; }

    // Booking details
    public string? GuestName { get; set; }
    public string? B2BClientName { get; set; }
    public string? CompanyGstNo { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckOutDate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string? BookingStatus { get; set; }

    // JSON payload for modal viewer
    public string? JsonPayload { get; set; }

    // Portal push tracking
    public string? PushStatus { get; set; }     // null = Not Pushed | PENDING | PUSHED | FAILED
    public DateTime? PushedAt { get; set; }
    public string? PushResponse { get; set; }

    public DateTime EffectiveCheckOutDate => ActualCheckOutDate ?? CheckOutDate;

    public bool IsPushed => string.Equals(PushStatus, "PUSHED", StringComparison.OrdinalIgnoreCase);
    public bool IsFailed => string.Equals(PushStatus, "FAILED", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => string.Equals(PushStatus, "PENDING", StringComparison.OrdinalIgnoreCase);
    public bool IsNotPushed => string.IsNullOrEmpty(PushStatus);
}

public sealed class B2BEInvoiceDashboardViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? GenerationType { get; set; }
    public string? BookingNoSearch { get; set; }
    public string? PushStatusFilter { get; set; }   // null=All | NOT_PUSHED | PUSHED | FAILED

    public List<B2BEInvoiceDashboardRow> Rows { get; set; } = new();

    // Summary
    public int TotalInvoices  => Rows.Count;
    public int ManualCount    => Rows.Count(r => string.Equals(r.GenerationType, "MANUAL", StringComparison.OrdinalIgnoreCase));
    public int AutoCount      => Rows.Count(r => string.Equals(r.GenerationType, "AUTO",   StringComparison.OrdinalIgnoreCase));
    public int PushedCount    => Rows.Count(r => string.Equals(r.PushStatus,     "PUSHED",  StringComparison.OrdinalIgnoreCase));
    public int NotPushedCount => Rows.Count(r => string.IsNullOrEmpty(r.PushStatus));
    public int FailedCount    => Rows.Count(r => string.Equals(r.PushStatus,     "FAILED",  StringComparison.OrdinalIgnoreCase));
    public decimal TotalGrandTotal => Rows.Sum(r => r.GrandTotal);
    public decimal TotalTaxAmount  => Rows.Sum(r => r.TaxAmount);
}
