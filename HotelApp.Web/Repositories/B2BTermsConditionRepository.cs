using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class B2BTermsConditionRepository : IB2BTermsConditionRepository
    {
        private readonly IDbConnection _connection;

        public B2BTermsConditionRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<B2BTermsCondition>> GetByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT t.Id,
                       t.TermsCode,
                       t.TermsTitle,
                       t.TermsType,
                       t.CancellationPolicyId,
                       cp.PolicyName AS CancellationPolicyName,
                       t.PaymentTerms,
                       t.RefundPolicy,
                       t.NoShowPolicy,
                       t.AmendmentPolicy,
                       t.CheckInCheckOutPolicy,
                       t.ChildPolicy,
                       t.ExtraBedPolicy,
                       t.BillingInstructions,
                       t.TaxNotes,
                       t.LegalDisclaimer,
                       t.SpecialConditions,
                       t.IsDefault,
                       t.BranchID,
                       t.IsActive,
                       t.CreatedDate,
                       t.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(createdUser.FullName)), ''), createdUser.Username) AS CreatedByName,
                       t.UpdatedDate,
                       t.UpdatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(updatedUser.FullName)), ''), updatedUser.Username) AS UpdatedByName
                  FROM dbo.B2BTermsConditions t
             LEFT JOIN dbo.CancellationPolicies cp ON cp.Id = t.CancellationPolicyId
             LEFT JOIN dbo.Users createdUser ON createdUser.Id = t.CreatedBy
             LEFT JOIN dbo.Users updatedUser ON updatedUser.Id = t.UpdatedBy
                 WHERE t.BranchID = @BranchId
              ORDER BY t.IsDefault DESC, t.TermsTitle;";

            return await _connection.QueryAsync<B2BTermsCondition>(sql, new { BranchId = branchId });
        }

        public async Task<IEnumerable<B2BTermsCondition>> GetActiveByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT t.Id,
                       t.TermsCode,
                       t.TermsTitle,
                       t.TermsType,
                       t.CancellationPolicyId,
                       cp.PolicyName AS CancellationPolicyName,
                       t.PaymentTerms,
                       t.RefundPolicy,
                       t.NoShowPolicy,
                       t.AmendmentPolicy,
                       t.CheckInCheckOutPolicy,
                       t.ChildPolicy,
                       t.ExtraBedPolicy,
                       t.BillingInstructions,
                       t.TaxNotes,
                       t.LegalDisclaimer,
                       t.SpecialConditions,
                       t.IsDefault,
                       t.BranchID,
                       t.IsActive,
                       t.CreatedDate,
                       t.CreatedBy,
                       t.UpdatedDate,
                       t.UpdatedBy
                  FROM dbo.B2BTermsConditions t
             LEFT JOIN dbo.CancellationPolicies cp ON cp.Id = t.CancellationPolicyId
                 WHERE t.BranchID = @BranchId
                   AND t.IsActive = 1
              ORDER BY t.IsDefault DESC, t.TermsTitle;";

            return await _connection.QueryAsync<B2BTermsCondition>(sql, new { BranchId = branchId });
        }

        public async Task<B2BTermsCondition?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT t.Id,
                       t.TermsCode,
                       t.TermsTitle,
                       t.TermsType,
                       t.CancellationPolicyId,
                       cp.PolicyName AS CancellationPolicyName,
                       t.PaymentTerms,
                       t.RefundPolicy,
                       t.NoShowPolicy,
                       t.AmendmentPolicy,
                       t.CheckInCheckOutPolicy,
                       t.ChildPolicy,
                       t.ExtraBedPolicy,
                       t.BillingInstructions,
                       t.TaxNotes,
                       t.LegalDisclaimer,
                       t.SpecialConditions,
                       t.IsDefault,
                       t.BranchID,
                       t.IsActive,
                       t.CreatedDate,
                       t.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(createdUser.FullName)), ''), createdUser.Username) AS CreatedByName,
                       t.UpdatedDate,
                       t.UpdatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(updatedUser.FullName)), ''), updatedUser.Username) AS UpdatedByName
                  FROM dbo.B2BTermsConditions t
             LEFT JOIN dbo.CancellationPolicies cp ON cp.Id = t.CancellationPolicyId
             LEFT JOIN dbo.Users createdUser ON createdUser.Id = t.CreatedBy
             LEFT JOIN dbo.Users updatedUser ON updatedUser.Id = t.UpdatedBy
                 WHERE t.Id = @Id;";

            return await _connection.QueryFirstOrDefaultAsync<B2BTermsCondition>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(B2BTermsCondition termsCondition)
        {
            const string sql = @"
                INSERT INTO dbo.B2BTermsConditions
                    (TermsCode, TermsTitle, TermsType, CancellationPolicyId, PaymentTerms, RefundPolicy,
                     NoShowPolicy, AmendmentPolicy, CheckInCheckOutPolicy, ChildPolicy, ExtraBedPolicy,
                     BillingInstructions, TaxNotes, LegalDisclaimer, SpecialConditions, IsDefault,
                     BranchID, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@TermsCode, @TermsTitle, @TermsType, @CancellationPolicyId, @PaymentTerms, @RefundPolicy,
                     @NoShowPolicy, @AmendmentPolicy, @CheckInCheckOutPolicy, @ChildPolicy, @ExtraBedPolicy,
                     @BillingInstructions, @TaxNotes, @LegalDisclaimer, @SpecialConditions, @IsDefault,
                     @BranchID, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.ExecuteScalarAsync<int>(sql, termsCondition);
        }

        public async Task<bool> UpdateAsync(B2BTermsCondition termsCondition)
        {
            const string sql = @"
                UPDATE dbo.B2BTermsConditions
                   SET TermsCode = @TermsCode,
                       TermsTitle = @TermsTitle,
                       TermsType = @TermsType,
                       CancellationPolicyId = @CancellationPolicyId,
                       PaymentTerms = @PaymentTerms,
                       RefundPolicy = @RefundPolicy,
                       NoShowPolicy = @NoShowPolicy,
                       AmendmentPolicy = @AmendmentPolicy,
                       CheckInCheckOutPolicy = @CheckInCheckOutPolicy,
                       ChildPolicy = @ChildPolicy,
                       ExtraBedPolicy = @ExtraBedPolicy,
                       BillingInstructions = @BillingInstructions,
                       TaxNotes = @TaxNotes,
                       LegalDisclaimer = @LegalDisclaimer,
                       SpecialConditions = @SpecialConditions,
                       IsDefault = @IsDefault,
                       IsActive = @IsActive,
                       UpdatedDate = SYSUTCDATETIME(),
                       UpdatedBy = @UpdatedBy
                 WHERE Id = @Id;";

            return await _connection.ExecuteAsync(sql, termsCondition) > 0;
        }

        public async Task<bool> CodeExistsAsync(string termsCode, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM dbo.B2BTermsConditions WHERE TermsCode = @TermsCode AND BranchID = @BranchId AND Id <> @ExcludeId"
                : "SELECT COUNT(1) FROM dbo.B2BTermsConditions WHERE TermsCode = @TermsCode AND BranchID = @BranchId";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                TermsCode = termsCode,
                BranchId = branchId,
                ExcludeId = excludeId
            });

            return count > 0;
        }
    }
}