using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class B2BClientRepository : IB2BClientRepository
    {
        private readonly IDbConnection _connection;

        public B2BClientRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<B2BClient>> GetByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT c.Id,
                       c.ClientCode,
                       c.ClientName,
                       c.DisplayName,
                       c.CompanyType,
                      c.AgreementId,
                      a.AgreementCode,
                      a.AgreementName,
                       c.Pan,
                       c.ContactPerson,
                       c.ContactNo,
                       c.CorporateEmail,
                       c.AlternateContact,
                       c.Address,
                       c.AddressLine2,
                       c.City,
                       c.CountryId,
                       country.Name AS CountryName,
                       c.Pincode,
                       c.StateId,
                       s.Name AS StateName,
                       s.Code AS StateCode,
                       c.IsCreditAllowed,
                       c.CreditAmount,
                       c.CreditDays,
                       c.BillingCycle,
                       c.BillingType,
                       c.OutstandingAmount,
                       c.AllowExceedLimit,
                       c.GstNo,
                       c.Cin,
                       c.GstRegistrationType,
                       c.PlaceOfSupplyStateId,
                       pos.Name AS PlaceOfSupplyStateName,
                       c.ReverseCharge,
                       c.EInvoiceApplicable,
                       c.TdsApplicable,
                       c.TdsPercentage,
                       c.Blacklisted,
                       c.Remarks,
                       c.BranchID,
                       c.IsActive,
                       c.CreatedDate,
                       c.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), u.Username) AS CreatedByName,
                       c.UpdatedDate,
                       c.UpdatedBy
                  FROM dbo.B2BClients c
               LEFT JOIN dbo.B2BAgreements a ON a.Id = c.AgreementId
             LEFT JOIN dbo.States s ON s.Id = c.StateId
             LEFT JOIN dbo.Countries country ON country.Id = c.CountryId
             LEFT JOIN dbo.States pos ON pos.Id = c.PlaceOfSupplyStateId
               LEFT JOIN dbo.Users u ON u.Id = c.CreatedBy
                 WHERE c.BranchID = @BranchId
              ORDER BY c.ClientName, c.ClientCode;";

            return await _connection.QueryAsync<B2BClient>(sql, new { BranchId = branchId });
        }

        public async Task<IEnumerable<B2BClient>> GetActiveByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT c.Id,
                       c.ClientCode,
                       c.ClientName,
                       c.DisplayName,
                       c.CompanyType,
                      c.AgreementId,
                      a.AgreementCode,
                      a.AgreementName,
                       c.Pan,
                       c.ContactPerson,
                       c.ContactNo,
                       c.CorporateEmail,
                       c.AlternateContact,
                       c.Address,
                       c.AddressLine2,
                       c.City,
                       c.CountryId,
                       country.Name AS CountryName,
                       c.Pincode,
                       c.StateId,
                       s.Name AS StateName,
                       s.Code AS StateCode,
                       c.IsCreditAllowed,
                       c.CreditAmount,
                       c.CreditDays,
                       c.BillingCycle,
                       c.BillingType,
                       c.OutstandingAmount,
                       c.AllowExceedLimit,
                       c.GstNo,
                       c.Cin,
                       c.GstRegistrationType,
                       c.PlaceOfSupplyStateId,
                       pos.Name AS PlaceOfSupplyStateName,
                       c.ReverseCharge,
                       c.EInvoiceApplicable,
                       c.TdsApplicable,
                       c.TdsPercentage,
                       c.Blacklisted,
                       c.Remarks,
                       c.BranchID,
                       c.IsActive,
                       c.CreatedDate,
                       c.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), u.Username) AS CreatedByName,
                       c.UpdatedDate,
                       c.UpdatedBy
                  FROM dbo.B2BClients c
               LEFT JOIN dbo.B2BAgreements a ON a.Id = c.AgreementId
             LEFT JOIN dbo.States s ON s.Id = c.StateId
               LEFT JOIN dbo.Countries country ON country.Id = c.CountryId
               LEFT JOIN dbo.States pos ON pos.Id = c.PlaceOfSupplyStateId
               LEFT JOIN dbo.Users u ON u.Id = c.CreatedBy
                 WHERE c.BranchID = @BranchId
                   AND c.IsActive = 1
              ORDER BY c.ClientName, c.ClientCode;";

            return await _connection.QueryAsync<B2BClient>(sql, new { BranchId = branchId });
        }

        public async Task<B2BClient?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT c.Id,
                       c.ClientCode,
                       c.ClientName,
                       c.DisplayName,
                       c.CompanyType,
                      c.AgreementId,
                      a.AgreementCode,
                      a.AgreementName,
                       c.Pan,
                       c.ContactPerson,
                       c.ContactNo,
                       c.CorporateEmail,
                       c.AlternateContact,
                       c.Address,
                       c.AddressLine2,
                       c.City,
                       c.CountryId,
                       country.Name AS CountryName,
                       c.Pincode,
                       c.StateId,
                       s.Name AS StateName,
                       s.Code AS StateCode,
                       c.IsCreditAllowed,
                       c.CreditAmount,
                       c.CreditDays,
                       c.BillingCycle,
                       c.BillingType,
                       c.OutstandingAmount,
                       c.AllowExceedLimit,
                       c.GstNo,
                       c.Cin,
                       c.GstRegistrationType,
                       c.PlaceOfSupplyStateId,
                       pos.Name AS PlaceOfSupplyStateName,
                       c.ReverseCharge,
                       c.EInvoiceApplicable,
                       c.TdsApplicable,
                       c.TdsPercentage,
                       c.Blacklisted,
                       c.Remarks,
                       c.BranchID,
                       c.IsActive,
                       c.CreatedDate,
                       c.CreatedBy,
                       COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), u.Username) AS CreatedByName,
                       c.UpdatedDate,
                       c.UpdatedBy
                  FROM dbo.B2BClients c
               LEFT JOIN dbo.B2BAgreements a ON a.Id = c.AgreementId
             LEFT JOIN dbo.States s ON s.Id = c.StateId
             LEFT JOIN dbo.Countries country ON country.Id = c.CountryId
             LEFT JOIN dbo.States pos ON pos.Id = c.PlaceOfSupplyStateId
               LEFT JOIN dbo.Users u ON u.Id = c.CreatedBy
                 WHERE c.Id = @Id;";

            return await _connection.QueryFirstOrDefaultAsync<B2BClient>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(B2BClient client)
        {
            const string sql = @"
                INSERT INTO dbo.B2BClients
                    (ClientCode, ClientName, DisplayName, CompanyType, AgreementId, Pan, ContactPerson, ContactNo, CorporateEmail,
                     AlternateContact, Address, AddressLine2, City, CountryId, Pincode, StateId, IsCreditAllowed,
                     CreditAmount, CreditDays, BillingCycle, BillingType, OutstandingAmount, AllowExceedLimit, GstNo,
                     Cin, GstRegistrationType, PlaceOfSupplyStateId, ReverseCharge, EInvoiceApplicable, TdsApplicable,
                     TdsPercentage, Blacklisted, Remarks, BranchID, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@ClientCode, @ClientName, @DisplayName, @CompanyType, @AgreementId, @Pan, @ContactPerson, @ContactNo, @CorporateEmail,
                     @AlternateContact, @Address, @AddressLine2, @City, @CountryId, @Pincode, @StateId, @IsCreditAllowed,
                     @CreditAmount, @CreditDays, @BillingCycle, @BillingType, @OutstandingAmount, @AllowExceedLimit, @GstNo,
                     @Cin, @GstRegistrationType, @PlaceOfSupplyStateId, @ReverseCharge, @EInvoiceApplicable, @TdsApplicable,
                     @TdsPercentage, @Blacklisted, @Remarks, @BranchID, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            return await _connection.ExecuteScalarAsync<int>(sql, client);
        }

        public async Task<bool> UpdateAsync(B2BClient client)
        {
            const string sql = @"
                UPDATE dbo.B2BClients
                   SET ClientCode = @ClientCode,
                       ClientName = @ClientName,
                       DisplayName = @DisplayName,
                       CompanyType = @CompanyType,
                       AgreementId = @AgreementId,
                       Pan = @Pan,
                       ContactPerson = @ContactPerson,
                       ContactNo = @ContactNo,
                       CorporateEmail = @CorporateEmail,
                       AlternateContact = @AlternateContact,
                       Address = @Address,
                       AddressLine2 = @AddressLine2,
                       City = @City,
                       CountryId = @CountryId,
                       Pincode = @Pincode,
                       StateId = @StateId,
                       IsCreditAllowed = @IsCreditAllowed,
                       CreditAmount = @CreditAmount,
                       CreditDays = @CreditDays,
                       BillingCycle = @BillingCycle,
                       BillingType = @BillingType,
                       OutstandingAmount = @OutstandingAmount,
                       AllowExceedLimit = @AllowExceedLimit,
                       GstNo = @GstNo,
                       Cin = @Cin,
                       GstRegistrationType = @GstRegistrationType,
                       PlaceOfSupplyStateId = @PlaceOfSupplyStateId,
                       ReverseCharge = @ReverseCharge,
                       EInvoiceApplicable = @EInvoiceApplicable,
                       TdsApplicable = @TdsApplicable,
                       TdsPercentage = @TdsPercentage,
                       Blacklisted = @Blacklisted,
                       Remarks = @Remarks,
                       IsActive = @IsActive,
                       UpdatedDate = SYSUTCDATETIME(),
                       UpdatedBy = @UpdatedBy
                 WHERE Id = @Id;";

            return await _connection.ExecuteAsync(sql, client) > 0;
        }

        public async Task<bool> CodeExistsAsync(string clientCode, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM dbo.B2BClients WHERE ClientCode = @ClientCode AND BranchID = @BranchId AND Id <> @ExcludeId"
                : "SELECT COUNT(1) FROM dbo.B2BClients WHERE ClientCode = @ClientCode AND BranchID = @BranchId";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                ClientCode = clientCode,
                BranchId = branchId,
                ExcludeId = excludeId
            });

            return count > 0;
        }
    }
}