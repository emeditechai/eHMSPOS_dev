using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class MailConfigurationRepository : IMailConfigurationRepository
    {
        private readonly IDbConnection _connection;

        public MailConfigurationRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public Task<MailConfiguration?> GetByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT TOP 1
                    Id,
                    BranchID,
                    SmtpHost,
                    SmtpPort,
                    UserName,
                    PasswordProtected,
                    EnableSslTls,
                    FromEmail,
                    FromName,
                    AdminNotificationEmail,
                    IsActive,
                    CreatedDate,
                    CreatedBy,
                    LastModifiedDate,
                    LastModifiedBy
                FROM MailConfigurations
                WHERE BranchID = @BranchId";

            return _connection.QueryFirstOrDefaultAsync<MailConfiguration>(sql, new { BranchId = branchId });
        }

        public Task<int> UpsertAsync(MailConfiguration settings)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM MailConfigurations WHERE BranchID = @BranchID)
                BEGIN
                    UPDATE MailConfigurations
                    SET SmtpHost = @SmtpHost,
                        SmtpPort = @SmtpPort,
                        UserName = @UserName,
                        PasswordProtected = @PasswordProtected,
                        EnableSslTls = @EnableSslTls,
                        FromEmail = @FromEmail,
                        FromName = @FromName,
                        AdminNotificationEmail = @AdminNotificationEmail,
                        IsActive = @IsActive,
                        LastModifiedDate = GETDATE(),
                        LastModifiedBy = @LastModifiedBy
                    WHERE BranchID = @BranchID;

                    SELECT Id FROM MailConfigurations WHERE BranchID = @BranchID;
                END
                ELSE
                BEGIN
                    INSERT INTO MailConfigurations
                        (BranchID, SmtpHost, SmtpPort, UserName, PasswordProtected, EnableSslTls, FromEmail, FromName, AdminNotificationEmail, IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy)
                    VALUES
                        (@BranchID, @SmtpHost, @SmtpPort, @UserName, @PasswordProtected, @EnableSslTls, @FromEmail, @FromName, @AdminNotificationEmail, @IsActive, GETDATE(), @CreatedBy, GETDATE(), @LastModifiedBy);

                    SELECT CAST(SCOPE_IDENTITY() as int);
                END";

            return _connection.ExecuteScalarAsync<int>(sql, new
            {
                settings.BranchID,
                settings.SmtpHost,
                settings.SmtpPort,
                settings.UserName,
                settings.PasswordProtected,
                settings.EnableSslTls,
                settings.FromEmail,
                settings.FromName,
                settings.AdminNotificationEmail,
                settings.IsActive,
                settings.CreatedBy,
                settings.LastModifiedBy
            });
        }
    }
}
