using HotelApp.Web.Repositories;

namespace HotelApp.Web.ViewModels;

public sealed class BusinessAnalyticsDashboardViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public BusinessAnalyticsDashboardSummaryRow Summary { get; set; } = new();
    public List<BusinessAnalyticsDashboardDailyRow> Daily { get; set; } = new();
    public List<BusinessAnalyticsPaymentMethodRow> PaymentMethods { get; set; } = new();
}
