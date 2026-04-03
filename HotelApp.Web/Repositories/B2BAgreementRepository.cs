using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class B2BAgreementRepository : IB2BAgreementRepository
    {
        private readonly IDbConnection _connection;

        public B2BAgreementRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<B2BAgreement>> GetByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT a.Id,
                       a.AgreementCode,
                       a.AgreementName,
                       a.ContractReference,
                       a.AgreementType,
                       a.EffectiveFrom,
                       a.EffectiveTo,
                       a.BillingType,
                       a.CreditDays,
                       a.BillingCycle,
                       a.PaymentTerms,
                       a.SecurityDepositAmount,
                       a.CreditLimit,
                       a.RatePlanType,
                       a.DiscountPercent,
                       a.MealPlan,
                       a.TermsConditionId,
                       tc.TermsTitle AS TermsConditionName,
                       a.CancellationPolicyId,
                       cp.PolicyName AS CancellationPolicyName,
                       a.GstSlabId,
                       gs.SlabName AS GstSlabName,
                       a.SeasonalRateNotes,
                       a.BlackoutDatesNotes,
                       a.IsAmendmentAllowed,
                       a.AmendmentChargeAmount,
                       a.IncludesBreakfast,
                       a.IncludesLunch,
                       a.IncludesDinner,
                       a.IncludesLaundry,
                       a.IncludesAirportTransfer,
                       a.IncludesWifi,
                       a.IncludesAccessToLounge,
                       a.ServiceRemarks,
                       a.ApprovalStatus,
                       a.ApprovedByUserId,
                       COALESCE(NULLIF(LTRIM(RTRIM(approvedUser.FullName)), ''), approvedUser.Username) AS ApprovedByName,
                       a.ApprovedDate,
                       a.SignedByName,
                       a.SignedDate,
                       a.SignedDocumentPath,
                       a.AutoRenew,
                       a.RenewalNoticeDays,
                       a.Remarks,
                       a.InternalRemarks,
                       a.BranchID,
                       a.IsActive,
                       a.CreatedDate,
                       a.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(createdUser.FullName)), ''), createdUser.Username) AS CreatedByName,
                       a.UpdatedDate,
                       a.UpdatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(updatedUser.FullName)), ''), updatedUser.Username) AS UpdatedByName,
                       (SELECT COUNT(1) FROM dbo.B2BAgreementRoomRates rr WHERE rr.AgreementId = a.Id AND rr.IsActive = 1) AS RoomRateCount,
                       (SELECT COUNT(1) FROM dbo.B2BClients client WHERE client.AgreementId = a.Id) AS AssignedClientCount
                  FROM dbo.B2BAgreements a
             LEFT JOIN dbo.B2BTermsConditions tc ON tc.Id = a.TermsConditionId
             LEFT JOIN dbo.CancellationPolicies cp ON cp.Id = a.CancellationPolicyId
             LEFT JOIN dbo.GstSlabs gs ON gs.Id = a.GstSlabId
             LEFT JOIN dbo.Users approvedUser ON approvedUser.Id = a.ApprovedByUserId
             LEFT JOIN dbo.Users createdUser ON createdUser.Id = a.CreatedBy
             LEFT JOIN dbo.Users updatedUser ON updatedUser.Id = a.UpdatedBy
                 WHERE a.BranchID = @BranchId
              ORDER BY a.EffectiveFrom DESC, a.AgreementName;";

            return await _connection.QueryAsync<B2BAgreement>(sql, new { BranchId = branchId });
        }

        public async Task<B2BAgreement?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT a.Id,
                       a.AgreementCode,
                       a.AgreementName,
                       a.ContractReference,
                       a.AgreementType,
                       a.EffectiveFrom,
                       a.EffectiveTo,
                       a.BillingType,
                       a.CreditDays,
                       a.BillingCycle,
                       a.PaymentTerms,
                       a.SecurityDepositAmount,
                       a.CreditLimit,
                       a.RatePlanType,
                       a.DiscountPercent,
                       a.MealPlan,
                       a.TermsConditionId,
                       tc.TermsTitle AS TermsConditionName,
                       a.CancellationPolicyId,
                       cp.PolicyName AS CancellationPolicyName,
                       a.GstSlabId,
                       gs.SlabName AS GstSlabName,
                       a.SeasonalRateNotes,
                       a.BlackoutDatesNotes,
                       a.IsAmendmentAllowed,
                       a.AmendmentChargeAmount,
                       a.IncludesBreakfast,
                       a.IncludesLunch,
                       a.IncludesDinner,
                       a.IncludesLaundry,
                       a.IncludesAirportTransfer,
                       a.IncludesWifi,
                       a.IncludesAccessToLounge,
                       a.ServiceRemarks,
                       a.ApprovalStatus,
                       a.ApprovedByUserId,
                       COALESCE(NULLIF(LTRIM(RTRIM(approvedUser.FullName)), ''), approvedUser.Username) AS ApprovedByName,
                       a.ApprovedDate,
                       a.SignedByName,
                       a.SignedDate,
                       a.SignedDocumentPath,
                       a.AutoRenew,
                       a.RenewalNoticeDays,
                       a.Remarks,
                       a.InternalRemarks,
                       a.BranchID,
                       a.IsActive,
                       a.CreatedDate,
                       a.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(createdUser.FullName)), ''), createdUser.Username) AS CreatedByName,
                       a.UpdatedDate,
                       a.UpdatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(updatedUser.FullName)), ''), updatedUser.Username) AS UpdatedByName,
                       (SELECT COUNT(1) FROM dbo.B2BAgreementRoomRates rr WHERE rr.AgreementId = a.Id AND rr.IsActive = 1) AS RoomRateCount,
                       (SELECT COUNT(1) FROM dbo.B2BClients client WHERE client.AgreementId = a.Id) AS AssignedClientCount
                  FROM dbo.B2BAgreements a
             LEFT JOIN dbo.B2BTermsConditions tc ON tc.Id = a.TermsConditionId
             LEFT JOIN dbo.CancellationPolicies cp ON cp.Id = a.CancellationPolicyId
             LEFT JOIN dbo.GstSlabs gs ON gs.Id = a.GstSlabId
             LEFT JOIN dbo.Users approvedUser ON approvedUser.Id = a.ApprovedByUserId
             LEFT JOIN dbo.Users createdUser ON createdUser.Id = a.CreatedBy
             LEFT JOIN dbo.Users updatedUser ON updatedUser.Id = a.UpdatedBy
                 WHERE a.Id = @Id;

                SELECT rr.Id,
                       rr.AgreementId,
                       rr.RoomTypeId,
                       rt.TypeName AS RoomTypeName,
                       rr.SeasonLabel,
                       rr.ValidFrom,
                       rr.ValidTo,
                       rr.BaseRate,
                       rr.ContractRate,
                       rr.ExtraPaxRate,
                       rr.MealPlan,
                       rr.GstSlabId,
                       gs.SlabName AS GstSlabName,
                       rr.Remarks,
                       rr.IsActive
                  FROM dbo.B2BAgreementRoomRates rr
                  JOIN dbo.RoomTypes rt ON rt.Id = rr.RoomTypeId
             LEFT JOIN dbo.GstSlabs gs ON gs.Id = rr.GstSlabId
                 WHERE rr.AgreementId = @Id
              ORDER BY rr.ValidFrom, rt.TypeName;";

            using var multi = await _connection.QueryMultipleAsync(sql, new { Id = id });
            var agreement = await multi.ReadFirstOrDefaultAsync<B2BAgreement>();
            if (agreement == null)
            {
                return null;
            }

            agreement.RoomRates = (await multi.ReadAsync<B2BAgreementRoomRate>()).ToList();
            return agreement;
        }

        public async Task<int> CreateAsync(B2BAgreement agreement)
        {
            const string sql = @"
                INSERT INTO dbo.B2BAgreements
                    (AgreementCode, AgreementName, ContractReference, AgreementType, EffectiveFrom, EffectiveTo,
                     BillingType, CreditDays, BillingCycle, PaymentTerms, SecurityDepositAmount, CreditLimit,
                     RatePlanType, DiscountPercent, MealPlan, TermsConditionId, CancellationPolicyId, GstSlabId,
                     SeasonalRateNotes, BlackoutDatesNotes, IsAmendmentAllowed, AmendmentChargeAmount,
                     IncludesBreakfast, IncludesLunch, IncludesDinner, IncludesLaundry, IncludesAirportTransfer,
                     IncludesWifi, IncludesAccessToLounge, ServiceRemarks, ApprovalStatus, ApprovedByUserId,
                     ApprovedDate, SignedByName, SignedDate, SignedDocumentPath, AutoRenew, RenewalNoticeDays,
                     Remarks, InternalRemarks, BranchID, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@AgreementCode, @AgreementName, @ContractReference, @AgreementType, @EffectiveFrom, @EffectiveTo,
                     @BillingType, @CreditDays, @BillingCycle, @PaymentTerms, @SecurityDepositAmount, @CreditLimit,
                     @RatePlanType, @DiscountPercent, @MealPlan, @TermsConditionId, @CancellationPolicyId, @GstSlabId,
                     @SeasonalRateNotes, @BlackoutDatesNotes, @IsAmendmentAllowed, @AmendmentChargeAmount,
                     @IncludesBreakfast, @IncludesLunch, @IncludesDinner, @IncludesLaundry, @IncludesAirportTransfer,
                     @IncludesWifi, @IncludesAccessToLounge, @ServiceRemarks, @ApprovalStatus, @ApprovedByUserId,
                     @ApprovedDate, @SignedByName, @SignedDate, @SignedDocumentPath, @AutoRenew, @RenewalNoticeDays,
                     @Remarks, @InternalRemarks, @BranchID, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            var shouldCloseConnection = _connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                _connection.Open();
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                var agreementId = await _connection.ExecuteScalarAsync<int>(sql, agreement, transaction);
                await InsertRoomRatesAsync(agreementId, agreement.RoomRates, agreement.CreatedBy, transaction);
                transaction.Commit();
                return agreementId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (shouldCloseConnection && _connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
            }
        }

        public async Task<bool> UpdateAsync(B2BAgreement agreement)
        {
            const string sql = @"
                UPDATE dbo.B2BAgreements
                   SET AgreementCode = @AgreementCode,
                       AgreementName = @AgreementName,
                       ContractReference = @ContractReference,
                       AgreementType = @AgreementType,
                       EffectiveFrom = @EffectiveFrom,
                       EffectiveTo = @EffectiveTo,
                       BillingType = @BillingType,
                       CreditDays = @CreditDays,
                       BillingCycle = @BillingCycle,
                       PaymentTerms = @PaymentTerms,
                       SecurityDepositAmount = @SecurityDepositAmount,
                       CreditLimit = @CreditLimit,
                       RatePlanType = @RatePlanType,
                       DiscountPercent = @DiscountPercent,
                       MealPlan = @MealPlan,
                       TermsConditionId = @TermsConditionId,
                       CancellationPolicyId = @CancellationPolicyId,
                       GstSlabId = @GstSlabId,
                       SeasonalRateNotes = @SeasonalRateNotes,
                       BlackoutDatesNotes = @BlackoutDatesNotes,
                       IsAmendmentAllowed = @IsAmendmentAllowed,
                       AmendmentChargeAmount = @AmendmentChargeAmount,
                       IncludesBreakfast = @IncludesBreakfast,
                       IncludesLunch = @IncludesLunch,
                       IncludesDinner = @IncludesDinner,
                       IncludesLaundry = @IncludesLaundry,
                       IncludesAirportTransfer = @IncludesAirportTransfer,
                       IncludesWifi = @IncludesWifi,
                       IncludesAccessToLounge = @IncludesAccessToLounge,
                       ServiceRemarks = @ServiceRemarks,
                       ApprovalStatus = @ApprovalStatus,
                       ApprovedByUserId = @ApprovedByUserId,
                       ApprovedDate = @ApprovedDate,
                       SignedByName = @SignedByName,
                       SignedDate = @SignedDate,
                       SignedDocumentPath = @SignedDocumentPath,
                       AutoRenew = @AutoRenew,
                       RenewalNoticeDays = @RenewalNoticeDays,
                       Remarks = @Remarks,
                       InternalRemarks = @InternalRemarks,
                       IsActive = @IsActive,
                       UpdatedDate = SYSUTCDATETIME(),
                       UpdatedBy = @UpdatedBy
                 WHERE Id = @Id;";

            const string deleteRoomRatesSql = @"DELETE FROM dbo.B2BAgreementRoomRates WHERE AgreementId = @AgreementId;";

            var shouldCloseConnection = _connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                _connection.Open();
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                var updated = await _connection.ExecuteAsync(sql, agreement, transaction) > 0;
                await _connection.ExecuteAsync(deleteRoomRatesSql, new { AgreementId = agreement.Id }, transaction);
                await InsertRoomRatesAsync(agreement.Id, agreement.RoomRates, agreement.UpdatedBy, transaction);
                transaction.Commit();
                return updated;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (shouldCloseConnection && _connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
            }
        }

        public async Task<bool> CodeExistsAsync(string agreementCode, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM dbo.B2BAgreements WHERE AgreementCode = @AgreementCode AND BranchID = @BranchId AND Id <> @ExcludeId"
                : "SELECT COUNT(1) FROM dbo.B2BAgreements WHERE AgreementCode = @AgreementCode AND BranchID = @BranchId";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                AgreementCode = agreementCode,
                BranchId = branchId,
                ExcludeId = excludeId
            });

            return count > 0;
        }

        private async Task InsertRoomRatesAsync(int agreementId, IEnumerable<B2BAgreementRoomRate>? roomRates, int? performedBy, IDbTransaction transaction)
        {
            const string sql = @"
                INSERT INTO dbo.B2BAgreementRoomRates
                    (AgreementId, RoomTypeId, SeasonLabel, ValidFrom, ValidTo, BaseRate, ContractRate,
                     ExtraPaxRate, MealPlan, GstSlabId, Remarks, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@AgreementId, @RoomTypeId, @SeasonLabel, @ValidFrom, @ValidTo, @BaseRate, @ContractRate,
                     @ExtraPaxRate, @MealPlan, @GstSlabId, @Remarks, @IsActive, SYSUTCDATETIME(), @CreatedBy);";

            if (roomRates == null)
            {
                return;
            }

            foreach (var row in roomRates)
            {
                await _connection.ExecuteAsync(sql, new
                {
                    AgreementId = agreementId,
                    row.RoomTypeId,
                    row.SeasonLabel,
                    row.ValidFrom,
                    row.ValidTo,
                    row.BaseRate,
                    row.ContractRate,
                    row.ExtraPaxRate,
                    row.MealPlan,
                    row.GstSlabId,
                    row.Remarks,
                    row.IsActive,
                    CreatedBy = performedBy
                }, transaction);
            }
        }
    }
}