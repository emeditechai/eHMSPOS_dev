using System.Data;
using System.Linq;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IGuestRepository _guestRepository;
        private readonly IHotelSettingsRepository _hotelSettingsRepository;

        public BookingRepository(IDbConnection dbConnection, IGuestRepository guestRepository, IHotelSettingsRepository hotelSettingsRepository)
        {
            _dbConnection = dbConnection;
            _guestRepository = guestRepository;
            _hotelSettingsRepository = hotelSettingsRepository;
        }

        public async Task<BookingQuoteResult?> GetQuoteAsync(BookingQuoteRequest request)
        {
            // Get hotel settings to use check-in/check-out times
            var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(request.BranchID);
            var checkInTime = hotelSettings?.CheckInTime ?? new TimeSpan(14, 0, 0); // Default 2:00 PM
            var checkOutTime = hotelSettings?.CheckOutTime ?? new TimeSpan(12, 0, 0); // Default 12:00 PM
            
            var nights = CalculateNights(request.CheckInDate, request.CheckOutDate, checkInTime, checkOutTime);
            if (nights <= 0)
            {
                return null;
            }

            const string rateSql = @"
                SELECT TOP 1 *
                FROM RateMaster
                WHERE RoomTypeId = @RoomTypeId
                  AND CustomerType = @CustomerType
                  AND Source = @Source
                  AND BranchID = @BranchID
                  AND @CheckInDate >= StartDate
                  AND @CheckOutDate <= EndDate
                  AND IsActive = 1
                ORDER BY StartDate DESC";

            var ratePlan = await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(rateSql, request);

            const string roomTypeSql = "SELECT * FROM RoomTypes WHERE Id = @RoomTypeId AND IsActive = 1";
            var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(roomTypeSql, new { request.RoomTypeId });
            if (roomType == null)
            {
                return null;
            }

            // Primary rate source is RateMaster. RoomType.BaseRate is only a fallback (should be 0 or configured in Rate Master)
            var nightlyBase = ratePlan?.BaseRate ?? roomType.BaseRate;
            var nightlyExtra = ratePlan?.ExtraPaxRate ?? 0;
            var taxPercentage = ratePlan?.TaxPercentage ?? 0;
            
            // If CGST/SGST are 0 or NULL, split the tax percentage equally
            var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
            var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);
            
            var totalGuests = request.Adults + request.Children;
            var extraGuests = Math.Max(0, totalGuests - roomType.MaxOccupancy);

            var baseAmount = nightlyBase * nights;
            var extraAmount = nightlyExtra * extraGuests * nights;
            var totalRoomRate = baseAmount + extraAmount;
            var totalTax = Math.Round(totalRoomRate * (taxPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var totalCGST = Math.Round(totalRoomRate * (cgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var totalSGST = Math.Round(totalRoomRate * (sgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);

            return new BookingQuoteResult
            {
                Nights = nights,
                RatePlanId = ratePlan?.Id,
                BaseRatePerNight = nightlyBase,
                ExtraPaxRatePerNight = nightlyExtra,
                TaxPercentage = taxPercentage,
                CGSTPercentage = cgstPercentage,
                SGSTPercentage = sgstPercentage,
                TotalRoomRate = totalRoomRate,
                TotalTaxAmount = totalTax,
                TotalCGSTAmount = totalCGST,
                TotalSGSTAmount = totalSGST,
                GrandTotal = totalRoomRate + totalTax
            };
        }

        public async Task<Room?> FindAvailableRoomAsync(int roomTypeId, DateTime checkIn, DateTime checkOut)
        {
            const string sql = @"
                SELECT TOP 1 r.*
                FROM Rooms r
                WHERE r.RoomTypeId = @RoomTypeId
                  AND r.IsActive = 1
                  AND NOT EXISTS (
                    SELECT 1
                    FROM BookingRoomNights brn
                    INNER JOIN Bookings b ON b.Id = brn.BookingId
                    WHERE brn.RoomId = r.Id
                      AND b.Status IN ('Pending','Confirmed','CheckedIn')
                      AND brn.StayDate >= @CheckIn
                      AND brn.StayDate < @CheckOut
                  )
                ORDER BY r.RoomNumber";

            return await _dbConnection.QueryFirstOrDefaultAsync<Room>(sql, new { RoomTypeId = roomTypeId, CheckIn = checkIn, CheckOut = checkOut });
        }

        public async Task<BookingCreationResult> CreateBookingAsync(
            Booking booking,
            IEnumerable<BookingGuest> guests,
            IEnumerable<BookingPayment> payments,
            IEnumerable<BookingRoomNight> roomNights)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            const string insertBookingSql = @"
                INSERT INTO Bookings (
                    BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID, CreatedBy,
                    LastModifiedBy)
                VALUES (
                    @BookingNumber, @Status, @PaymentStatus, @Channel, @Source, @CustomerType,
                    @CheckInDate, @CheckOutDate, @Nights, @RoomTypeId, @RoomId, @RatePlanId,
                    @BaseAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @DiscountAmount, @TotalAmount, @DepositAmount,
                    @BalanceAmount, @Adults, @Children, @PrimaryGuestFirstName, @PrimaryGuestLastName,
                    @PrimaryGuestEmail, @PrimaryGuestPhone, @LoyaltyId, @SpecialRequests, @BranchID, @CreatedBy,
                    @LastModifiedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var bookingId = await _dbConnection.ExecuteScalarAsync<int>(insertBookingSql, booking, transaction);

            // Insert/Update guests in Guests table and link to BookingGuests
            Guest? primaryGuest = null;
            const string insertGuestSql = @"
                INSERT INTO BookingGuests (BookingId, FullName, Email, Phone, GuestType, IsPrimary)
                VALUES (@BookingId, @FullName, @Email, @Phone, @GuestType, @IsPrimary);";

            foreach (var bookingGuest in guests)
            {
                // Find or create guest in Guests table
                var nameParts = bookingGuest.FullName.Split(' ', 2);
                var firstName = nameParts.Length > 0 ? nameParts[0] : bookingGuest.FullName;
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                
                var guestType = bookingGuest.IsPrimary ? "Primary" : "Companion";
                var parentGuestId = bookingGuest.IsPrimary ? (int?)null : primaryGuest?.Id;
                
                // Check if guest exists by phone
                const string findGuestSql = "SELECT TOP 1 * FROM Guests WHERE Phone = @Phone AND IsActive = 1 ORDER BY LastModifiedDate DESC";
                var existingGuest = await _dbConnection.QueryFirstOrDefaultAsync<Guest>(findGuestSql, new { Phone = bookingGuest.Phone }, transaction);
                
                if (existingGuest != null)
                {
                    // Update existing guest
                    const string updateGuestSql = @"
                        UPDATE Guests SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                                         GuestType = @GuestType, ParentGuestId = @ParentGuestId, LastModifiedDate = GETDATE()
                        WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateGuestSql, new
                    {
                        Id = existingGuest.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = bookingGuest.Email ?? "",
                        GuestType = guestType,
                        ParentGuestId = parentGuestId
                    }, transaction);
                    
                    if (bookingGuest.IsPrimary)
                    {
                        existingGuest.FirstName = firstName;
                        existingGuest.LastName = lastName;
                        primaryGuest = existingGuest;
                    }
                }
                else
                {
                    // Create new guest
                    const string insertNewGuestSql = @"
                        INSERT INTO Guests (FirstName, LastName, Email, Phone, GuestType, ParentGuestId, IsActive, CreatedDate, LastModifiedDate)
                        VALUES (@FirstName, @LastName, @Email, @Phone, @GuestType, @ParentGuestId, 1, GETDATE(), GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    var newGuestId = await _dbConnection.ExecuteScalarAsync<int>(insertNewGuestSql, new
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = bookingGuest.Email ?? "",
                        Phone = bookingGuest.Phone ?? "",
                        GuestType = guestType,
                        ParentGuestId = parentGuestId
                    }, transaction);
                    
                    if (bookingGuest.IsPrimary)
                    {
                        primaryGuest = new Guest
                        {
                            Id = newGuestId,
                            FirstName = firstName,
                            LastName = lastName
                        };
                    }
                }
                
                // Insert into BookingGuests for this booking
                bookingGuest.BookingId = bookingId;
                await _dbConnection.ExecuteAsync(insertGuestSql, bookingGuest, transaction);
            }

            const string insertPaymentSql = @"
                INSERT INTO BookingPayments (BookingId, Amount, PaymentMethod, PaymentReference, Status, PaidOn, Notes)
                VALUES (@BookingId, @Amount, @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes);";

            foreach (var payment in payments)
            {
                payment.BookingId = bookingId;
                await _dbConnection.ExecuteAsync(insertPaymentSql, payment, transaction);
            }

            const string insertNightSql = @"
                INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status);";

            foreach (var night in roomNights)
            {
                night.BookingId = bookingId;
                await _dbConnection.ExecuteAsync(insertNightSql, night, transaction);
            }

            const string updateRoomStatusSql = "UPDATE Rooms SET Status = 'Reserved', LastModifiedDate = GETDATE() WHERE Id = @RoomId";
            if (booking.RoomId.HasValue)
            {
                await _dbConnection.ExecuteAsync(updateRoomStatusSql, new { booking.RoomId }, transaction);
            }

            transaction.Commit();

            return new BookingCreationResult
            {
                BookingId = bookingId,
                BookingNumber = booking.BookingNumber
            };
        }

        public async Task<IEnumerable<Booking>> GetRecentAsync(int take = 25)
        {
            const string sql = @"
                SELECT TOP (@Take)
                    Id, BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID,
                    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM Bookings
                ORDER BY CreatedDate DESC";

            var bookings = (await _dbConnection.QueryAsync<Booking>(sql, new { Take = take })).ToList();
            await PopulateRelatedCollectionsAsync(bookings);
            return bookings;
        }
        
        public async Task<IEnumerable<Booking>> GetRecentByBranchAsync(int branchId, int take = 25)
        {
            const string sql = @"
                SELECT TOP (@Take)
                    Id, BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID,
                    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM Bookings
                WHERE BranchID = @BranchId
                ORDER BY CreatedDate DESC";

            var bookings = (await _dbConnection.QueryAsync<Booking>(sql, new { BranchId = branchId, Take = take })).ToList();
            await PopulateRelatedCollectionsAsync(bookings);
            return bookings;
        }

        public async Task<IEnumerable<Booking>> GetByBranchAndDateRangeAsync(int branchId, DateTime? fromDate, DateTime? toDate, int take = 100)
        {
            var sql = @"
                SELECT TOP (@Take)
                    Id, BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID,
                    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM Bookings
                WHERE BranchID = @BranchId";

            if (fromDate.HasValue)
            {
                sql += " AND CAST(CheckInDate AS DATE) >= CAST(@FromDate AS DATE)";
            }

            if (toDate.HasValue)
            {
                sql += " AND CAST(CheckInDate AS DATE) <= CAST(@ToDate AS DATE)";
            }

            sql += " ORDER BY CheckInDate DESC, CreatedDate DESC";

            var bookings = (await _dbConnection.QueryAsync<Booking>(sql, new { BranchId = branchId, FromDate = fromDate, ToDate = toDate, Take = take })).ToList();
            await PopulateRelatedCollectionsAsync(bookings);
            return bookings;
        }

        public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber)
        {
            const string sql = @"
                SELECT
                    Id, BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID,
                    CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
                FROM Bookings
                WHERE BookingNumber = @BookingNumber";

            var booking = await _dbConnection.QueryFirstOrDefaultAsync<Booking>(sql, new { BookingNumber = bookingNumber });
            if (booking == null)
            {
                return null;
            }

            await PopulateRelatedCollectionsAsync(new List<Booking> { booking });
            return booking;
        }

        private async Task PopulateRelatedCollectionsAsync(IList<Booking> bookings)
        {
            if (!bookings.Any())
            {
                return;
            }

            var bookingIds = bookings.Select(b => b.Id).ToArray();
            var roomTypeIds = bookings.Select(b => b.RoomTypeId).Distinct().ToArray();
            var roomIds = bookings.Where(b => b.RoomId.HasValue).Select(b => b.RoomId!.Value).Distinct().ToArray();
            var ratePlanIds = bookings.Where(b => b.RatePlanId.HasValue).Select(b => b.RatePlanId!.Value).Distinct().ToArray();

            const string guestSql = "SELECT * FROM BookingGuests WHERE BookingId IN @Ids AND IsActive = 1";
            var guests = await _dbConnection.QueryAsync<BookingGuest>(guestSql, new { Ids = bookingIds });

            const string paymentSql = "SELECT * FROM BookingPayments WHERE BookingId IN @Ids";
            var payments = await _dbConnection.QueryAsync<BookingPayment>(paymentSql, new { Ids = bookingIds });

            const string nightSql = "SELECT * FROM BookingRoomNights WHERE BookingId IN @Ids";
            var nights = await _dbConnection.QueryAsync<BookingRoomNight>(nightSql, new { Ids = bookingIds });

            var roomTypes = roomTypeIds.Any()
                ? await _dbConnection.QueryAsync<RoomType>("SELECT * FROM RoomTypes WHERE Id IN @Ids", new { Ids = roomTypeIds })
                : Enumerable.Empty<RoomType>();

            var rooms = roomIds.Any()
                ? await _dbConnection.QueryAsync<Room>("SELECT * FROM Rooms WHERE Id IN @Ids", new { Ids = roomIds })
                : Enumerable.Empty<Room>();

            var ratePlans = ratePlanIds.Any()
                ? await _dbConnection.QueryAsync<RateMaster>("SELECT * FROM RateMaster WHERE Id IN @Ids", new { Ids = ratePlanIds })
                : Enumerable.Empty<RateMaster>();

            var roomTypeLookup = roomTypes.ToDictionary(rt => rt.Id, rt => rt);
            var roomLookup = rooms.ToDictionary(r => r.Id, r => r);
            var ratePlanLookup = ratePlans.ToDictionary(rp => rp.Id, rp => rp);

            foreach (var booking in bookings)
            {
                booking.Guests = guests.Where(g => g.BookingId == booking.Id).ToList();
                booking.Payments = payments.Where(p => p.BookingId == booking.Id).ToList();
                booking.RoomNights = nights.Where(n => n.BookingId == booking.Id).OrderBy(n => n.StayDate).ToList();

                if (roomTypeLookup.TryGetValue(booking.RoomTypeId, out var roomType))
                {
                    booking.RoomType = roomType;
                }

                if (booking.RoomId.HasValue && roomLookup.TryGetValue(booking.RoomId.Value, out var room))
                {
                    booking.Room = room;
                }

                if (booking.RatePlanId.HasValue && ratePlanLookup.TryGetValue(booking.RatePlanId.Value, out var ratePlan))
                {
                    booking.RatePlan = ratePlan;
                }
            }
        }

        public async Task<int> GetTodayBookingCountAsync()
        {
            var today = DateTime.Today;
            const string sql = @"
                SELECT COUNT(*) 
                FROM Bookings 
                WHERE CAST(CreatedDate AS DATE) = @Today";
            
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { Today = today });
        }

        public async Task<decimal> GetTodayAdvanceAmountAsync()
        {
            var today = DateTime.Today;
            const string sql = @"
                SELECT ISNULL(SUM(DepositAmount), 0)
                FROM Bookings 
                WHERE CAST(CreatedDate AS DATE) = @Today";
            
            return await _dbConnection.ExecuteScalarAsync<decimal>(sql, new { Today = today });
        }

        public async Task<int> GetTodayCheckInCountAsync()
        {
            var today = DateTime.Today;
            const string sql = @"
                SELECT COUNT(*) 
                FROM Bookings 
                WHERE CAST(CheckInDate AS DATE) = @Today 
                AND Status = 'Confirmed'";
            
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { Today = today });
        }

        public async Task<int> GetTodayCheckOutCountAsync()
        {
            var today = DateTime.Today;
            const string sql = @"
                SELECT COUNT(*) 
                FROM Bookings 
                WHERE CAST(CheckOutDate AS DATE) = @Today 
                AND Status IN ('Confirmed', 'CheckedIn')";
            
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { Today = today });
        }

        public async Task<bool> UpdateRoomTypeAsync(string bookingNumber, int newRoomTypeId, decimal baseAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount)
        {
            try
            {
                var booking = await GetByBookingNumberAsync(bookingNumber);
                if (booking == null) return false;

                const string sql = @"
                    UPDATE Bookings
                    SET RoomTypeId = @RoomTypeId,
                        BaseAmount = @BaseAmount,
                        TaxAmount = @TaxAmount,
                        CGSTAmount = @CGSTAmount,
                        SGSTAmount = @SGSTAmount,
                        TotalAmount = @TotalAmount,
                        BalanceAmount = @TotalAmount - ISNULL((SELECT SUM(Amount) FROM BookingPayments WHERE BookingId = Bookings.Id), 0),
                        LastModifiedDate = GETDATE()
                    WHERE BookingNumber = @BookingNumber";

                var rowsAffected = await _dbConnection.ExecuteAsync(sql, new
                {
                    BookingNumber = bookingNumber,
                    RoomTypeId = newRoomTypeId,
                    BaseAmount = baseAmount,
                    TaxAmount = taxAmount,
                    CGSTAmount = cgstAmount,
                    SGSTAmount = sgstAmount,
                    TotalAmount = totalAmount
                });

                if (rowsAffected > 0)
                {
                    await AddAuditLogAsync(
                        booking.Id,
                        bookingNumber,
                        "RoomTypeChange",
                        $"Room type changed",
                        booking.RoomTypeId.ToString(),
                        newRoomTypeId.ToString(),
                        null
                    );
                }

                return rowsAffected > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateActualCheckOutDateAsync(string bookingNumber, DateTime actualCheckOutDate, int performedBy)
        {
            const string sql = @"
                UPDATE Bookings 
                SET ActualCheckOutDate = @ActualCheckOutDate,
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @PerformedBy
                WHERE BookingNumber = @BookingNumber";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new 
            { 
                BookingNumber = bookingNumber, 
                ActualCheckOutDate = actualCheckOutDate,
                PerformedBy = performedBy
            });

            return rowsAffected > 0;
        }

        public async Task<bool> UpdateRoomAssignmentAsync(string bookingNumber, int roomId)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            try
            {
                // Get current room assignment before updating
                const string getCurrentRoomSql = @"
                    SELECT RoomId, Id, CheckInDate, CheckOutDate, TotalAmount, TaxAmount, CGSTAmount, SGSTAmount, Nights
                    FROM Bookings 
                    WHERE BookingNumber = @BookingNumber";

                var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    getCurrentRoomSql,
                    new { BookingNumber = bookingNumber },
                    transaction
                );

                if (booking == null)
                {
                    transaction.Rollback();
                    return false;
                }

                var previousRoomId = booking.RoomId as int?;

                // If there was a previous room assigned, mark it as Available
                if (previousRoomId.HasValue && previousRoomId.Value != roomId)
                {
                    const string updatePreviousRoomSql = @"
                        UPDATE Rooms 
                        SET Status = 'Available',
                            LastModifiedDate = GETUTCDATE()
                        WHERE Id = @RoomId";

                    await _dbConnection.ExecuteAsync(
                        updatePreviousRoomSql,
                        new { RoomId = previousRoomId.Value },
                        transaction
                    );
                }

                // Delete ALL previous BookingRoomNights records for this booking
                const string deleteAllRoomNightsSql = @"
                    DELETE FROM BookingRoomNights 
                    WHERE BookingId = @BookingId";

                await _dbConnection.ExecuteAsync(
                    deleteAllRoomNightsSql,
                    new { BookingId = (int)booking.Id },
                    transaction
                );

                // Update the booking with the new room assignment
                const string updateBookingSql = @"
                    UPDATE Bookings 
                    SET RoomId = @RoomId,
                        LastModifiedDate = GETUTCDATE()
                    WHERE BookingNumber = @BookingNumber";

                var bookingUpdated = await _dbConnection.ExecuteAsync(
                    updateBookingSql,
                    new { RoomId = roomId, BookingNumber = bookingNumber },
                    transaction
                );

                if (bookingUpdated == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Mark the new room as Occupied
                const string updateNewRoomSql = @"
                    UPDATE Rooms 
                    SET Status = 'Occupied',
                        LastModifiedDate = GETUTCDATE()
                    WHERE Id = @RoomId";

                await _dbConnection.ExecuteAsync(
                    updateNewRoomSql,
                    new { RoomId = roomId },
                    transaction
                );

                // Create BookingRoomNights records
                var bookingId = (int)booking.Id;
                var checkInDate = (DateTime)booking.CheckInDate;
                var checkOutDate = (DateTime)booking.CheckOutDate;
                var nights = (int)booking.Nights;
                var totalAmount = (decimal)booking.TotalAmount;
                var totalTax = (decimal)booking.TaxAmount;
                var totalCGST = (decimal)booking.CGSTAmount;
                var totalSGST = (decimal)booking.SGSTAmount;

                var nightlyRoomAmount = Math.Round((totalAmount - totalTax) / nights, 2, MidpointRounding.AwayFromZero);
                var nightlyTax = Math.Round(totalTax / nights, 2, MidpointRounding.AwayFromZero);
                var nightlyCGST = Math.Round(totalCGST / nights, 2, MidpointRounding.AwayFromZero);
                var nightlySGST = Math.Round(totalSGST / nights, 2, MidpointRounding.AwayFromZero);

                const string insertRoomNightSql = @"
                    INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                    VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                for (var date = checkInDate.Date; date < checkOutDate.Date; date = date.AddDays(1))
                {
                    await _dbConnection.ExecuteAsync(
                        insertRoomNightSql,
                        new
                        {
                            BookingId = bookingId,
                            RoomId = roomId,
                            StayDate = date,
                            RateAmount = nightlyRoomAmount,
                            TaxAmount = nightlyTax,
                            CGSTAmount = nightlyCGST,
                            SGSTAmount = nightlySGST,
                            Status = "Reserved"
                        },
                        transaction
                    );
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateBookingDatesAsync(string bookingNumber, DateTime checkInDate, DateTime checkOutDate, int nights, decimal baseAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            try
            {
                // Get current booking details for audit logging
                const string getBookingSql = @"
                    SELECT Id, RoomId, DepositAmount, CheckInDate, CheckOutDate, Nights, BaseAmount, TaxAmount, TotalAmount
                    FROM Bookings 
                    WHERE BookingNumber = @BookingNumber";

                var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    getBookingSql,
                    new { BookingNumber = bookingNumber },
                    transaction
                );

                if (booking == null)
                {
                    transaction.Rollback();
                    return false;
                }

                var bookingId = (int)booking.Id;
                var roomId = booking.RoomId as int?;
                var depositAmount = (decimal)booking.DepositAmount;
                
                // Store old values for audit log
                var oldCheckIn = (DateTime)booking.CheckInDate;
                var oldCheckOut = (DateTime)booking.CheckOutDate;
                var oldNights = (int)booking.Nights;
                var oldBaseAmount = (decimal)booking.BaseAmount;
                var oldTaxAmount = (decimal)booking.TaxAmount;
                var oldTotalAmount = (decimal)booking.TotalAmount;

                // Calculate new balance
                var balanceAmount = totalAmount - depositAmount;

                // Update booking with new dates and amounts
                const string updateBookingSql = @"
                    UPDATE Bookings 
                    SET CheckInDate = @CheckInDate,
                        CheckOutDate = @CheckOutDate,
                        Nights = @Nights,
                        BaseAmount = @BaseAmount,
                        TaxAmount = @TaxAmount,
                        CGSTAmount = @CGSTAmount,
                        SGSTAmount = @SGSTAmount,
                        TotalAmount = @TotalAmount,
                        BalanceAmount = @BalanceAmount,
                        LastModifiedDate = GETUTCDATE()
                    WHERE BookingNumber = @BookingNumber";

                var updated = await _dbConnection.ExecuteAsync(
                    updateBookingSql,
                    new
                    {
                        CheckInDate = checkInDate,
                        CheckOutDate = checkOutDate,
                        Nights = nights,
                        BaseAmount = baseAmount,
                        TaxAmount = taxAmount,
                        CGSTAmount = cgstAmount,
                        SGSTAmount = sgstAmount,
                        TotalAmount = totalAmount,
                        BalanceAmount = balanceAmount,
                        BookingNumber = bookingNumber
                    },
                    transaction
                );

                if (updated == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Delete existing BookingRoomNights records
                const string deleteRoomNightsSql = @"
                    DELETE FROM BookingRoomNights 
                    WHERE BookingId = @BookingId";

                await _dbConnection.ExecuteAsync(
                    deleteRoomNightsSql,
                    new { BookingId = bookingId },
                    transaction
                );

                // Create new BookingRoomNights records if room is assigned
                if (roomId.HasValue)
                {
                    var nightlyRoomAmount = Math.Round(baseAmount / nights, 2, MidpointRounding.AwayFromZero);
                    var nightlyTax = Math.Round(taxAmount / nights, 2, MidpointRounding.AwayFromZero);
                    var nightlyCGST = Math.Round(cgstAmount / nights, 2, MidpointRounding.AwayFromZero);
                    var nightlySGST = Math.Round(sgstAmount / nights, 2, MidpointRounding.AwayFromZero);

                    const string insertRoomNightSql = @"
                        INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                        VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                    for (var date = checkInDate.Date; date < checkOutDate.Date; date = date.AddDays(1))
                    {
                        await _dbConnection.ExecuteAsync(
                            insertRoomNightSql,
                            new
                            {
                                BookingId = bookingId,
                                RoomId = roomId.Value,
                                StayDate = date,
                                RateAmount = nightlyRoomAmount,
                                TaxAmount = nightlyTax,
                                CGSTAmount = nightlyCGST,
                                SGSTAmount = nightlySGST,
                                Status = "Reserved"
                            },
                            transaction
                        );
                    }
                }

                // Log the date change in audit log
                var oldValueStr = $"Check-In: {oldCheckIn:dd/MM/yyyy}, Check-Out: {oldCheckOut:dd/MM/yyyy}, Nights: {oldNights}, Room Total: ₹{oldBaseAmount:N2}, Tax: ₹{oldTaxAmount:N2}, Grand Total: ₹{oldTotalAmount:N2}";
                var newValueStr = $"Check-In: {checkInDate:dd/MM/yyyy}, Check-Out: {checkOutDate:dd/MM/yyyy}, Nights: {nights}, Room Total: ₹{baseAmount:N2}, Tax: ₹{taxAmount:N2}, Grand Total: ₹{totalAmount:N2}";
                
                await AddAuditLogAsync(bookingId, bookingNumber, "DatesChanged", 
                    $"Booking dates changed from {oldCheckIn:dd/MM/yyyy}-{oldCheckOut:dd/MM/yyyy} to {checkInDate:dd/MM/yyyy}-{checkOutDate:dd/MM/yyyy}", 
                    oldValueStr, newValueStr, null, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<BookingAuditLog>> GetAuditLogAsync(int bookingId)
        {
            const string sql = @"
                SELECT Id, BookingId, BookingNumber, ActionType, ActionDescription, 
                       OldValue, NewValue, PerformedBy, PerformedAt
                FROM BookingAuditLog 
                WHERE BookingId = @BookingId
                ORDER BY PerformedAt DESC";

            return await _dbConnection.QueryAsync<BookingAuditLog>(sql, new { BookingId = bookingId });
        }

        public async Task AddAuditLogAsync(int bookingId, string bookingNumber, string actionType, string description, 
            string? oldValue = null, string? newValue = null, int? performedBy = null)
        {
            const string sql = @"
                INSERT INTO BookingAuditLog (BookingId, BookingNumber, ActionType, ActionDescription, OldValue, NewValue, PerformedBy)
                VALUES (@BookingId, @BookingNumber, @ActionType, @Description, @OldValue, @NewValue, @PerformedBy)";

            await _dbConnection.ExecuteAsync(sql, new
            {
                BookingId = bookingId,
                BookingNumber = bookingNumber,
                ActionType = actionType,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                PerformedBy = performedBy
            });
        }

        private async Task AddAuditLogAsync(int bookingId, string bookingNumber, string actionType, string description, 
            string? oldValue, string? newValue, int? performedBy, IDbTransaction transaction)
        {
            const string sql = @"
                INSERT INTO BookingAuditLog (BookingId, BookingNumber, ActionType, ActionDescription, OldValue, NewValue, PerformedBy)
                VALUES (@BookingId, @BookingNumber, @ActionType, @Description, @OldValue, @NewValue, @PerformedBy)";

            await _dbConnection.ExecuteAsync(sql, new
            {
                BookingId = bookingId,
                BookingNumber = bookingNumber,
                ActionType = actionType,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                PerformedBy = performedBy
            }, transaction);
        }

        public async Task<IEnumerable<BookingPayment>> GetPaymentsAsync(int bookingId)
        {
            const string sql = @"
                SELECT * FROM BookingPayments 
                WHERE BookingId = @BookingId 
                ORDER BY PaidOn DESC";

            return await _dbConnection.QueryAsync<BookingPayment>(sql, new { BookingId = bookingId });
        }

        public async Task<bool> AddPaymentAsync(BookingPayment payment, int performedBy)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            try
            {
                // Insert payment
                const string insertPaymentSql = @"
                    INSERT INTO BookingPayments (BookingId, Amount, PaymentMethod, PaymentReference, Status, PaidOn, Notes, CardType, CardLastFourDigits, BankId, ChequeDate)
                    VALUES (@BookingId, @Amount, @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes, @CardType, @CardLastFourDigits, @BankId, @ChequeDate)";

                await _dbConnection.ExecuteAsync(insertPaymentSql, payment, transaction);

                // Update booking deposit and balance
                const string updateBookingSql = @"
                    UPDATE Bookings 
                    SET DepositAmount = DepositAmount + @Amount,
                        BalanceAmount = BalanceAmount - @Amount,
                        PaymentStatus = CASE 
                            WHEN (BalanceAmount - @Amount) <= 0 THEN 'Paid'
                            WHEN (DepositAmount + @Amount) > 0 THEN 'Partially Paid'
                            ELSE 'Pending'
                        END,
                        LastModifiedBy = @PerformedBy,
                        LastModifiedDate = GETDATE()
                    WHERE Id = @BookingId";

                await _dbConnection.ExecuteAsync(updateBookingSql, new 
                { 
                    BookingId = payment.BookingId, 
                    Amount = payment.Amount,
                    PerformedBy = performedBy
                }, transaction);

                // Get booking details for audit log
                const string getBookingSql = "SELECT BookingNumber, DepositAmount, BalanceAmount FROM Bookings WHERE Id = @BookingId";
                var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(getBookingSql, new { BookingId = payment.BookingId }, transaction);

                if (booking != null)
                {
                    // Add audit log
                    await AddAuditLogAsync(
                        payment.BookingId,
                        booking.BookingNumber,
                        "Payment",
                        $"Payment of ₹{payment.Amount:N2} recorded via {payment.PaymentMethod}",
                        null,
                        $"Deposit: ₹{booking.DepositAmount:N2}, Balance: ₹{booking.BalanceAmount:N2}",
                        performedBy,
                        transaction
                    );
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> AddGuestToBookingAsync(BookingGuest guest, int branchId)
        {
            // Ensure connection is open
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // Insert into BookingGuests table
                const string bookingGuestSql = @"
                    INSERT INTO BookingGuests (BookingId, FullName, Email, Phone, GuestType, IsPrimary, 
                                             RelationshipToPrimary, Age, DateOfBirth, IdentityType, 
                                             IdentityNumber, DocumentPath, CreatedDate, CreatedBy)
                    VALUES (@BookingId, @FullName, @Email, @Phone, @GuestType, 0, 
                            @RelationshipToPrimary, @Age, @DateOfBirth, @IdentityType, 
                            @IdentityNumber, @DocumentPath, GETDATE(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)";

                var bookingGuestId = await _dbConnection.ExecuteScalarAsync<int>(bookingGuestSql, guest, transaction);

                if (bookingGuestId > 0)
                {
                    // Split FullName into FirstName and LastName
                    var nameParts = guest.FullName.Trim().Split(new[] { ' ' }, 2);
                    var firstName = nameParts[0];
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    // Insert into Guests table for future reference
                    const string guestSql = @"
                        INSERT INTO Guests (FirstName, LastName, Email, Phone, GuestType, BranchID, 
                                          DateOfBirth, IdentityType, IdentityNumber, 
                                          IsActive, CreatedDate, LastModifiedDate)
                        VALUES (@FirstName, @LastName, @Email, @Phone, @GuestType, @BranchID, 
                                @DateOfBirth, @IdentityType, @IdentityNumber, 
                                1, GETDATE(), GETDATE())";

                    var guestParams = new
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = guest.Email ?? "",
                        Phone = guest.Phone ?? "",
                        GuestType = guest.GuestType ?? "Companion",
                        BranchID = branchId,
                        DateOfBirth = guest.DateOfBirth,
                        IdentityType = guest.IdentityType,
                        IdentityNumber = guest.IdentityNumber
                    };

                    await _dbConnection.ExecuteAsync(guestSql, guestParams, transaction);
                }

                transaction.Commit();
                return bookingGuestId > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateGuestAsync(BookingGuest guest)
        {
            // Ensure connection is open
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            const string sql = @"
                UPDATE BookingGuests 
                SET FullName = @FullName,
                    Email = @Email,
                    Phone = @Phone,
                    GuestType = @GuestType,
                    RelationshipToPrimary = @RelationshipToPrimary,
                    Age = @Age,
                    DateOfBirth = @DateOfBirth,
                    IdentityType = @IdentityType,
                    IdentityNumber = @IdentityNumber,
                    DocumentPath = @DocumentPath,
                    ModifiedDate = GETDATE(),
                    ModifiedBy = @ModifiedBy
                WHERE Id = @Id AND IsActive = 1";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, guest);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteGuestAsync(int guestId, int deletedBy)
        {
            // Ensure connection is open
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            const string sql = @"
                UPDATE BookingGuests 
                SET IsActive = 0,
                    ModifiedDate = GETDATE(),
                    ModifiedBy = @DeletedBy
                WHERE Id = @GuestId AND IsPrimary = 0 AND IsActive = 1";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { GuestId = guestId, DeletedBy = deletedBy });
            return rowsAffected > 0;
        }

        public async Task<Booking?> GetLastBookingByGuestPhoneAsync(string phone)
        {
            const string sql = @"
                SELECT TOP 1 b.* 
                FROM Bookings b
                INNER JOIN BookingGuests bg ON b.Id = bg.BookingId
                WHERE bg.Phone = @Phone 
                    AND bg.IsPrimary = 1
                ORDER BY b.CreatedDate DESC";

            return await _dbConnection.QueryFirstOrDefaultAsync<Booking>(sql, new { Phone = phone });
        }

        /// <summary>
        /// Calculates the number of nights between check-in and check-out dates,
        /// considering the hotel's configured check-in and check-out times.
        /// </summary>
        /// <param name="checkInDate">Check-in date (date only)</param>
        /// <param name="checkOutDate">Check-out date (date only)</param>
        /// <param name="checkInTime">Configured check-in time from Hotel Settings</param>
        /// <param name="checkOutTime">Configured check-out time from Hotel Settings</param>
        /// <returns>Number of nights for the stay</returns>
        private int CalculateNights(DateTime checkInDate, DateTime checkOutDate, TimeSpan checkInTime, TimeSpan checkOutTime)
        {
            // Combine dates with their respective times
            var actualCheckIn = checkInDate.Date.Add(checkInTime);
            var actualCheckOut = checkOutDate.Date.Add(checkOutTime);
            
            // Calculate the time difference
            var duration = actualCheckOut - actualCheckIn;
            
            // Calculate nights: Full 24-hour periods count as nights
            // Example: Dec 1 3PM to Dec 3 11AM = ~43.5 hours = 1.8 days ≈ 2 nights
            var nights = (int)Math.Ceiling(duration.TotalHours / 24.0);
            
            // Ensure at least 1 night if check-out is after check-in
            if (nights < 1 && duration.TotalHours > 0)
            {
                nights = 1;
            }
            
            return nights;
        }
    }
}
