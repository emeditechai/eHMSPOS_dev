using System.Data;
using Dapper;

namespace HotelApp.Web.Services
{
    public interface IDatabaseMigrationService
    {
        Task RunMigrationsAsync();
    }

    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(IDbConnection db, ILogger<DatabaseMigrationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task RunMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Starting database migrations...");

                // Migration 26: Add Gender Column
                await AddGenderColumnAsync();

                _logger.LogInformation("All database migrations completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running database migrations");
                throw;
            }
        }

        private async Task AddGenderColumnAsync()
        {
            try
            {
                // Check if Gender column exists in Guests table
                var guestsColumnExists = await _db.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM sys.columns 
                      WHERE object_id = OBJECT_ID(N'[dbo].[Guests]') AND name = 'Gender'");

                if (guestsColumnExists == 0)
                {
                    await _db.ExecuteAsync("ALTER TABLE [dbo].[Guests] ADD [Gender] NVARCHAR(20) NULL");
                    _logger.LogInformation("✓ Gender column added to Guests table");
                }
                else
                {
                    _logger.LogInformation("✓ Gender column already exists in Guests table");
                }

                // Check if Gender column exists in BookingGuests table
                var bookingGuestsColumnExists = await _db.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM sys.columns 
                      WHERE object_id = OBJECT_ID(N'[dbo].[BookingGuests]') AND name = 'Gender'");

                if (bookingGuestsColumnExists == 0)
                {
                    await _db.ExecuteAsync("ALTER TABLE [dbo].[BookingGuests] ADD [Gender] NVARCHAR(20) NULL");
                    _logger.LogInformation("✓ Gender column added to BookingGuests table");
                }
                else
                {
                    _logger.LogInformation("✓ Gender column already exists in BookingGuests table");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Gender column");
                throw;
            }
        }
    }
}
