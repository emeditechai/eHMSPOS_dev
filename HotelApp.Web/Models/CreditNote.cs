namespace HotelApp.Web.Models;

/// <summary>
/// Persisted credit note document generated when a refund is processed.
/// </summary>
public class CreditNote
{
    public int Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public int CancellationId { get; set; }
    public int? RefundPaymentId { get; set; }
    public int BranchID { get; set; }
    public string CustomerType { get; set; } = "B2C";

    // Guest / company (denormalized)
    public string GuestName { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyGstNo { get; set; }
    public string? BillingAddress { get; set; }

    // Booking context
    public string? OriginalInvoiceNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public string? RoomType { get; set; }

    // Financials
    public decimal OriginalTotalAmount { get; set; }
    public decimal RefundBaseAmount { get; set; }
    public decimal RefundCGSTAmount { get; set; }
    public decimal RefundSGSTAmount { get; set; }
    public decimal RefundTotalAmount { get; set; }

    // Cancellation
    public string? CancellationReason { get; set; }
    public DateTime CancellationDate { get; set; }

    // Refund payment
    public string? RefundPaymentMethod { get; set; }
    public string? RefundPaymentReference { get; set; }

    // Metadata
    public DateTime GeneratedAt { get; set; }
    public int? GeneratedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// View-model passed to the A5 credit note print view.
/// Combines the credit note document with the live hotel settings.
/// </summary>
public class CreditNoteViewModel
{
    // ── Credit note document ───────────────────────────────────────────────
    public int Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    // ── Hotel details (from HotelSettings) ────────────────────────────────
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string HotelPhone { get; set; } = string.Empty;
    public string HotelEmail { get; set; } = string.Empty;
    public string? HotelGstNo { get; set; }
    public string? HotelLogoPath { get; set; }

    // ── Customer / Booking ─────────────────────────────────────────────────
    public string BookingNumber { get; set; } = string.Empty;
    public string? OriginalInvoiceNumber { get; set; }
    public string CustomerType { get; set; } = "B2C";
    public string GuestName { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyGstNo { get; set; }
    public string? BillingAddress { get; set; }

    // ── Stay details ───────────────────────────────────────────────────────
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    public string? RoomType { get; set; }

    // ── Financial breakdown ────────────────────────────────────────────────
    public decimal OriginalTotalAmount { get; set; }
    public decimal RefundBaseAmount { get; set; }
    public decimal RefundCGSTAmount { get; set; }
    public decimal RefundSGSTAmount { get; set; }
    public decimal RefundTotalAmount { get; set; }
    public decimal RefundTaxAmount => RefundCGSTAmount + RefundSGSTAmount;

    // ── Cancellation context ───────────────────────────────────────────────
    public string? CancellationReason { get; set; }
    public DateTime CancellationDate { get; set; }

    // ── Refund payment ─────────────────────────────────────────────────────
    public string? RefundPaymentMethod { get; set; }
    public string? RefundPaymentReference { get; set; }
}
