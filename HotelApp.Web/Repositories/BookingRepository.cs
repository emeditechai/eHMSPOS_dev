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

        public string ConnectionString => _dbConnection.ConnectionString;

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

            // Try to find exact match first, then fallback to any active rate for the room type
            const string rateSql = @"
                SELECT TOP 1 *
                FROM RateMaster
                WHERE RoomTypeId = @RoomTypeId
                  AND BranchID = @BranchID
                  AND @CheckInDate >= StartDate
                  AND @CheckOutDate <= EndDate
                  AND IsActive = 1
                  AND (
                      -- Priority 1: Exact match on CustomerType and Source
                      (CustomerType = @CustomerType AND Source = @Source)
                      OR
                      -- Priority 2: Match CustomerType only
                      (CustomerType = @CustomerType AND Source != @Source)
                      OR
                      -- Priority 3: Any rate for this room type
                      (CustomerType != @CustomerType)
                  )
                ORDER BY 
                    CASE 
                        WHEN CustomerType = @CustomerType AND Source = @Source THEN 1
                        WHEN CustomerType = @CustomerType THEN 2
                        ELSE 3
                    END,
                    StartDate DESC";

            var ratePlan = await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(rateSql, request);

            const string roomTypeSql = "SELECT * FROM RoomTypes WHERE Id = @RoomTypeId AND IsActive = 1";
            var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(roomTypeSql, new { request.RoomTypeId });
            if (roomType == null)
            {
                return null;
            }

            // Calculate night-by-night rates with weekend and special day logic
            var taxPercentage = ratePlan?.TaxPercentage ?? 0;
            var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
            var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);
            
            var totalGuests = request.Adults + request.Children;
            var extraGuests = Math.Max(0, totalGuests - roomType.MaxOccupancy);

            decimal totalOriginalRoomRate = 0; // Original rate before discount
            decimal avgOriginalBase = 0;
            decimal avgOriginalExtra = 0;
            decimal discountPercent = 0;

            // Calculate ORIGINAL rate for each night (before discount)
            for (var date = request.CheckInDate.Date; date < request.CheckOutDate.Date; date = date.AddDays(1))
            {
                var (nightlyBase, nightlyExtra, discount) = await GetEffectiveRateForDateAsync(
                    ratePlan?.Id ?? 0, 
                    date, 
                    ratePlan?.BaseRate ?? roomType.BaseRate, 
                    ratePlan?.ExtraPaxRate ?? 0,
                    applyDiscount: false); // Get ORIGINAL rate without discount

                avgOriginalBase += nightlyBase;
                avgOriginalExtra += nightlyExtra;
                discountPercent = discount; // Store discount percentage

                var nightRoomRate = nightlyBase + (nightlyExtra * extraGuests);
                totalOriginalRoomRate += nightRoomRate;
            }

            // Calculate averages for display
            avgOriginalBase = nights > 0 ? avgOriginalBase / nights : 0;
            avgOriginalExtra = nights > 0 ? avgOriginalExtra / nights : 0;

            // Multiply by number of required rooms
            var requiredRooms = request.RequiredRooms > 0 ? request.RequiredRooms : 1;
            var totalOriginalForAllRooms = totalOriginalRoomRate * requiredRooms;

            // Calculate discount amount on original amount
            decimal totalDiscountAmount = 0;
            if (discountPercent > 0)
            {
                totalDiscountAmount = Math.Round(totalOriginalForAllRooms * (discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }

            // Final amount after discount
            var totalRoomRateAfterDiscount = totalOriginalForAllRooms - totalDiscountAmount;

            // Tax is calculated on DISCOUNTED amount
            var totalTax = Math.Round(totalRoomRateAfterDiscount * (taxPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var totalCGST = Math.Round(totalRoomRateAfterDiscount * (cgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);
            var totalSGST = Math.Round(totalRoomRateAfterDiscount * (sgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);

            return new BookingQuoteResult
            {
                Nights = nights,
                RatePlanId = ratePlan?.Id,
                BaseRatePerNight = avgOriginalBase, // Original base rate (for display)
                ExtraPaxRatePerNight = avgOriginalExtra,
                TaxPercentage = taxPercentage,
                CGSTPercentage = cgstPercentage,
                SGSTPercentage = sgstPercentage,
                DiscountPercentage = discountPercent,
                DiscountAmount = totalDiscountAmount,
                TotalRoomRate = totalRoomRateAfterDiscount, // Amount after discount (this is what we charge)
                TotalTaxAmount = totalTax,
                TotalCGSTAmount = totalCGST,
                TotalSGSTAmount = totalSGST,
                GrandTotal = totalRoomRateAfterDiscount + totalTax
            };
        }

        private async Task<(decimal baseRate, decimal extraPaxRate, decimal discountPercent)> GetEffectiveRateForDateAsync(
            int rateMasterId, 
            DateTime date, 
            decimal defaultBaseRate, 
            decimal defaultExtraPaxRate,
            IDbTransaction? transaction = null,
            bool applyDiscount = true)
        {
            // Get discount percentage from RateMaster
            decimal discountPercent = 0;
            if (rateMasterId > 0)
            {
                const string discountSql = "SELECT ApplyDiscount FROM RateMaster WHERE Id = @RateMasterId";
                var discountStr = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                    discountSql,
                    new { RateMasterId = rateMasterId },
                    transaction);

                if (!string.IsNullOrEmpty(discountStr) && decimal.TryParse(discountStr, out var discount))
                {
                    discountPercent = discount;
                }
            }

            if (rateMasterId == 0)
            {
                return (defaultBaseRate, defaultExtraPaxRate, discountPercent);
            }

            // Priority 1: Check for Special Day Rates
            const string specialDaySql = @"
                SELECT TOP 1 BaseRate, ExtraPaxRate
                FROM SpecialDayRates
                WHERE RateMasterId = @RateMasterId
                  AND IsActive = 1
                  AND @Date >= CAST(FromDate AS DATE)
                  AND @Date <= CAST(ToDate AS DATE)
                ORDER BY FromDate DESC";

            var specialRate = await _dbConnection.QueryFirstOrDefaultAsync<(decimal BaseRate, decimal ExtraPaxRate)?>(  
                specialDaySql, 
                new { RateMasterId = rateMasterId, Date = date.Date },
                transaction);

            if (specialRate.HasValue)
            {
                var baseRate = specialRate.Value.BaseRate;
                var extraPaxRate = specialRate.Value.ExtraPaxRate;
                
                // Only apply discount if requested
                if (applyDiscount && discountPercent > 0)
                {
                    baseRate = Math.Round(baseRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                    extraPaxRate = Math.Round(extraPaxRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                }
                
                return (baseRate, extraPaxRate, discountPercent);
            }

            // Priority 2: Check for Weekend Rates
            // IMPORTANT: WeekendRates.DayOfWeek is stored in English ("Saturday", ...).
            // date.ToString("dddd") is culture-dependent and can produce non-English names.
            // Use a stable English mapping to avoid missing weekend rates.
            var dayOfWeek = date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Monday",
                DayOfWeek.Tuesday => "Tuesday",
                DayOfWeek.Wednesday => "Wednesday",
                DayOfWeek.Thursday => "Thursday",
                DayOfWeek.Friday => "Friday",
                DayOfWeek.Saturday => "Saturday",
                DayOfWeek.Sunday => "Sunday",
                _ => ""
            };
            const string weekendSql = @"
                SELECT TOP 1 BaseRate, ExtraPaxRate
                FROM WeekendRates
                WHERE RateMasterId = @RateMasterId
                  AND IsActive = 1
                  AND DayOfWeek = @DayOfWeek";

            var weekendRate = await _dbConnection.QueryFirstOrDefaultAsync<(decimal BaseRate, decimal ExtraPaxRate)?>(  
                weekendSql, 
                new { RateMasterId = rateMasterId, DayOfWeek = dayOfWeek },
                transaction);

            if (weekendRate.HasValue)
            {
                var baseRate = weekendRate.Value.BaseRate;
                var extraPaxRate = weekendRate.Value.ExtraPaxRate;
                
                // Only apply discount if requested
                if (applyDiscount && discountPercent > 0)
                {
                    baseRate = Math.Round(baseRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                    extraPaxRate = Math.Round(extraPaxRate * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                }
                
                return (baseRate, extraPaxRate, discountPercent);
            }

            // Priority 3: Default Rate
            var defaultBase = defaultBaseRate;
            var defaultExtra = defaultExtraPaxRate;
            
            // Only apply discount if requested
            if (applyDiscount && discountPercent > 0)
            {
                defaultBase = Math.Round(defaultBase * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                defaultExtra = Math.Round(defaultExtra * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }
            
            return (defaultBase, defaultExtra, discountPercent);
        }

        private async Task<List<(DateTime date, decimal rateAmountAfterDiscount, decimal actualBaseRate, decimal discountAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount)>> BuildRoomNightBreakdownAsync(
            int rateMasterId,
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal defaultBaseRate,
            decimal defaultExtraPaxRate,
            int extraGuests,
            decimal taxPercentage,
            decimal cgstPercentage,
            decimal sgstPercentage,
            IDbTransaction? transaction = null)
        {
            var breakdown = new List<(DateTime, decimal, decimal, decimal, decimal, decimal, decimal)>();

            for (var date = checkInDate.Date; date < checkOutDate.Date; date = date.AddDays(1))
            {
                // Get original (pre-discount) nightly base/extra first
                var (originalBase, originalExtra, discountPercent) = await GetEffectiveRateForDateAsync(
                    rateMasterId,
                    date,
                    defaultBaseRate,
                    defaultExtraPaxRate,
                    transaction,
                    applyDiscount: false);

                var actualBaseRate = Math.Round(originalBase + (originalExtra * extraGuests), 2, MidpointRounding.AwayFromZero);

                var rateAfterDiscount = actualBaseRate;
                var discountAmount = 0m;
                if (discountPercent > 0)
                {
                    discountAmount = Math.Round(actualBaseRate * (discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                    rateAfterDiscount = Math.Round(actualBaseRate - discountAmount, 2, MidpointRounding.AwayFromZero);
                }

                // Taxes are calculated on the AFTER-discount amount
                var nightTax = Math.Round(rateAfterDiscount * (taxPercentage / 100m), 2, MidpointRounding.AwayFromZero);
                var nightCGST = Math.Round(rateAfterDiscount * (cgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);
                var nightSGST = Math.Round(rateAfterDiscount * (sgstPercentage / 100m), 2, MidpointRounding.AwayFromZero);

                breakdown.Add((date, rateAfterDiscount, actualBaseRate, discountAmount, nightTax, nightCGST, nightSGST));
            }

            return breakdown;
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

        /// <summary>
        /// Gets the tax percentages from RateMaster for display purposes
        /// </summary>
        public async Task<(decimal taxPercentage, decimal cgstPercentage, decimal sgstPercentage)> GetRateMasterTaxPercentagesAsync(int ratePlanId)
        {
            const string sql = "SELECT TaxPercentage, CGSTPercentage, SGSTPercentage FROM RateMaster WHERE Id = @RatePlanId";
            var result = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(sql, new { RatePlanId = ratePlanId });
            
            if (result != null)
            {
                decimal taxPercentage = result.TaxPercentage ?? 0m;
                // If CGST/SGST are 0 or null, calculate from total tax percentage (split 50/50)
                decimal cgstPercentage = (result.CGSTPercentage != null && result.CGSTPercentage > 0) 
                    ? result.CGSTPercentage 
                    : taxPercentage / 2;
                decimal sgstPercentage = (result.SGSTPercentage != null && result.SGSTPercentage > 0) 
                    ? result.SGSTPercentage 
                    : taxPercentage / 2;
                return (taxPercentage, cgstPercentage, sgstPercentage);
            }
            
            return (0m, 0m, 0m);
        }

        /// <summary>
        /// Checks if sufficient room capacity is available for booking based on Max_RoomAvailability
        /// </summary>
        public async Task<bool> CheckRoomCapacityAvailabilityAsync(int roomTypeId, int branchId, DateTime checkIn, DateTime checkOut, int requiredRooms)
        {
            const string sql = @"
                -- Get room type capacity configuration
                DECLARE @MaxCapacity INT;
                SELECT @MaxCapacity = Max_RoomAvailability 
                FROM RoomTypes 
                WHERE Id = @RoomTypeId AND BranchID = @BranchId AND IsActive = 1;

                -- If no capacity configured, return 0 (unavailable)
                IF @MaxCapacity IS NULL
                    SELECT 0 AS IsAvailable;
                ELSE
                BEGIN
                    -- Calculate total required rooms already booked for this room type in the date range
                    DECLARE @BookedRooms INT;
                    SELECT @BookedRooms = ISNULL(SUM(RequiredRooms), 0)
                    FROM Bookings
                    WHERE RoomTypeId = @RoomTypeId
                        AND BranchID = @BranchId
                        AND Status IN ('Confirmed', 'CheckedIn')
                        AND CAST(CheckInDate AS DATE) < CAST(@CheckOut AS DATE)
                        AND (
                            CASE 
                                WHEN ActualCheckOutDate IS NOT NULL 
                                THEN CAST(ActualCheckOutDate AS DATE)
                                ELSE CAST(CheckOutDate AS DATE)
                            END > CAST(@CheckIn AS DATE)
                        );

                    -- Check if there's enough capacity
                    SELECT CASE 
                        WHEN (@MaxCapacity - @BookedRooms) >= @RequiredRooms THEN 1
                        ELSE 0
                    END AS IsAvailable;
                END";

            var result = await _dbConnection.ExecuteScalarAsync<int>(sql, new 
            { 
                RoomTypeId = roomTypeId, 
                BranchId = branchId,
                CheckIn = checkIn.Date, 
                CheckOut = checkOut.Date,
                RequiredRooms = requiredRooms
            });

            return result == 1;
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

            // Ensure booking monetary totals are consistent with the night-wise breakdown.
            // We treat BookingRoomNights.RateAmount as the AFTER-discount nightly room charge.
            // DiscountAmount is derived from RateMaster.ApplyDiscount over the ORIGINAL nightly rates.
            // This avoids situations where nights are discounted correctly but DiscountAmount is stored only for a single night.
            if (roomNights != null)
            {
                var nightsList = roomNights.ToList();
                if (nightsList.Count > 0)
                {
                    var nightlyBaseAfterDiscountSum = nightsList.Sum(n => n.RateAmount);
                    var nightlyTaxSum = nightsList.Sum(n => n.TaxAmount);
                    var nightlyCGSTSum = nightsList.Sum(n => n.CGSTAmount);
                    var nightlySGSTSum = nightsList.Sum(n => n.SGSTAmount);

                    var ratePlanId = booking.RatePlanId ?? 0;
                    decimal discountPercent = 0;
                    if (ratePlanId > 0)
                    {
                        const string discountSql = "SELECT ApplyDiscount FROM RateMaster WHERE Id = @RateMasterId";
                        var discountStr = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                            discountSql,
                            new { RateMasterId = ratePlanId },
                            transaction);

                        if (!string.IsNullOrWhiteSpace(discountStr) && decimal.TryParse(discountStr, out var parsedDiscount))
                        {
                            discountPercent = parsedDiscount;
                        }
                    }

                    var requiredRooms = booking.RequiredRooms > 0 ? booking.RequiredRooms : 1;
                    var nightlyAfterDiscountForAllRooms = nightlyBaseAfterDiscountSum * requiredRooms;
                    var nightlyTaxForAllRooms = nightlyTaxSum * requiredRooms;
                    var nightlyCGSTForAllRooms = nightlyCGSTSum * requiredRooms;
                    var nightlySGSTForAllRooms = nightlySGSTSum * requiredRooms;

                    decimal discountAmount = 0;
                    if (discountPercent > 0)
                    {
                        // If we know the after-discount sum, original sum can be reconstructed:
                        // after = original * (1 - d)  =>  original = after / (1 - d)
                        var divisor = 1 - (discountPercent / 100m);
                        if (divisor > 0)
                        {
                            var originalTotal = Math.Round(nightlyAfterDiscountForAllRooms / divisor, 2, MidpointRounding.AwayFromZero);
                            discountAmount = Math.Round(originalTotal - nightlyAfterDiscountForAllRooms, 2, MidpointRounding.AwayFromZero);
                        }
                    }

                    booking.BaseAmount = Math.Round(nightlyAfterDiscountForAllRooms, 2, MidpointRounding.AwayFromZero);
                    booking.TaxAmount = Math.Round(nightlyTaxForAllRooms, 2, MidpointRounding.AwayFromZero);
                    booking.CGSTAmount = Math.Round(nightlyCGSTForAllRooms, 2, MidpointRounding.AwayFromZero);
                    booking.SGSTAmount = Math.Round(nightlySGSTForAllRooms, 2, MidpointRounding.AwayFromZero);
                    booking.DiscountAmount = discountAmount;
                    booking.TotalAmount = Math.Round(booking.BaseAmount + booking.TaxAmount, 2, MidpointRounding.AwayFromZero);
                }
            }

            const string insertBookingSql = @"
                INSERT INTO Bookings (
                    BookingNumber, Status, PaymentStatus, Channel, Source, CustomerType,
                    CheckInDate, CheckOutDate, Nights, RoomTypeId, RequiredRooms, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, BranchID, CreatedBy,
                    LastModifiedBy)
                VALUES (
                    @BookingNumber, @Status, @PaymentStatus, @Channel, @Source, @CustomerType,
                    @CheckInDate, @CheckOutDate, @Nights, @RoomTypeId, @RequiredRooms, @RoomId, @RatePlanId,
                    @BaseAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @DiscountAmount, @TotalAmount, @DepositAmount,
                    @BalanceAmount, @Adults, @Children, @PrimaryGuestFirstName, @PrimaryGuestLastName,
                    @PrimaryGuestEmail, @PrimaryGuestPhone, @LoyaltyId, @SpecialRequests, @BranchID, @CreatedBy,
                    @LastModifiedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var bookingId = await _dbConnection.ExecuteScalarAsync<int>(insertBookingSql, booking, transaction);

            // Insert/Update guests in Guests table and link to BookingGuests
            Guest? primaryGuest = null;
            const string insertGuestSql = @"
                INSERT INTO BookingGuests (BookingId, GuestId, FullName, Email, Phone, Gender, GuestType, IsPrimary,
                                         Age, DateOfBirth,
                                         Address, City, State, Country, Pincode, CountryId, StateId, CityId)
                VALUES (@BookingId, @GuestId, @FullName, @Email, @Phone, @Gender, @GuestType, @IsPrimary,
                        @Age, @DateOfBirth,
                        @Address, @City, @State, @Country, @Pincode, @CountryId, @StateId, @CityId);";

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
                
                int persistedGuestId;
                if (existingGuest != null)
                {
                    // Update existing guest with all details including address
                    const string updateGuestSql = @"
                        UPDATE Guests SET 
                            FirstName = @FirstName, 
                            LastName = @LastName, 
                            Email = @Email, 
                            Gender = @Gender,
                            DateOfBirth = @DateOfBirth,
                            Age = @Age,
                            GuestType = @GuestType, 
                            ParentGuestId = @ParentGuestId,
                            Address = @Address,
                            City = @City,
                            State = @State,
                            Country = @Country,
                            Pincode = @Pincode,
                            CountryId = @CountryId,
                            StateId = @StateId,
                            CityId = @CityId,
                            BranchID = @BranchID,
                            LastModifiedDate = GETDATE()
                        WHERE Id = @Id";
                    await _dbConnection.ExecuteAsync(updateGuestSql, new
                    {
                        Id = existingGuest.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = bookingGuest.Email ?? "",
                        Gender = bookingGuest.Gender,
                        DateOfBirth = bookingGuest.DateOfBirth,
                        Age = bookingGuest.Age,
                        GuestType = guestType,
                        ParentGuestId = parentGuestId,
                        Address = bookingGuest.Address,
                        City = bookingGuest.City,
                        State = bookingGuest.State,
                        Country = bookingGuest.Country,
                        Pincode = bookingGuest.Pincode,
                        CountryId = bookingGuest.CountryId,
                        StateId = bookingGuest.StateId,
                        CityId = bookingGuest.CityId,
                        BranchID = booking.BranchID
                    }, transaction);
                    
                    if (bookingGuest.IsPrimary)
                    {
                        existingGuest.FirstName = firstName;
                        existingGuest.LastName = lastName;
                        primaryGuest = existingGuest;
                    }

                    persistedGuestId = existingGuest.Id;
                }
                else
                {
                    // Create new guest with all details including address
                    const string insertNewGuestSql = @"
                        INSERT INTO Guests (
                            FirstName, LastName, Email, Phone, Gender, DateOfBirth, Age, GuestType, ParentGuestId,
                            Address, City, State, Country, Pincode, CountryId, StateId, CityId,
                            BranchID, IsActive, CreatedDate, LastModifiedDate
                        )
                        VALUES (
                            @FirstName, @LastName, @Email, @Phone, @Gender, @DateOfBirth, @Age, @GuestType, @ParentGuestId,
                            @Address, @City, @State, @Country, @Pincode, @CountryId, @StateId, @CityId,
                            @BranchID, 1, GETDATE(), GETDATE()
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    
                    var newGuestId = await _dbConnection.ExecuteScalarAsync<int>(insertNewGuestSql, new
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = bookingGuest.Email ?? "",
                        Phone = bookingGuest.Phone ?? "",
                        Gender = bookingGuest.Gender,
                        DateOfBirth = bookingGuest.DateOfBirth,
                        Age = bookingGuest.Age,
                        GuestType = guestType,
                        ParentGuestId = parentGuestId,
                        Address = bookingGuest.Address,
                        City = bookingGuest.City,
                        State = bookingGuest.State,
                        Country = bookingGuest.Country,
                        Pincode = bookingGuest.Pincode,
                        CountryId = bookingGuest.CountryId,
                        StateId = bookingGuest.StateId,
                        CityId = bookingGuest.CityId,
                        BranchID = booking.BranchID
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

                    persistedGuestId = newGuestId;
                }
                
                // Insert into BookingGuests for this booking
                bookingGuest.BookingId = bookingId;
                bookingGuest.GuestId = persistedGuestId;
                await _dbConnection.ExecuteAsync(insertGuestSql, bookingGuest, transaction);
            }

            const string insertPaymentSql = @"
                INSERT INTO BookingPayments (BookingId, Amount, PaymentMethod, PaymentReference, Status, PaidOn, Notes, IsAdvancePayment)
                VALUES (@BookingId, @Amount, @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes, @IsAdvancePayment);";

            var depositAccumulator = 0m;
            foreach (var payment in payments)
            {
                payment.BookingId = bookingId;
                await _dbConnection.ExecuteAsync(insertPaymentSql, payment, transaction);

                depositAccumulator += payment.Amount;
                await AddAuditLogAsync(
                    bookingId,
                    booking.BookingNumber,
                    "PaymentReceived",
                    $"Payment of ₹{payment.Amount:N2} captured via {payment.PaymentMethod}",
                    null,
                    $"Total advance collected: ₹{depositAccumulator:N2}",
                    booking.CreatedBy,
                    transaction);
            }

            const string insertNightSql = @"
                INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status);";

            foreach (var night in roomNights)
            {
                night.BookingId = bookingId;
                await _dbConnection.ExecuteAsync(insertNightSql, night, transaction);
            }

            // Always create ReservationRoomNights from booking dates.
            // This is used for printing plan details BEFORE room assignment.
            await UpsertReservationRoomNightsAsync(
                bookingId,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.BaseAmount,
                booking.DiscountAmount,
                booking.TaxAmount,
                booking.CGSTAmount,
                booking.SGSTAmount,
                booking.Nights,
                transaction);

            const string updateRoomStatusSql = "UPDATE Rooms SET Status = 'Reserved', LastModifiedDate = GETDATE() WHERE Id = @RoomId";
            if (booking.RoomId.HasValue)
            {
                await _dbConnection.ExecuteAsync(updateRoomStatusSql, new { booking.RoomId }, transaction);
            }

            await AddAuditLogAsync(
                bookingId,
                booking.BookingNumber,
                "Created",
                $"Booking created for {booking.PrimaryGuestFirstName} {booking.PrimaryGuestLastName}".Trim(),
                null,
                $"Stay {booking.CheckInDate:dd MMM yyyy} - {booking.CheckOutDate:dd MMM yyyy}, Total ₹{booking.TotalAmount:N2}, Advance ₹{booking.DepositAmount:N2}",
                booking.CreatedBy,
                transaction);

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
                    CheckInDate, CheckOutDate, ActualCheckInDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
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
                    CheckInDate, CheckOutDate, ActualCheckInDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
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
                    CheckInDate, CheckOutDate, ActualCheckInDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
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
                    CheckInDate, CheckOutDate, ActualCheckInDate, ActualCheckOutDate, Nights, RoomTypeId, RoomId, RatePlanId,
                    BaseAmount, TaxAmount, CGSTAmount, SGSTAmount, DiscountAmount, TotalAmount, DepositAmount,
                    BalanceAmount, Adults, Children, PrimaryGuestFirstName, PrimaryGuestLastName,
                    PrimaryGuestEmail, PrimaryGuestPhone, LoyaltyId, SpecialRequests, RequiredRooms, BranchID,
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

            const string reservationNightSql = "SELECT * FROM ReservationRoomNights WHERE BookingId IN @Ids";
            var reservationNights = await _dbConnection.QueryAsync<ReservationRoomNight>(reservationNightSql, new { Ids = bookingIds });

            const string bookingRoomsSql = @"
                SELECT br.*, r.RoomNumber 
                FROM BookingRooms br
                INNER JOIN Rooms r ON br.RoomId = r.Id
                WHERE br.BookingId IN @Ids AND br.IsActive = 1";
            var bookingRooms = await _dbConnection.QueryAsync<BookingRoom, string, BookingRoom>(
                bookingRoomsSql,
                (bookingRoom, roomNumber) =>
                {
                    bookingRoom.Room = new Room { Id = bookingRoom.RoomId, RoomNumber = roomNumber };
                    return bookingRoom;
                },
                new { Ids = bookingIds },
                splitOn: "RoomNumber"
            );

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
                booking.ReservationRoomNights = reservationNights.Where(n => n.BookingId == booking.Id).OrderBy(n => n.StayDate).ToList();
                booking.AssignedRooms = bookingRooms.Where(br => br.BookingId == booking.Id).ToList();

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
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            try
            {
                const string getBookingSql = @"
                    SELECT Id, RoomTypeId, RoomId, Nights, CheckInDate, CheckOutDate
                    FROM Bookings
                    WHERE BookingNumber = @BookingNumber";

                var booking = await _dbConnection.QueryFirstOrDefaultAsync<Booking>(
                    getBookingSql,
                    new { BookingNumber = bookingNumber },
                    transaction);

                if (booking == null)
                {
                    transaction.Rollback();
                    return false;
                }

                const string sql = @"
                    UPDATE Bookings
                    SET RoomTypeId = @RoomTypeId,
                        BaseAmount = @BaseAmount,
                        TaxAmount = @TaxAmount,
                        CGSTAmount = @CGSTAmount,
                        SGSTAmount = @SGSTAmount,
                        TotalAmount = @TotalAmount,
                        BalanceAmount = @TotalAmount - ISNULL((
                            SELECT SUM(
                                Amount
                                + ISNULL(DiscountAmount, 0)
                                + CASE WHEN ISNULL(IsRoundOffApplied, 0) = 1 THEN ISNULL(RoundOffAmount, 0) ELSE 0 END
                            )
                            FROM BookingPayments
                            WHERE BookingId = Bookings.Id
                        ), 0),
                        LastModifiedDate = GETDATE()
                    WHERE BookingNumber = @BookingNumber";

                var rowsAffected = 0;
                try
                {
                    rowsAffected = await _dbConnection.ExecuteAsync(sql, new
                    {
                        BookingNumber = bookingNumber,
                        RoomTypeId = newRoomTypeId,
                        BaseAmount = baseAmount,
                        TaxAmount = taxAmount,
                        CGSTAmount = cgstAmount,
                        SGSTAmount = sgstAmount,
                        TotalAmount = totalAmount
                    }, transaction);
                }
                catch (Exception ex) when (
                    ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("DiscountAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("RoundOffAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("IsRoundOffApplied", StringComparison.OrdinalIgnoreCase))
                {
                    const string legacySql = @"
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

                    rowsAffected = await _dbConnection.ExecuteAsync(legacySql, new
                    {
                        BookingNumber = bookingNumber,
                        RoomTypeId = newRoomTypeId,
                        BaseAmount = baseAmount,
                        TaxAmount = taxAmount,
                        CGSTAmount = cgstAmount,
                        SGSTAmount = sgstAmount,
                        TotalAmount = totalAmount
                    }, transaction);
                }

                if (rowsAffected == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                if (booking.RoomId.HasValue && booking.Nights > 0)
                {
                    const string deleteNightsSql = @"DELETE FROM BookingRoomNights WHERE BookingId = @BookingId";
                    await _dbConnection.ExecuteAsync(deleteNightsSql, new { BookingId = booking.Id }, transaction);

                    var nightlyRoomAmount = Math.Round(baseAmount / booking.Nights, 2, MidpointRounding.AwayFromZero);
                    var nightlyTax = Math.Round(taxAmount / booking.Nights, 2, MidpointRounding.AwayFromZero);
                    var nightlyCGST = Math.Round(cgstAmount / booking.Nights, 2, MidpointRounding.AwayFromZero);
                    var nightlySGST = Math.Round(sgstAmount / booking.Nights, 2, MidpointRounding.AwayFromZero);

                    const string insertNightSql = @"
                        INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                        VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                    for (var date = booking.CheckInDate.Date; date < booking.CheckOutDate.Date; date = date.AddDays(1))
                    {
                        await _dbConnection.ExecuteAsync(
                            insertNightSql,
                            new
                            {
                                BookingId = booking.Id,
                                RoomId = booking.RoomId.Value,
                                StayDate = date,
                                RateAmount = nightlyRoomAmount,
                                ActualBaseRate = nightlyRoomAmount,
                                DiscountAmount = 0m,
                                TaxAmount = nightlyTax,
                                CGSTAmount = nightlyCGST,
                                SGSTAmount = nightlySGST,
                                Status = "Reserved"
                            },
                            transaction);
                    }
                }

                await AddAuditLogAsync(
                    booking.Id,
                    bookingNumber,
                    "RoomTypeChange",
                    "Room type changed",
                    booking.RoomTypeId.ToString(),
                    newRoomTypeId.ToString(),
                    null,
                    transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateActualCheckOutDateAsync(string bookingNumber, DateTime actualCheckOutDate, int performedBy)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            const string getBookingSql = @"SELECT Id FROM Bookings WHERE BookingNumber = @BookingNumber";
            var booking = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(getBookingSql, new { BookingNumber = bookingNumber }, transaction);

            if (booking == null)
            {
                transaction.Rollback();
                return false;
            }

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
            }, transaction);

            if (rowsAffected == 0)
            {
                transaction.Rollback();
                return false;
            }

            await AddAuditLogAsync(
                (int)booking.Id,
                bookingNumber,
                "CheckedOut",
                $"Actual checkout recorded at {actualCheckOutDate:dd MMM yyyy hh:mm tt}",
                null,
                actualCheckOutDate.ToString("o"),
                performedBy,
                transaction);

            transaction.Commit();
            return true;
        }

        public async Task<IEnumerable<int>> GetAssignedRoomIdsAsync(string bookingNumber)
        {
            const string sql = @"
                SELECT br.RoomId
                FROM BookingRooms br
                INNER JOIN Bookings b ON br.BookingId = b.Id
                WHERE b.BookingNumber = @BookingNumber
                    AND br.IsActive = 1
                ORDER BY br.RoomId";

            var roomIds = await _dbConnection.QueryAsync<int>(sql, new { BookingNumber = bookingNumber });
            return roomIds ?? Enumerable.Empty<int>();
        }

        public async Task<bool> UpdateRoomAssignmentAsync(string bookingNumber, int roomId, int? performedBy = null)
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
                    SELECT RoomId, Id, CheckInDate, CheckOutDate, TotalAmount, TaxAmount, CGSTAmount, SGSTAmount, Nights, ActualCheckInDate,
                           RatePlanId, RoomTypeId, Adults, Children
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
                var actualCheckInDate = booking.ActualCheckInDate == null ? (DateTime?)null : (DateTime)booking.ActualCheckInDate;
                var shouldCaptureActualCheckIn = !previousRoomId.HasValue && !actualCheckInDate.HasValue;
                var actualCheckInTimestamp = shouldCaptureActualCheckIn ? DateTime.Now : actualCheckInDate;

                const string roomNumberSql = "SELECT RoomNumber FROM Rooms WHERE Id = @RoomId";
                var newRoomNumber = await _dbConnection.QueryFirstOrDefaultAsync<string>(roomNumberSql, new { RoomId = roomId }, transaction);
                string? previousRoomNumber = null;
                if (previousRoomId.HasValue)
                {
                    previousRoomNumber = await _dbConnection.QueryFirstOrDefaultAsync<string>(roomNumberSql, new { RoomId = previousRoomId.Value }, transaction);
                }

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
                var updateBookingSql = @"
                    UPDATE Bookings 
                    SET RoomId = @RoomId,
                        LastModifiedDate = GETUTCDATE()";

                if (shouldCaptureActualCheckIn)
                {
                    updateBookingSql += ",\n                        ActualCheckInDate = @ActualCheckInDate";
                }

                updateBookingSql += "\n                    WHERE BookingNumber = @BookingNumber";

                var bookingUpdated = await _dbConnection.ExecuteAsync(
                    updateBookingSql,
                    new
                    {
                        RoomId = roomId,
                        BookingNumber = bookingNumber,
                        ActualCheckInDate = actualCheckInTimestamp
                    },
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

                // Create BookingRoomNights records with proper rate calculation
                var bookingId = (int)booking.Id;
                var checkInDate = (DateTime)booking.CheckInDate;
                var checkOutDate = (DateTime)booking.CheckOutDate;
                var nights = (int)booking.Nights;
                var ratePlanId = booking.RatePlanId as int? ?? 0;
                var roomTypeId = (int)booking.RoomTypeId;
                var adults = (int)booking.Adults;
                var children = (int)booking.Children;

                // Get rate plan details and room type info
                var ratePlan = ratePlanId > 0 
                    ? await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(
                        "SELECT * FROM RateMaster WHERE Id = @Id", 
                        new { Id = ratePlanId }, 
                        transaction)
                    : null;

                var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(
                    "SELECT * FROM RoomTypes WHERE Id = @Id", 
                    new { Id = roomTypeId }, 
                    transaction);

                if (roomType == null)
                {
                    transaction.Rollback();
                    return false;
                }

                var defaultBaseRate = ratePlan?.BaseRate ?? roomType.BaseRate;
                var defaultExtraPaxRate = ratePlan?.ExtraPaxRate ?? 0;
                var taxPercentage = ratePlan?.TaxPercentage ?? 0;
                var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
                var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);
                var extraGuests = Math.Max(0, adults + children - roomType.MaxOccupancy);

                // Build room night breakdown with weekend/special day rates
                var roomNightBreakdown = await BuildRoomNightBreakdownAsync(
                    ratePlanId,
                    checkInDate,
                    checkOutDate,
                    defaultBaseRate,
                    defaultExtraPaxRate,
                    extraGuests,
                    taxPercentage,
                    cgstPercentage,
                    sgstPercentage);

                const string insertRoomNightSql = @"
                    INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                    VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                foreach (var (date, rateAmountAfterDiscount, actualBaseRate, discountAmount, taxAmount, cgstAmount, sgstAmount) in roomNightBreakdown)
                {
                    await _dbConnection.ExecuteAsync(
                        insertRoomNightSql,
                        new
                        {
                            BookingId = bookingId,
                            RoomId = roomId,
                            StayDate = date,
                            RateAmount = rateAmountAfterDiscount,
                            ActualBaseRate = actualBaseRate,
                            DiscountAmount = discountAmount,
                            TaxAmount = taxAmount,
                            CGSTAmount = cgstAmount,
                            SGSTAmount = sgstAmount,
                            Status = "Reserved"
                        },
                        transaction
                    );
                }

                var actionType = previousRoomId.HasValue && previousRoomId.Value != roomId ? "RoomChanged" : previousRoomId.HasValue ? "RoomAssignmentUpdated" : "RoomAssigned";
                var description = actionType switch
                {
                    "RoomChanged" => $"Room changed from {previousRoomNumber ?? previousRoomId?.ToString() ?? "N/A"} to {newRoomNumber ?? roomId.ToString()}",
                    "RoomAssignmentUpdated" => $"Room {newRoomNumber ?? roomId.ToString()} assignment refreshed",
                    _ => $"Room {newRoomNumber ?? roomId.ToString()} assigned to booking"
                };

                await AddAuditLogAsync(
                    bookingId,
                    bookingNumber,
                    actionType,
                    description,
                    previousRoomNumber,
                    newRoomNumber,
                    performedBy,
                    transaction);

                if (shouldCaptureActualCheckIn && actualCheckInTimestamp.HasValue)
                {
                    await AddAuditLogAsync(
                        bookingId,
                        bookingNumber,
                        "CheckedIn",
                        $"Actual check-in recorded at {actualCheckInTimestamp:dd MMM yyyy hh:mm tt}",
                        null,
                        actualCheckInTimestamp.Value.ToString("o"),
                        performedBy,
                        transaction);
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

        public async Task<bool> AssignMultipleRoomsAsync(string bookingNumber, int[] roomIds, int? performedBy = null)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();

            try
            {
                // Get booking details
                const string getBookingSql = @"
                    SELECT Id, CheckInDate, CheckOutDate, ActualCheckInDate, RoomTypeId,
                           RatePlanId, Adults, Children, Nights
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
                var actualCheckInDate = booking.ActualCheckInDate == null ? (DateTime?)null : (DateTime)booking.ActualCheckInDate;
                var shouldCaptureActualCheckIn = !actualCheckInDate.HasValue;
                var actualCheckInTimestamp = shouldCaptureActualCheckIn ? DateTime.Now : actualCheckInDate;

                // Deactivate any existing room assignments
                const string deactivateExistingSql = @"
                    UPDATE BookingRooms 
                    SET IsActive = 0, UnassignedDate = GETUTCDATE()
                    WHERE BookingId = @BookingId AND IsActive = 1";
                
                await _dbConnection.ExecuteAsync(
                    deactivateExistingSql,
                    new { BookingId = bookingId },
                    transaction
                );

                // Get room numbers for audit log
                var roomNumbers = new List<string>();

                // Insert new room assignments and update room status
                foreach (var roomId in roomIds)
                {
                    // Get room number and verify room type
                    const string roomInfoSql = "SELECT RoomNumber, RoomTypeId, Status FROM Rooms WHERE Id = @RoomId";
                    var roomInfo = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                        roomInfoSql,
                        new { RoomId = roomId },
                        transaction
                    );

                    if (roomInfo == null || (int)roomInfo.RoomTypeId != (int)booking.RoomTypeId)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    roomNumbers.Add((string)roomInfo.RoomNumber);

                    // Insert into BookingRooms
                    const string insertBookingRoomSql = @"
                        INSERT INTO BookingRooms (BookingId, RoomId, AssignedDate, IsActive, CreatedDate, CreatedBy)
                        VALUES (@BookingId, @RoomId, @AssignedDate, 1, GETUTCDATE(), @CreatedBy)";

                    await _dbConnection.ExecuteAsync(
                        insertBookingRoomSql,
                        new
                        {
                            BookingId = bookingId,
                            RoomId = roomId,
                            AssignedDate = actualCheckInTimestamp ?? DateTime.Now,
                            CreatedBy = performedBy?.ToString()
                        },
                        transaction
                    );

                    // Update room status to Occupied
                    const string updateRoomSql = @"
                        UPDATE Rooms 
                        SET Status = 'Occupied', LastModifiedDate = GETUTCDATE()
                        WHERE Id = @RoomId";

                    await _dbConnection.ExecuteAsync(
                        updateRoomSql,
                        new { RoomId = roomId },
                        transaction
                    );
                }

                // Update booking with primary room (first room) and actual check-in date if needed
                var updateBookingSql = @"
                    UPDATE Bookings 
                    SET RoomId = @RoomId, LastModifiedDate = GETUTCDATE()";

                if (shouldCaptureActualCheckIn)
                {
                    updateBookingSql += ", ActualCheckInDate = @ActualCheckInDate";
                }

                updateBookingSql += " WHERE BookingNumber = @BookingNumber";

                await _dbConnection.ExecuteAsync(
                    updateBookingSql,
                    new
                    {
                        RoomId = roomIds[0], // Primary room
                        ActualCheckInDate = actualCheckInTimestamp,
                        BookingNumber = bookingNumber
                    },
                    transaction
                );

                // Get rate plan and room type details for room night calculation
                var ratePlanId = booking.RatePlanId as int? ?? 0;
                var checkInDate = (DateTime)booking.CheckInDate;
                var checkOutDate = (DateTime)booking.CheckOutDate;
                var adults = (int)booking.Adults;
                var children = (int)booking.Children;
                var roomTypeId = (int)booking.RoomTypeId;

                var ratePlan = ratePlanId > 0 
                    ? await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(
                        "SELECT * FROM RateMaster WHERE Id = @Id", 
                        new { Id = ratePlanId }, 
                        transaction)
                    : null;

                var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(
                    "SELECT * FROM RoomTypes WHERE Id = @Id", 
                    new { Id = roomTypeId }, 
                    transaction);

                if (roomType != null)
                {
                    var defaultBaseRate = ratePlan?.BaseRate ?? roomType.BaseRate;
                    var defaultExtraPaxRate = ratePlan?.ExtraPaxRate ?? 0;
                    var taxPercentage = ratePlan?.TaxPercentage ?? 0;
                    var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
                    var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);
                    var extraGuests = Math.Max(0, adults + children - roomType.MaxOccupancy);

                    // Build room night breakdown with weekend/special day rates
                    var roomNightBreakdown = await BuildRoomNightBreakdownAsync(
                        ratePlanId,
                        checkInDate,
                        checkOutDate,
                        defaultBaseRate,
                        defaultExtraPaxRate,
                        extraGuests,
                        taxPercentage,
                        cgstPercentage,
                        sgstPercentage,
                        transaction);

                    // Create room nights for each assigned room
                    const string insertRoomNightSql = @"
                        INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                        VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                    foreach (var roomId in roomIds)
                    {
                        foreach (var (date, rateAmountAfterDiscount, actualBaseRate, discountAmount, taxAmount, cgstAmount, sgstAmount) in roomNightBreakdown)
                        {
                            await _dbConnection.ExecuteAsync(
                                insertRoomNightSql,
                                new
                                {
                                    BookingId = bookingId,
                                    RoomId = roomId,
                                    StayDate = date,
                                    RateAmount = rateAmountAfterDiscount,
                                    ActualBaseRate = actualBaseRate,
                                    DiscountAmount = discountAmount,
                                    TaxAmount = taxAmount,
                                    CGSTAmount = cgstAmount,
                                    SGSTAmount = sgstAmount,
                                    Status = "Reserved"
                                },
                                transaction
                            );
                        }
                    }
                }


                // Add audit log
                var roomNumbersList = string.Join(", ", roomNumbers);
                await AddAuditLogAsync(
                    bookingId,
                    bookingNumber,
                    "RoomAssigned",
                    $"Assigned room(s): {roomNumbersList}",
                    null,
                    roomNumbersList,
                    performedBy,
                    transaction
                );

                if (shouldCaptureActualCheckIn)
                {
                    await AddAuditLogAsync(
                        bookingId,
                        bookingNumber,
                        "CheckedIn",
                        $"Actual check-in recorded at {actualCheckInTimestamp:dd MMM yyyy hh:mm tt}",
                        null,
                        actualCheckInTimestamp.Value.ToString("o"),
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

        public async Task<bool> UpdateBookingDatesAsync(string bookingNumber, DateTime checkInDate, DateTime checkOutDate, int nights, decimal baseAmount, decimal discountAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount, decimal totalAmount)
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

                // Recalculate totals from source-of-truth logic to avoid persisting a single-night discount
                // when extending dates. Quote-based amounts can drift if requiredRooms/rate plan logic changes.
                const string bookingMetaSql = @"
                    SELECT RoomTypeId, RequiredRooms, Adults, Children, RatePlanId, BranchID
                    FROM Bookings
                    WHERE BookingNumber = @BookingNumber";

                var bookingMeta = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    bookingMetaSql,
                    new { BookingNumber = bookingNumber },
                    transaction);

                if (bookingMeta == null)
                {
                    transaction.Rollback();
                    return false;
                }

                int roomTypeId = (int)bookingMeta.RoomTypeId;
                int requiredRooms = (int)bookingMeta.RequiredRooms;
                int adults = (int)bookingMeta.Adults;
                int children = (int)bookingMeta.Children;
                int ratePlanId = bookingMeta.RatePlanId == null ? 0 : (int)bookingMeta.RatePlanId;
                int branchId = (int)bookingMeta.BranchID;

                // Resolve room type and rate plan for accurate weekend/special-day pricing.
                const string roomTypeSql = "SELECT * FROM RoomTypes WHERE Id = @RoomTypeId AND IsActive = 1";
                var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(
                    roomTypeSql,
                    new { RoomTypeId = roomTypeId },
                    transaction);

                const string rateSql = @"
                    SELECT TOP 1 *
                    FROM RateMaster
                    WHERE Id = @RatePlanId AND BranchID = @BranchID AND IsActive = 1";

                var ratePlan = ratePlanId > 0
                    ? await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(
                        rateSql,
                        new { RatePlanId = ratePlanId, BranchID = branchId },
                        transaction)
                    : null;

                if (roomType != null)
                {
                    var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(branchId);
                    var checkInTime = hotelSettings?.CheckInTime ?? new TimeSpan(14, 0, 0);
                    var checkOutTime = hotelSettings?.CheckOutTime ?? new TimeSpan(12, 0, 0);

                    // Keep nights consistent with system rule.
                    var computedNights = CalculateNights(checkInDate, checkOutDate, checkInTime, checkOutTime);
                    nights = computedNights;

                    var taxPercentage = ratePlan?.TaxPercentage ?? 0;
                    var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
                    var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);

                    var totalGuests = adults + children;
                    var extraGuests = Math.Max(0, totalGuests - roomType.MaxOccupancy);

                    // Build AFTER-discount nightly amounts (this matches what we charge and what nights store).
                    var breakdown = await BuildRoomNightBreakdownAsync(
                        ratePlan?.Id ?? 0,
                        checkInDate,
                        checkOutDate,
                        ratePlan?.BaseRate ?? roomType.BaseRate,
                        ratePlan?.ExtraPaxRate ?? 0,
                        extraGuests,
                        taxPercentage,
                        cgstPercentage,
                        sgstPercentage,
                        transaction);

                    var nightlyBaseAfterDiscountSum = breakdown.Sum(x => x.rateAmountAfterDiscount);
                    var nightlyTaxSum = breakdown.Sum(x => x.taxAmount);
                    var nightlyCGSTSum = breakdown.Sum(x => x.cgstAmount);
                    var nightlySGSTSum = breakdown.Sum(x => x.sgstAmount);

                    var rr = requiredRooms > 0 ? requiredRooms : 1;
                    baseAmount = Math.Round(nightlyBaseAfterDiscountSum * rr, 2, MidpointRounding.AwayFromZero);
                    taxAmount = Math.Round(nightlyTaxSum * rr, 2, MidpointRounding.AwayFromZero);
                    cgstAmount = Math.Round(nightlyCGSTSum * rr, 2, MidpointRounding.AwayFromZero);
                    sgstAmount = Math.Round(nightlySGSTSum * rr, 2, MidpointRounding.AwayFromZero);

                    // Derive discount amount from RateMaster discount percent over ORIGINAL totals.
                    decimal discountPercent = 0;
                    if (ratePlan?.Id > 0)
                    {
                        const string discountSql = "SELECT ApplyDiscount FROM RateMaster WHERE Id = @RateMasterId";
                        var discountStr = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                            discountSql,
                            new { RateMasterId = ratePlan.Id },
                            transaction);

                        if (!string.IsNullOrWhiteSpace(discountStr) && decimal.TryParse(discountStr, out var parsedDiscount))
                        {
                            discountPercent = parsedDiscount;
                        }
                    }

                    discountAmount = 0;
                    if (discountPercent > 0)
                    {
                        var divisor = 1 - (discountPercent / 100m);
                        if (divisor > 0)
                        {
                            var originalTotal = Math.Round(baseAmount / divisor, 2, MidpointRounding.AwayFromZero);
                            discountAmount = Math.Round(originalTotal - baseAmount, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
                
                // Store old values for audit log
                var oldCheckIn = (DateTime)booking.CheckInDate;
                var oldCheckOut = (DateTime)booking.CheckOutDate;
                var oldNights = (int)booking.Nights;
                var oldBaseAmount = (decimal)booking.BaseAmount;
                var oldTaxAmount = (decimal)booking.TaxAmount;
                var oldTotalAmount = (decimal)booking.TotalAmount;

                // Ensure totals remain consistent when dates change.
                // BaseAmount is AFTER-discount room total, DiscountAmount is total discount across stay.
                var computedTotalAmount = Math.Round(baseAmount + taxAmount, 2, MidpointRounding.AwayFromZero);

                // Calculate new balance
                var balanceAmount = computedTotalAmount - depositAmount;

                // Update booking with new dates and amounts
                const string updateBookingSql = @"
                    UPDATE Bookings 
                    SET CheckInDate = @CheckInDate,
                        CheckOutDate = @CheckOutDate,
                        Nights = @Nights,
                        BaseAmount = @BaseAmount,
                        DiscountAmount = @DiscountAmount,
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
                        DiscountAmount = discountAmount,
                        TaxAmount = taxAmount,
                        CGSTAmount = cgstAmount,
                        SGSTAmount = sgstAmount,
                        TotalAmount = computedTotalAmount,
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

                // Regenerate ReservationRoomNights based on updated dates (before room assignment receipt use-case).
                await UpsertReservationRoomNightsAsync(
                    bookingId,
                    checkInDate,
                    checkOutDate,
                    baseAmount,
                    discountAmount,
                    taxAmount,
                    cgstAmount,
                    sgstAmount,
                    nights,
                    transaction);

                // If this booking has multiple assigned rooms (BookingRooms), regenerate nights per-room.
                // This ensures BookingRoomNights.RoomId is populated for each room and avoids multi-room calculation drift.
                var assignedRoomIds = (await _dbConnection.QueryAsync<int>(
                        @"SELECT br.RoomId FROM BookingRooms br WHERE br.BookingId = @BookingId AND br.IsActive = 1",
                        new { BookingId = bookingId },
                        transaction))
                    .Distinct()
                    .ToList();

                var shouldWritePerRoomNights = assignedRoomIds.Count > 0;

                // Create new BookingRoomNights records if room is assigned
                if (roomId.HasValue || shouldWritePerRoomNights)
                {
                    // Prefer true night-wise rates when possible (special day/weekend), fallback to even split.
                    var nightlyBreakdown = new List<(DateTime date, decimal rateAmountAfterDiscount, decimal actualBaseRate, decimal discountAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount)>();
                    try
                    {
                        if (bookingMeta != null && roomType != null)
                        {
                            var rateMasterIdForNights = ratePlan?.Id ?? 0;
                            var taxPercentageForNights = ratePlan?.TaxPercentage ?? 0;
                            var cgstPercentageForNights = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentageForNights / 2);
                            var sgstPercentageForNights = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentageForNights / 2);

                            var totalGuests = adults + children;
                            var extraGuests = Math.Max(0, totalGuests - roomType.MaxOccupancy);

                            nightlyBreakdown = await BuildRoomNightBreakdownAsync(
                                rateMasterIdForNights,
                                checkInDate,
                                checkOutDate,
                                ratePlan?.BaseRate ?? roomType.BaseRate,
                                ratePlan?.ExtraPaxRate ?? 0,
                                extraGuests,
                                taxPercentageForNights,
                                cgstPercentageForNights,
                                sgstPercentageForNights,
                                transaction);
                        }
                    }
                    catch
                    {
                        nightlyBreakdown = new List<(DateTime, decimal, decimal, decimal, decimal, decimal, decimal)>();
                    }

                    var fallbackNightlyRoomAmount = nights > 0 ? Math.Round(baseAmount / nights, 2, MidpointRounding.AwayFromZero) : 0m;
                    var fallbackNightlyTax = nights > 0 ? Math.Round(taxAmount / nights, 2, MidpointRounding.AwayFromZero) : 0m;
                    var fallbackNightlyCGST = nights > 0 ? Math.Round(cgstAmount / nights, 2, MidpointRounding.AwayFromZero) : 0m;
                    var fallbackNightlySGST = nights > 0 ? Math.Round(sgstAmount / nights, 2, MidpointRounding.AwayFromZero) : 0m;

                    const string insertRoomNightSql = @"
                        INSERT INTO BookingRoomNights (BookingId, RoomId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                        VALUES (@BookingId, @RoomId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status)";

                    // If assigned rooms exist, write one row per room per date.
                    // Otherwise, fall back to using the single booking RoomId.
                    var targetRoomIds = shouldWritePerRoomNights
                        ? assignedRoomIds
                        : new List<int> { roomId!.Value };

                    // Determine how to distribute totals.
                    // When writing per-room rows, each row should contain per-room amounts.
                    // When writing a single room id, we keep legacy behavior (single room).
                    var perRoomDivisor = shouldWritePerRoomNights ? (targetRoomIds.Count > 0 ? targetRoomIds.Count : 1) : 1;

                    for (var date = checkInDate.Date; date < checkOutDate.Date; date = date.AddDays(1))
                    {
                        var match = nightlyBreakdown.FirstOrDefault(x => x.date.Date == date);

                        // Values from nightlyBreakdown are per-room.
                        // Fallback values (baseAmount/discount/tax) are booking totals, so split by number of rooms.
                        var nightRatePerRoom = match.date == default
                            ? Math.Round(fallbackNightlyRoomAmount / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(match.rateAmountAfterDiscount, 2, MidpointRounding.AwayFromZero);

                        var nightActualBasePerRoom = match.date == default
                            ? (discountAmount > 0 && nights > 0
                                ? Math.Round(((baseAmount + discountAmount) / nights) / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                                : Math.Round((baseAmount / nights) / perRoomDivisor, 2, MidpointRounding.AwayFromZero))
                            : Math.Round(match.actualBaseRate, 2, MidpointRounding.AwayFromZero);

                        var nightDiscountPerRoom = match.date == default
                            ? (discountAmount > 0 && nights > 0
                                ? Math.Round((discountAmount / nights) / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                                : 0m)
                            : Math.Round(match.discountAmount, 2, MidpointRounding.AwayFromZero);

                        var nightTaxPerRoom = match.date == default
                            ? Math.Round(fallbackNightlyTax / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(match.taxAmount, 2, MidpointRounding.AwayFromZero);

                        var nightCGSTPerRoom = match.date == default
                            ? Math.Round(fallbackNightlyCGST / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(match.cgstAmount, 2, MidpointRounding.AwayFromZero);

                        var nightSGSTPerRoom = match.date == default
                            ? Math.Round(fallbackNightlySGST / perRoomDivisor, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(match.sgstAmount, 2, MidpointRounding.AwayFromZero);

                        foreach (var rid in targetRoomIds)
                        {
                            await _dbConnection.ExecuteAsync(
                                insertRoomNightSql,
                                new
                                {
                                    BookingId = bookingId,
                                    RoomId = rid,
                                    StayDate = date,
                                    RateAmount = nightRatePerRoom,
                                    ActualBaseRate = nightActualBasePerRoom,
                                    DiscountAmount = nightDiscountPerRoom,
                                    TaxAmount = nightTaxPerRoom,
                                    CGSTAmount = nightCGSTPerRoom,
                                    SGSTAmount = nightSGSTPerRoom,
                                    Status = "Reserved"
                                },
                                transaction
                            );
                        }
                    }
                }

                // Log the date change in audit log
                var oldValueStr = $"Check-In: {oldCheckIn:dd/MM/yyyy}, Check-Out: {oldCheckOut:dd/MM/yyyy}, Nights: {oldNights}, Room Total: ₹{oldBaseAmount:N2}, Tax: ₹{oldTaxAmount:N2}, Grand Total: ₹{oldTotalAmount:N2}";
                var newValueStr = $"Check-In: {checkInDate:dd/MM/yyyy}, Check-Out: {checkOutDate:dd/MM/yyyy}, Nights: {nights}, Room Total: ₹{baseAmount:N2}, Discount: ₹{discountAmount:N2}, Tax: ₹{taxAmount:N2}, Grand Total: ₹{computedTotalAmount:N2}";
                
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

        private async Task UpsertReservationRoomNightsAsync(
            int bookingId,
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal baseAmountAfterDiscount,
            decimal discountAmount,
            decimal taxAmount,
            decimal cgstAmount,
            decimal sgstAmount,
            int nights,
            IDbTransaction transaction)
        {
            // Keep it simple and safe: delete+insert within the same transaction.
            const string deleteSql = @"DELETE FROM ReservationRoomNights WHERE BookingId = @BookingId";
            await _dbConnection.ExecuteAsync(deleteSql, new { BookingId = bookingId }, transaction);

            if (nights <= 0)
            {
                return;
            }

            // ReservationRoomNights stores plan totals per date for the whole booking (not per room).
            // We must follow the same weekend/special-day/standard logic used for BookingRoomNights.
            // Receipt will handle RequiredRooms multiplier when needed.
            var nightlyBreakdown = new List<(DateTime date, decimal rateAmountAfterDiscount, decimal actualBaseRate, decimal discountAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount)>();
            try
            {
                const string bookingMetaSql = @"
                    SELECT TOP 1 b.BranchID, b.RoomTypeId, b.RatePlanId, b.Adults, b.Children
                    FROM Bookings b
                    WHERE b.Id = @BookingId";

                var bookingMeta = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    bookingMetaSql,
                    new { BookingId = bookingId },
                    transaction);

                if (bookingMeta != null)
                {
                    var ratePlanId = bookingMeta.RatePlanId as int? ?? 0;
                    var roomTypeId = (int)bookingMeta.RoomTypeId;

                    const string roomTypeSql = "SELECT * FROM RoomTypes WHERE Id = @Id AND IsActive = 1";
                    var roomType = await _dbConnection.QueryFirstOrDefaultAsync<RoomType>(
                        roomTypeSql,
                        new { Id = roomTypeId },
                        transaction);

                    RateMaster? ratePlan = null;
                    if (ratePlanId > 0)
                    {
                        ratePlan = await _dbConnection.QueryFirstOrDefaultAsync<RateMaster>(
                            "SELECT TOP 1 * FROM RateMaster WHERE Id = @Id AND IsActive = 1",
                            new { Id = ratePlanId },
                            transaction);
                    }

                    if (roomType != null)
                    {
                        var adults = (int)(bookingMeta.Adults ?? 0);
                        var children = (int)(bookingMeta.Children ?? 0);
                        var totalGuests = adults + children;
                        var extraGuests = Math.Max(0, totalGuests - roomType.MaxOccupancy);

                        var taxPercentage = ratePlan?.TaxPercentage ?? 0;
                        var cgstPercentage = (ratePlan?.CGSTPercentage > 0 ? ratePlan.CGSTPercentage : taxPercentage / 2);
                        var sgstPercentage = (ratePlan?.SGSTPercentage > 0 ? ratePlan.SGSTPercentage : taxPercentage / 2);

                        var defaultBaseRate = ratePlan?.BaseRate ?? roomType.BaseRate;
                        var defaultExtraPaxRate = ratePlan?.ExtraPaxRate ?? 0;

                        nightlyBreakdown = await BuildRoomNightBreakdownAsync(
                            ratePlan?.Id ?? 0,
                            checkInDate,
                            checkOutDate,
                            defaultBaseRate,
                            defaultExtraPaxRate,
                            extraGuests,
                            taxPercentage,
                            cgstPercentage,
                            sgstPercentage,
                            transaction);
                    }
                }
            }
            catch
            {
                nightlyBreakdown = new List<(DateTime date, decimal rateAmountAfterDiscount, decimal actualBaseRate, decimal discountAmount, decimal taxAmount, decimal cgstAmount, decimal sgstAmount)>();
            }

            // Fallback: even split (legacy safe behavior) when we can't compute per-day rates.
            var fallbackNightlyAfterDiscount = Math.Round(baseAmountAfterDiscount / nights, 2, MidpointRounding.AwayFromZero);
            var fallbackNightlyTax = Math.Round(taxAmount / nights, 2, MidpointRounding.AwayFromZero);
            var fallbackNightlyCgst = Math.Round(cgstAmount / nights, 2, MidpointRounding.AwayFromZero);
            var fallbackNightlySgst = Math.Round(sgstAmount / nights, 2, MidpointRounding.AwayFromZero);
            var fallbackNightlyDiscount = discountAmount > 0
                ? Math.Round(discountAmount / nights, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var fallbackNightlyActualBase = Math.Round(fallbackNightlyAfterDiscount + fallbackNightlyDiscount, 2, MidpointRounding.AwayFromZero);

            const string insertSql = @"
                INSERT INTO ReservationRoomNights (BookingId, StayDate, RateAmount, ActualBaseRate, DiscountAmount, TaxAmount, CGSTAmount, SGSTAmount, Status)
                VALUES (@BookingId, @StayDate, @RateAmount, @ActualBaseRate, @DiscountAmount, @TaxAmount, @CGSTAmount, @SGSTAmount, @Status);";

            for (var date = checkInDate.Date; date < checkOutDate.Date; date = date.AddDays(1))
            {
                var match = nightlyBreakdown.FirstOrDefault(x => x.date.Date == date);
                var nightRate = match.date == default
                    ? fallbackNightlyAfterDiscount
                    : Math.Round(match.rateAmountAfterDiscount, 2, MidpointRounding.AwayFromZero);

                var nightActualBase = match.date == default
                    ? fallbackNightlyActualBase
                    : Math.Round(match.actualBaseRate, 2, MidpointRounding.AwayFromZero);

                var nightDiscount = match.date == default
                    ? fallbackNightlyDiscount
                    : Math.Round(match.discountAmount, 2, MidpointRounding.AwayFromZero);

                var nightTax = match.date == default
                    ? fallbackNightlyTax
                    : Math.Round(match.taxAmount, 2, MidpointRounding.AwayFromZero);

                var nightCgst = match.date == default
                    ? fallbackNightlyCgst
                    : Math.Round(match.cgstAmount, 2, MidpointRounding.AwayFromZero);

                var nightSgst = match.date == default
                    ? fallbackNightlySgst
                    : Math.Round(match.sgstAmount, 2, MidpointRounding.AwayFromZero);

                await _dbConnection.ExecuteAsync(
                    insertSql,
                    new
                    {
                        BookingId = bookingId,
                        StayDate = date,
                        RateAmount = nightRate,
                        ActualBaseRate = nightActualBase,
                        DiscountAmount = nightDiscount,
                        TaxAmount = nightTax,
                        CGSTAmount = nightCgst,
                        SGSTAmount = nightSgst,
                        Status = "Reserved"
                    },
                    transaction);
            }
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

        public async Task<IEnumerable<string>> GetAssignedRoomNumbersAsync(int bookingId)
        {
            const string sql = @"
                SELECT r.RoomNumber 
                FROM BookingRooms br
                INNER JOIN Rooms r ON br.RoomId = r.Id
                WHERE br.BookingId = @BookingId 
                  AND br.IsActive = 1
                ORDER BY r.RoomNumber";

            return await _dbConnection.QueryAsync<string>(sql, new { BookingId = bookingId });
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
                // Ensure QUOTED_IDENTIFIER is ON for the indexed column
                await _dbConnection.ExecuteAsync("SET QUOTED_IDENTIFIER ON", transaction: transaction);

                // Generate receipt number
                var receiptNumber = await GenerateReceiptNumberAsync(transaction);
                payment.ReceiptNumber = receiptNumber;

                // Insert payment
                const string insertPaymentSql = @"
                    INSERT INTO BookingPayments (
                        BookingId, ReceiptNumber,
                        Amount, DiscountAmount, DiscountPercent, RoundOffAmount, IsRoundOffApplied,
                        PaymentMethod, PaymentReference, Status, PaidOn, Notes,
                        CardType, CardLastFourDigits, BankId, ChequeDate,
                        CreatedBy, IsAdvancePayment
                    )
                    VALUES (
                        @BookingId, @ReceiptNumber,
                        @Amount, @DiscountAmount, @DiscountPercent, @RoundOffAmount, @IsRoundOffApplied,
                        @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes,
                        @CardType, @CardLastFourDigits, @BankId, @ChequeDate,
                        @CreatedBy, @IsAdvancePayment
                    )";

                var paymentParams = new
                {
                    payment.BookingId,
                    payment.ReceiptNumber,
                    payment.Amount,
                    payment.DiscountAmount,
                    payment.DiscountPercent,
                    payment.RoundOffAmount,
                    payment.IsRoundOffApplied,
                    payment.PaymentMethod,
                    payment.PaymentReference,
                    payment.Status,
                    payment.PaidOn,
                    payment.Notes,
                    payment.CardType,
                    payment.CardLastFourDigits,
                    payment.BankId,
                    payment.ChequeDate,
                    CreatedBy = performedBy,
                    payment.IsAdvancePayment
                };

                try
                {
                    await _dbConnection.ExecuteAsync(insertPaymentSql, paymentParams, transaction);
                }
                catch (Exception ex) when (
                    ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("DiscountAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("DiscountPercent", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("RoundOffAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("IsRoundOffApplied", StringComparison.OrdinalIgnoreCase))
                {
                    // Backward-compatible insert for DBs that haven't run the migration yet.
                    const string legacyInsertPaymentSql = @"
                        INSERT INTO BookingPayments (BookingId, ReceiptNumber, Amount, PaymentMethod, PaymentReference, Status, PaidOn, Notes, CardType, CardLastFourDigits, BankId, ChequeDate, CreatedBy, IsAdvancePayment)
                        VALUES (@BookingId, @ReceiptNumber, @Amount, @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes, @CardType, @CardLastFourDigits, @BankId, @ChequeDate, @CreatedBy, @IsAdvancePayment)";

                    var legacyParams = new
                    {
                        payment.BookingId,
                        payment.ReceiptNumber,
                        payment.Amount,
                        payment.PaymentMethod,
                        payment.PaymentReference,
                        payment.Status,
                        payment.PaidOn,
                        payment.Notes,
                        payment.CardType,
                        payment.CardLastFourDigits,
                        payment.BankId,
                        payment.ChequeDate,
                        CreatedBy = performedBy,
                        payment.IsAdvancePayment
                    };

                    await _dbConnection.ExecuteAsync(legacyInsertPaymentSql, legacyParams, transaction);
                }

                // Compute other charges grand total safely (may not exist on older DBs)
                decimal otherChargesGrandTotal = 0m;
                try
                {
                    const string otherChargesTotalSql = @"
                        SELECT ISNULL(SUM((Rate * CASE WHEN Qty IS NULL OR Qty <= 0 THEN 1 ELSE Qty END) + GSTAmount), 0)
                        FROM BookingOtherCharges
                        WHERE BookingId = @BookingId
                          AND IsActive = 1";

                    otherChargesGrandTotal = await _dbConnection.ExecuteScalarAsync<decimal>(
                        otherChargesTotalSql,
                        new { BookingId = payment.BookingId },
                        transaction
                    );
                }
                catch
                {
                    // Best-effort only: if the table/columns don't exist yet, treat as 0
                    otherChargesGrandTotal = 0m;
                }

                // Update booking deposit and balance
                // DepositAmount tracks actual money received (net).
                // BalanceAmount must reduce by the total adjustment applied against dues: net + discount + round-off.
                var discountApplied = payment.DiscountAmount;
                var roundOffApplied = payment.IsRoundOffApplied ? payment.RoundOffAmount : 0m;
                var appliedToBalance = payment.Amount + discountApplied + roundOffApplied;

                const string updateBookingSql = @"
                    UPDATE Bookings 
                    SET DepositAmount = DepositAmount + @NetAmount,
                        BalanceAmount = BalanceAmount - @AppliedToBalance,
                        PaymentStatus = CASE 
                            WHEN ((BalanceAmount - @AppliedToBalance) + @OtherChargesGrandTotal) <= 0 THEN 'Paid'
                            WHEN (DepositAmount + @NetAmount) > 0 THEN 'Partially Paid'
                            ELSE 'Pending'
                        END,
                        LastModifiedBy = @PerformedBy,
                        LastModifiedDate = GETDATE()
                    WHERE Id = @BookingId";

                await _dbConnection.ExecuteAsync(updateBookingSql, new 
                { 
                    BookingId = payment.BookingId,
                    NetAmount = payment.Amount,
                    AppliedToBalance = appliedToBalance,
                    OtherChargesGrandTotal = otherChargesGrandTotal,
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

        public async Task<bool> RecalculateBookingFinancialsAsync(int bookingId, int performedBy)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                const string getBookingSql = @"SELECT Id, TotalAmount, DepositAmount, BalanceAmount FROM Bookings WHERE Id = @BookingId";
                var booking = await _dbConnection.QueryFirstOrDefaultAsync<Booking>(getBookingSql, new { BookingId = bookingId }, transaction);
                if (booking == null)
                {
                    transaction.Rollback();
                    return false;
                }

                decimal appliedToBalance;
                decimal depositAmount;

                try
                {
                    const string paymentsSql = @"
                        SELECT
                            ISNULL(SUM(Amount), 0) AS DepositAmount,
                            ISNULL(SUM(
                                Amount
                                + ISNULL(DiscountAmount, 0)
                                + CASE WHEN ISNULL(IsRoundOffApplied, 0) = 1 THEN ISNULL(RoundOffAmount, 0) ELSE 0 END
                            ), 0) AS AppliedToBalance
                        FROM BookingPayments
                        WHERE BookingId = @BookingId";

                    var row = await _dbConnection.QueryFirstAsync<dynamic>(paymentsSql, new { BookingId = bookingId }, transaction);
                    depositAmount = row.DepositAmount is decimal d1 ? d1 : Convert.ToDecimal(row.DepositAmount ?? 0m);
                    appliedToBalance = row.AppliedToBalance is decimal d2 ? d2 : Convert.ToDecimal(row.AppliedToBalance ?? 0m);
                }
                catch (Exception ex) when (
                    ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("DiscountAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("RoundOffAmount", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("IsRoundOffApplied", StringComparison.OrdinalIgnoreCase))
                {
                    const string legacyPaymentsSql = @"
                        SELECT ISNULL(SUM(Amount), 0)
                        FROM BookingPayments
                        WHERE BookingId = @BookingId";

                    depositAmount = await _dbConnection.ExecuteScalarAsync<decimal>(legacyPaymentsSql, new { BookingId = bookingId }, transaction);
                    appliedToBalance = depositAmount;
                }

                var expectedBalance = booking.TotalAmount - appliedToBalance;
                var expectedDeposit = depositAmount;

                const string updateSql = @"
                    UPDATE Bookings
                    SET DepositAmount = @DepositAmount,
                        BalanceAmount = @BalanceAmount,
                        PaymentStatus = CASE
                            WHEN @BalanceAmount <= 0 THEN 'Paid'
                            WHEN @DepositAmount > 0 THEN 'Partially Paid'
                            ELSE 'Pending'
                        END,
                        LastModifiedBy = @PerformedBy,
                        LastModifiedDate = GETDATE()
                    WHERE Id = @BookingId";

                await _dbConnection.ExecuteAsync(updateSql, new
                {
                    BookingId = bookingId,
                    DepositAmount = expectedDeposit,
                    BalanceAmount = expectedBalance,
                    PerformedBy = performedBy
                }, transaction);

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
                // Ensure BookingGuests.GuestId is populated when possible
                if (guest.GuestId <= 0 && !string.IsNullOrWhiteSpace(guest.Phone))
                {
                    const string findGuestIdByPhoneSql = "SELECT TOP 1 Id FROM Guests WHERE Phone = @Phone AND IsActive = 1 ORDER BY LastModifiedDate DESC";
                    var existingGuestId = await _dbConnection.QueryFirstOrDefaultAsync<int?>(findGuestIdByPhoneSql, new { Phone = guest.Phone }, transaction);
                    if (existingGuestId.HasValue)
                    {
                        guest.GuestId = existingGuestId.Value;
                    }
                }

                // Insert into BookingGuests table
                const string bookingGuestSql = @"
                    INSERT INTO BookingGuests (BookingId, GuestId, FullName, Email, Phone, GuestType, IsPrimary, 
                                             RelationshipToPrimary, Age, DateOfBirth, Gender, IdentityType, 
                                             IdentityNumber, DocumentPath, Address, City, State, Country,
                                             Pincode, CountryId, StateId, CityId, CreatedDate, CreatedBy)
                    VALUES (@BookingId, @GuestId, @FullName, @Email, @Phone, @GuestType, 0, 
                            @RelationshipToPrimary, @Age, @DateOfBirth, @Gender, @IdentityType, 
                            @IdentityNumber, @DocumentPath, @Address, @City, @State, @Country,
                            @Pincode, @CountryId, @StateId, @CityId, GETDATE(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)";

                var bookingGuestId = await _dbConnection.ExecuteScalarAsync<int>(bookingGuestSql, guest, transaction);

                if (bookingGuestId > 0)
                {
                    // Split FullName into FirstName and LastName
                    var nameParts = guest.FullName.Trim().Split(new[] { ' ' }, 2);
                    var firstName = nameParts[0];
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    // Check if guest exists by phone in Guests table
                    const string findGuestSql = "SELECT TOP 1 * FROM Guests WHERE Phone = @Phone AND IsActive = 1 ORDER BY LastModifiedDate DESC";
                    var existingGuest = await _dbConnection.QueryFirstOrDefaultAsync<Guest>(findGuestSql, new { Phone = guest.Phone }, transaction);

                    if (existingGuest != null)
                    {
                        // Update existing guest in Guests table
                        const string updateGuestSql = @"
                            UPDATE Guests SET 
                                FirstName = @FirstName, 
                                LastName = @LastName, 
                                Email = @Email, 
                                GuestType = @GuestType, 
                                DateOfBirth = @DateOfBirth,
                                Gender = @Gender,
                                IdentityType = @IdentityType,
                                IdentityNumber = @IdentityNumber,
                                Address = @Address,
                                City = @City,
                                State = @State,
                                Country = @Country,
                                Pincode = @Pincode,
                                CountryId = @CountryId,
                                StateId = @StateId,
                                CityId = @CityId,
                                BranchID = @BranchID,
                                LastModifiedDate = GETDATE()
                            WHERE Id = @Id";

                        await _dbConnection.ExecuteAsync(updateGuestSql, new
                        {
                            Id = existingGuest.Id,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = guest.Email ?? "",
                            GuestType = guest.GuestType ?? "Companion",
                            DateOfBirth = guest.DateOfBirth,
                            Gender = guest.Gender,
                            IdentityType = guest.IdentityType,
                            IdentityNumber = guest.IdentityNumber,
                            Address = guest.Address,
                            City = guest.City,
                            State = guest.State,
                            Country = guest.Country,
                            Pincode = guest.Pincode,
                            CountryId = guest.CountryId,
                            StateId = guest.StateId,
                            CityId = guest.CityId,
                            BranchID = branchId
                        }, transaction);
                    }
                    else
                    {
                        // Insert new guest into Guests table
                        const string insertGuestSql = @"
                            INSERT INTO Guests (FirstName, LastName, Email, Phone, GuestType, BranchID, 
                                              DateOfBirth, Gender, IdentityType, IdentityNumber, Address, City, State, Country,
                                              Pincode, CountryId, StateId, CityId,
                                              IsActive, CreatedDate, LastModifiedDate)
                            VALUES (@FirstName, @LastName, @Email, @Phone, @GuestType, @BranchID, 
                                    @DateOfBirth, @Gender, @IdentityType, @IdentityNumber, @Address, @City, @State, @Country,
                                    @Pincode, @CountryId, @StateId, @CityId,
                                    1, GETDATE(), GETDATE())";

                        await _dbConnection.ExecuteAsync(insertGuestSql, new
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = guest.Email ?? "",
                            Phone = guest.Phone ?? "",
                            GuestType = guest.GuestType ?? "Companion",
                            BranchID = branchId,
                            DateOfBirth = guest.DateOfBirth,
                            Gender = guest.Gender,
                            IdentityType = guest.IdentityType,
                            IdentityNumber = guest.IdentityNumber,
                            Address = guest.Address,
                            City = guest.City,
                            State = guest.State,
                            Country = guest.Country,
                            Pincode = guest.Pincode,
                            CountryId = guest.CountryId,
                            StateId = guest.StateId,
                            CityId = guest.CityId
                        }, transaction);

                        // Link the newly created/ensured master Guest record back to BookingGuests.GuestId
                        // Best-effort: set GuestId for the just inserted BookingGuests row by phone.
                        const string linkGuestIdSql = @"
                            UPDATE bg
                            SET bg.GuestId = g.Id
                            FROM BookingGuests bg
                            INNER JOIN Guests g ON g.Phone = bg.Phone AND g.IsActive = 1
                            WHERE bg.Id = @BookingGuestId AND (bg.GuestId IS NULL OR bg.GuestId = 0);";

                        await _dbConnection.ExecuteAsync(linkGuestIdSql, new { BookingGuestId = bookingGuestId }, transaction);
                    }
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

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // Update BookingGuests table
                const string sql = @"
                    UPDATE BookingGuests 
                    SET FullName = @FullName,
                        Email = @Email,
                        Phone = @Phone,
                        GuestType = @GuestType,
                        RelationshipToPrimary = @RelationshipToPrimary,
                        Age = @Age,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        IdentityType = @IdentityType,
                        IdentityNumber = @IdentityNumber,
                        DocumentPath = @DocumentPath,
                        Address = @Address,
                        City = @City,
                        State = @State,
                        Country = @Country,
                        Pincode = @Pincode,
                        CountryId = @CountryId,
                        StateId = @StateId,
                        CityId = @CityId,
                        ModifiedDate = GETDATE(),
                        ModifiedBy = @ModifiedBy
                    WHERE Id = @Id AND IsActive = 1";

                var rowsAffected = await _dbConnection.ExecuteAsync(sql, guest, transaction);
                
                if (rowsAffected > 0 && !string.IsNullOrWhiteSpace(guest.Phone))
                {
                    // Get BranchID from the booking
                    const string getBranchSql = "SELECT BranchID FROM Bookings WHERE Id = @BookingId";
                    var branchId = await _dbConnection.QueryFirstOrDefaultAsync<int?>(getBranchSql, new { guest.BookingId }, transaction);
                    
                    // Split FullName into FirstName and LastName
                    var nameParts = guest.FullName?.Split(' ', 2) ?? new string[] { "", "" };
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    // Check if guest exists in Guests table
                    const string checkGuestSql = "SELECT Id FROM Guests WHERE Phone = @Phone AND IsActive = 1";
                    var existingGuestId = await _dbConnection.QueryFirstOrDefaultAsync<int?>(checkGuestSql, new { guest.Phone }, transaction);

                    if (existingGuestId.HasValue)
                    {
                        // Update existing guest in Guests table
                        const string updateGuestSql = @"
                            UPDATE Guests 
                            SET FirstName = @FirstName,
                                LastName = @LastName,
                                Email = @Email,
                                Phone = @Phone,
                                Address = @Address,
                                City = @City,
                                State = @State,
                                Country = @Country,
                                Pincode = @Pincode,
                                CountryId = @CountryId,
                                StateId = @StateId,
                                CityId = @CityId,
                                DateOfBirth = @DateOfBirth,
                                Gender = @Gender,
                                IdentityType = @IdentityType,
                                IdentityNumber = @IdentityNumber,
                                LastModifiedDate = GETDATE()
                            WHERE Id = @GuestId AND IsActive = 1";

                        await _dbConnection.ExecuteAsync(updateGuestSql, new
                        {
                            GuestId = existingGuestId.Value,
                            FirstName = firstName,
                            LastName = lastName,
                            guest.Email,
                            guest.Phone,
                            guest.Address,
                            guest.City,
                            guest.State,
                            guest.Country,
                            guest.Pincode,
                            guest.CountryId,
                            guest.StateId,
                            guest.CityId,
                            guest.DateOfBirth,
                            guest.Gender,
                            guest.IdentityType,
                            guest.IdentityNumber
                        }, transaction);
                    }
                    else
                    {
                        // Insert new guest in Guests table if not exists
                        const string insertGuestSql = @"
                            INSERT INTO Guests (FirstName, LastName, Email, Phone, Address, City, State, Country, Pincode, 
                                CountryId, StateId, CityId, DateOfBirth, Gender, IdentityType, IdentityNumber, BranchID, IsActive, CreatedDate, LastModifiedDate)
                            VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @City, @State, @Country, @Pincode,
                                @CountryId, @StateId, @CityId, @DateOfBirth, @Gender, @IdentityType, @IdentityNumber, @BranchID, 1, GETDATE(), GETDATE())";

                        await _dbConnection.ExecuteAsync(insertGuestSql, new
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            guest.Email,
                            guest.Phone,
                            guest.Address,
                            guest.City,
                            guest.State,
                            guest.Country,
                            guest.Pincode,
                            guest.CountryId,
                            guest.StateId,
                            guest.CityId,
                            guest.DateOfBirth,
                            guest.Gender,
                            guest.IdentityType,
                            guest.IdentityNumber,
                            BranchID = branchId
                        }, transaction);
                    }
                }

                transaction.Commit();
                return rowsAffected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateLatestGuestDocumentPathAsync(int guestId, string documentPath, int performedBy)
        {
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }

            const string sql = @"
                UPDATE bg
                SET bg.DocumentPath = @DocumentPath,
                    bg.ModifiedDate = GETDATE(),
                    bg.ModifiedBy = @PerformedBy
                FROM BookingGuests bg
                WHERE bg.Id = (
                    SELECT TOP 1 Id
                    FROM BookingGuests
                    WHERE GuestId = @GuestId AND IsActive = 1
                    ORDER BY CreatedDate DESC, Id DESC
                );";

            var rows = await _dbConnection.ExecuteAsync(sql, new
            {
                GuestId = guestId,
                DocumentPath = documentPath,
                PerformedBy = performedBy
            });

            return rows > 0;
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

        /// <summary>
        /// Generates a unique receipt number for a payment
        /// Format: RCP-YYYYMMDD-####
        /// </summary>
        private async Task<string> GenerateReceiptNumberAsync(IDbTransaction transaction)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            
            // Get the count of receipts generated today
            const string countSql = @"
                SELECT COUNT(*) 
                FROM BookingPayments 
                WHERE ReceiptNumber LIKE @Pattern";
            
            var pattern = $"RCP-{today}-%";
            var count = await _dbConnection.ExecuteScalarAsync<int>(countSql, new { Pattern = pattern }, transaction);
            
            // Generate receipt number with sequential number
            var sequenceNumber = (count + 1).ToString("D4");
            return $"RCP-{today}-{sequenceNumber}";
        }
    }
}
