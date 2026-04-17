namespace HotelApp.Web.Models;

public class PaymentDashboardSummary
{
    public decimal TotalPayments { get; set; }
    public decimal TotalGST { get; set; }
    public decimal TotalDiscount { get; set; }
    public int PaymentCount { get; set; }
    public decimal AveragePayment { get; set; }
}

public class PaymentMethodBreakdown
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
}

public class PaymentBillingHeadBreakdown
{
    public string BillingHead { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalGST { get; set; }
}

public class PaymentDetail
{
    public int PaymentId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }
    public DateTime PaidOn { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string BillingHead { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal GSTAmount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public decimal BookingBalance { get; set; }
}

public class PaymentDailyTrend
{
    public DateTime PaymentDate { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PaymentDashboardData
{
    public PaymentDashboardSummary Summary { get; set; } = new();
    public IEnumerable<PaymentMethodBreakdown> MethodBreakdown { get; set; } = new List<PaymentMethodBreakdown>();
    public IEnumerable<PaymentBillingHeadBreakdown> BillingHeadBreakdown { get; set; } = new List<PaymentBillingHeadBreakdown>();
    public IEnumerable<PaymentDetail> RecentPayments { get; set; } = new List<PaymentDetail>();
    public IEnumerable<PaymentDailyTrend> DailyTrend { get; set; } = new List<PaymentDailyTrend>();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
