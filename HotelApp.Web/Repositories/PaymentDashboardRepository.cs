using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public interface IPaymentDashboardRepository
{
    Task<PaymentDashboardData> GetPaymentDashboardDataAsync(int branchId, DateTime fromDate, DateTime toDate);
}

public class PaymentDashboardRepository : IPaymentDashboardRepository
{
    private readonly IDbConnection _dbConnection;

    public PaymentDashboardRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<PaymentDashboardData> GetPaymentDashboardDataAsync(int branchId, DateTime fromDate, DateTime toDate)
    {
        if (_dbConnection.State != ConnectionState.Open)
            _dbConnection.Open();

        using var multi = await _dbConnection.QueryMultipleAsync(
            "sp_GetPaymentDashboardData",
            new { BranchID = branchId, FromDate = fromDate.Date, ToDate = toDate.Date },
            commandType: CommandType.StoredProcedure);

        var summary       = await multi.ReadFirstOrDefaultAsync<PaymentDashboardSummary>() ?? new PaymentDashboardSummary();
        var methods       = (await multi.ReadAsync<PaymentMethodBreakdown>()).ToList();
        var billingHeads  = (await multi.ReadAsync<PaymentBillingHeadBreakdown>()).ToList();
        var details       = (await multi.ReadAsync<PaymentDetail>()).ToList();
        var dailyTrend    = (await multi.ReadAsync<PaymentDailyTrend>()).ToList();

        return new PaymentDashboardData
        {
            Summary             = summary,
            MethodBreakdown     = methods,
            BillingHeadBreakdown = billingHeads,
            RecentPayments      = details.Take(20),
            DailyTrend          = dailyTrend,
            FromDate            = fromDate,
            ToDate              = toDate
        };
    }
}
