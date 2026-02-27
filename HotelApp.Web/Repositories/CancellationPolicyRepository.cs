using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public sealed class CancellationPolicyRepository : ICancellationPolicyRepository
{
    private readonly IDbConnection _db;

    public CancellationPolicyRepository(IDbConnection db)
    {
        _db = db;
    }

    private void EnsureOpen()
    {
        if (_db.State != ConnectionState.Open)
        {
            _db.Open();
        }
    }

    public async Task<IReadOnlyList<CancellationPolicy>> GetByBranchAsync(int branchId)
    {
        EnsureOpen();
        const string sql = @"
            SELECT *
            FROM CancellationPolicies
            WHERE BranchID = @BranchID
            ORDER BY IsActive DESC, PolicyName, Id DESC";

        return (await _db.QueryAsync<CancellationPolicy>(sql, new { BranchID = branchId })).ToList();
    }

    public async Task<CancellationPolicy?> GetByIdAsync(int id)
    {
        EnsureOpen();
        return await _db.QueryFirstOrDefaultAsync<CancellationPolicy>(
            "SELECT TOP 1 * FROM CancellationPolicies WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IReadOnlyList<CancellationPolicyRule>> GetRulesAsync(int policyId)
    {
        EnsureOpen();
        const string sql = @"
            SELECT *
            FROM CancellationPolicyRules
            WHERE PolicyId = @PolicyId
            ORDER BY SortOrder, MinHoursBeforeCheckIn DESC";

        return (await _db.QueryAsync<CancellationPolicyRule>(sql, new { PolicyId = policyId })).ToList();
    }

    public async Task<int> CreateAsync(CancellationPolicy policy, IReadOnlyList<CancellationPolicyRule> rules, int performedBy)
    {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        EnsureOpen();

        using var tx = _db.BeginTransaction();
        try
        {
            const string insertSql = @"
                INSERT INTO CancellationPolicies (
                    BranchID, PolicyName, BookingSource, CustomerType, RateType,
                    ValidFrom, ValidTo,
                    NoShowRefundAllowed, ApprovalRequired, GatewayFeeDeductionPercent,
                    IsActive, CreatedBy, LastModifiedBy
                )
                VALUES (
                    @BranchID, @PolicyName, @BookingSource, @CustomerType, @RateType,
                    @ValidFrom, @ValidTo,
                    @NoShowRefundAllowed, @ApprovalRequired, @GatewayFeeDeductionPercent,
                    @IsActive, @CreatedBy, @LastModifiedBy
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var policyId = await _db.ExecuteScalarAsync<int>(insertSql, new
            {
                policy.BranchID,
                policy.PolicyName,
                policy.BookingSource,
                policy.CustomerType,
                policy.RateType,
                policy.ValidFrom,
                policy.ValidTo,
                policy.NoShowRefundAllowed,
                policy.ApprovalRequired,
                policy.GatewayFeeDeductionPercent,
                IsActive = policy.IsActive,
                CreatedBy = performedBy,
                LastModifiedBy = performedBy
            }, tx);

            await ReplaceRulesAsync(policyId, rules, performedBy, tx);

            tx.Commit();
            return policyId;
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public async Task<bool> UpdateAsync(CancellationPolicy policy, IReadOnlyList<CancellationPolicyRule> rules, int performedBy)
    {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (policy.Id <= 0) throw new ArgumentException("Invalid policy id", nameof(policy));

        EnsureOpen();
        using var tx = _db.BeginTransaction();
        try
        {
            const string updateSql = @"
                UPDATE CancellationPolicies
                SET
                    PolicyName = @PolicyName,
                    BookingSource = @BookingSource,
                    CustomerType = @CustomerType,
                    RateType = @RateType,
                    ValidFrom = @ValidFrom,
                    ValidTo = @ValidTo,
                    NoShowRefundAllowed = @NoShowRefundAllowed,
                    ApprovalRequired = @ApprovalRequired,
                    GatewayFeeDeductionPercent = @GatewayFeeDeductionPercent,
                    IsActive = @IsActive,
                    LastModifiedDate = SYSUTCDATETIME(),
                    LastModifiedBy = @LastModifiedBy
                WHERE Id = @Id AND BranchID = @BranchID;";

            var rows = await _db.ExecuteAsync(updateSql, new
            {
                policy.Id,
                policy.BranchID,
                policy.PolicyName,
                policy.BookingSource,
                policy.CustomerType,
                policy.RateType,
                policy.ValidFrom,
                policy.ValidTo,
                policy.NoShowRefundAllowed,
                policy.ApprovalRequired,
                policy.GatewayFeeDeductionPercent,
                policy.IsActive,
                LastModifiedBy = performedBy
            }, tx);

            await ReplaceRulesAsync(policy.Id, rules, performedBy, tx);

            tx.Commit();
            return rows > 0;
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public async Task<bool> SetActiveAsync(int policyId, bool isActive, int performedBy)
    {
        EnsureOpen();
        const string sql = @"
            UPDATE CancellationPolicies
            SET IsActive = @IsActive,
                LastModifiedDate = SYSUTCDATETIME(),
                LastModifiedBy = @UserId
            WHERE Id = @Id";

        var rows = await _db.ExecuteAsync(sql, new { Id = policyId, IsActive = isActive, UserId = performedBy });
        return rows > 0;
    }

    public async Task<CancellationPolicy?> GetApplicablePolicyAsync(int branchId, string source, string customerType, string rateType, DateTime checkInDate)
    {
        EnsureOpen();
        const string sql = @"
            SELECT TOP 1 *
            FROM CancellationPolicies
            WHERE BranchID = @BranchID
              AND IsActive = 1
              AND BookingSource = @Source
              AND CustomerType = @CustomerType
              AND RateType = @RateType
              AND (ValidFrom IS NULL OR ValidFrom <= CAST(@CheckInDate AS DATE))
              AND (ValidTo IS NULL OR ValidTo >= CAST(@CheckInDate AS DATE))
            ORDER BY COALESCE(ValidFrom, '1900-01-01') DESC, Id DESC";

        return await _db.QueryFirstOrDefaultAsync<CancellationPolicy>(sql, new
        {
            BranchID = branchId,
            Source = source,
            CustomerType = customerType,
            RateType = rateType,
            CheckInDate = checkInDate
        });
    }

    public async Task<(int? policyId, string? snapshotJson)> GetApplicablePolicySnapshotAsync(int branchId, string source, string customerType, string rateType, DateTime checkInDate)
    {
        var policy = await GetApplicablePolicyAsync(branchId, source, customerType, rateType, checkInDate);
        if (policy == null)
        {
            return (null, null);
        }

        var rules = await GetRulesAsync(policy.Id);

        var snapshot = new
        {
            policy.Id,
            policy.PolicyName,
            policy.BranchID,
            policy.BookingSource,
            policy.CustomerType,
            policy.RateType,
            policy.ValidFrom,
            policy.ValidTo,
            policy.NoShowRefundAllowed,
            policy.ApprovalRequired,
            policy.GatewayFeeDeductionPercent,
            Rules = rules.Select(r => new
            {
                r.MinHoursBeforeCheckIn,
                r.MaxHoursBeforeCheckIn,
                r.RefundPercent,
                r.FlatDeduction,
                r.GatewayFeeDeductionPercent,
                r.SortOrder,
                r.IsActive
            }).ToList()
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        return (policy.Id, json);
    }

    private async Task ReplaceRulesAsync(int policyId, IReadOnlyList<CancellationPolicyRule> rules, int performedBy, IDbTransaction tx)
    {
        await _db.ExecuteAsync(
            "DELETE FROM CancellationPolicyRules WHERE PolicyId = @PolicyId",
            new { PolicyId = policyId },
            tx);

        if (rules == null || rules.Count == 0)
        {
            return;
        }

        const string insertRuleSql = @"
            INSERT INTO CancellationPolicyRules (
                PolicyId, MinHoursBeforeCheckIn, MaxHoursBeforeCheckIn,
                RefundPercent, FlatDeduction, GatewayFeeDeductionPercent,
                IsActive, SortOrder, CreatedBy, LastModifiedBy
            )
            VALUES (
                @PolicyId, @MinHoursBeforeCheckIn, @MaxHoursBeforeCheckIn,
                @RefundPercent, @FlatDeduction, @GatewayFeeDeductionPercent,
                @IsActive, @SortOrder, @CreatedBy, @LastModifiedBy
            );";

        var normalized = rules
            .Select((r, idx) => new
            {
                PolicyId = policyId,
                MinHoursBeforeCheckIn = Math.Max(0, r.MinHoursBeforeCheckIn),
                MaxHoursBeforeCheckIn = Math.Max(0, r.MaxHoursBeforeCheckIn),
                RefundPercent = Math.Max(0m, r.RefundPercent),
                FlatDeduction = Math.Max(0m, r.FlatDeduction),
                GatewayFeeDeductionPercent = r.GatewayFeeDeductionPercent,
                IsActive = r.IsActive,
                SortOrder = r.SortOrder != 0 ? r.SortOrder : (idx + 1) * 10,
                CreatedBy = performedBy,
                LastModifiedBy = performedBy
            })
            .Where(r => r.MaxHoursBeforeCheckIn >= r.MinHoursBeforeCheckIn)
            .ToList();

        if (normalized.Count == 0)
        {
            return;
        }

        await _db.ExecuteAsync(insertRuleSql, normalized, tx);
    }
}
