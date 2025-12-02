using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class GuestRepository : IGuestRepository
    {
        private readonly IDbConnection _dbConnection;

        public GuestRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<Guest?> GetByPhoneAsync(string phone)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM Guests
                WHERE Phone = @Phone AND IsActive = 1
                ORDER BY LastModifiedDate DESC";

            return await _dbConnection.QueryFirstOrDefaultAsync<Guest>(sql, new { Phone = phone });
        }

        public async Task<Guest?> GetByPhoneAndBranchAsync(string phone, int branchId)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM Guests
                WHERE Phone = @Phone AND BranchID = @BranchId AND IsActive = 1
                ORDER BY LastModifiedDate DESC";

            return await _dbConnection.QueryFirstOrDefaultAsync<Guest>(sql, new { Phone = phone, BranchId = branchId });
        }

        public async Task<int> CreateAsync(Guest guest)
        {
            const string sql = @"
                INSERT INTO Guests (FirstName, LastName, Email, Phone, Address, City, State, Country,
                                   IdentityType, IdentityNumber, DateOfBirth, LoyaltyId, GuestType, 
                                   ParentGuestId, BranchID, IsActive, CreatedDate, LastModifiedDate)
                VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @City, @State, @Country,
                        @IdentityType, @IdentityNumber, @DateOfBirth, @LoyaltyId, @GuestType,
                        @ParentGuestId, @BranchID, @IsActive, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _dbConnection.ExecuteScalarAsync<int>(sql, guest);
        }

        public async Task<bool> UpdateAsync(Guest guest)
        {
            const string sql = @"
                UPDATE Guests
                SET FirstName = @FirstName,
                    LastName = @LastName,
                    Email = @Email,
                    Phone = @Phone,
                    Address = @Address,
                    City = @City,
                    State = @State,
                    Country = @Country,
                    IdentityType = @IdentityType,
                    IdentityNumber = @IdentityNumber,
                    DateOfBirth = @DateOfBirth,
                    LoyaltyId = @LoyaltyId,
                    GuestType = @GuestType,
                    ParentGuestId = @ParentGuestId,
                    BranchID = @BranchID,
                    IsActive = @IsActive,
                    LastModifiedDate = GETDATE()
                WHERE Id = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, guest);
            return affectedRows > 0;
        }

        public async Task<Guest?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM Guests
                WHERE Email = @Email AND IsActive = 1
                ORDER BY LastModifiedDate DESC";

            return await _dbConnection.QueryFirstOrDefaultAsync<Guest>(sql, new { Email = email });
        }

        public async Task<Guest?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Guests WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<Guest>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Guest>> GetChildGuestsAsync(int parentGuestId)
        {
            const string sql = @"
                SELECT *
                FROM Guests
                WHERE ParentGuestId = @ParentGuestId AND IsActive = 1
                ORDER BY GuestType, FirstName";

            return await _dbConnection.QueryAsync<Guest>(sql, new { ParentGuestId = parentGuestId });
        }

        public async Task<Guest?> FindOrCreateGuestAsync(string firstName, string lastName, string email, string phone, string guestType, int? parentGuestId = null)
        {
            // Try to find existing guest by phone (primary identifier)
            Guest? existingGuest = null;
            
            if (!string.IsNullOrWhiteSpace(phone))
            {
                existingGuest = await GetByPhoneAsync(phone);
            }
            
            // If not found by phone, try email
            if (existingGuest == null && !string.IsNullOrWhiteSpace(email))
            {
                existingGuest = await GetByEmailAsync(email);
            }

            if (existingGuest != null)
            {
                // Update existing guest if needed
                existingGuest.FirstName = firstName;
                existingGuest.LastName = lastName;
                existingGuest.Email = email;
                existingGuest.Phone = phone;
                existingGuest.GuestType = guestType;
                existingGuest.ParentGuestId = parentGuestId;
                
                await UpdateAsync(existingGuest);
                return existingGuest;
            }

            // Create new guest
            var newGuest = new Guest
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                GuestType = guestType,
                ParentGuestId = parentGuestId,
                IsActive = true
            };

            var guestId = await CreateAsync(newGuest);
            newGuest.Id = guestId;
            
            return newGuest;
        }

        public async Task<IEnumerable<Guest>> GetAllAsync()
        {
            const string sql = @"
                SELECT *
                FROM Guests
                WHERE IsActive = 1
                ORDER BY LastModifiedDate DESC";

            return await _dbConnection.QueryAsync<Guest>(sql);
        }

        public async Task<IEnumerable<Guest>> GetAllByBranchAsync(int branchId)
        {
            const string sql = @"
                SELECT *
                FROM Guests
                WHERE IsActive = 1 AND BranchID = @BranchId
                ORDER BY LastModifiedDate DESC";

            return await _dbConnection.QueryAsync<Guest>(sql, new { BranchId = branchId });
        }
    }
}
