using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    // ── BanquetVenueRepository ────────────────────────────────────────────────

    public class BanquetVenueRepository : IBanquetVenueRepository
    {
        private readonly IDbConnection _db;
        public BanquetVenueRepository(IDbConnection db) => _db = db;

        public async Task<IEnumerable<BanquetVenue>> GetByBranchAsync(int branchId, bool activeOnly = true)
        {
            var sql = $@"
                SELECT v.*, f.FloorName, gs.SlabName AS GstSlabName
                FROM BanquetVenues v
                LEFT JOIN Floors f ON f.Id = v.FloorId
                LEFT JOIN GstSlabs gs ON gs.Id = v.GstSlabId
                WHERE v.BranchID = @BranchID
                  {(activeOnly ? "AND v.IsActive = 1" : "")}
                ORDER BY v.VenueName";
            return await _db.QueryAsync<BanquetVenue>(sql, new { BranchID = branchId });
        }

        public async Task<BanquetVenue?> GetByIdAsync(int id)
        {
            var sql = @"
                SELECT v.*, f.FloorName, gs.SlabName AS GstSlabName
                FROM BanquetVenues v
                LEFT JOIN Floors f ON f.Id = v.FloorId
                LEFT JOIN GstSlabs gs ON gs.Id = v.GstSlabId
                WHERE v.Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<BanquetVenue>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(BanquetVenue v)
        {
            var sql = @"
                INSERT INTO BanquetVenues
                    (VenueCode,VenueName,VenueType,CapacitySeated,CapacityBuffet,CapacityTheater,CapacityCockTail,
                     Area_SqFt,FloorId,BaseRatePerDay,BaseRatePerHalfDay,
                     GSTPercent,CGSTPercent,SGSTPercent,IGSTPercent,SACCode,GstSlabId,
                     IsAC,HasStage,HasProjector,HasSoundSystem,HasParking,HasCatering,HasWifi,
                     Description,PhotoPath,BranchID,IsActive,CreatedBy)
                VALUES
                    (@VenueCode,@VenueName,@VenueType,@CapacitySeated,@CapacityBuffet,@CapacityTheater,@CapacityCockTail,
                     @Area_SqFt,@FloorId,@BaseRatePerDay,@BaseRatePerHalfDay,
                     @GSTPercent,@CGSTPercent,@SGSTPercent,@IGSTPercent,@SACCode,@GstSlabId,
                     @IsAC,@HasStage,@HasProjector,@HasSoundSystem,@HasParking,@HasCatering,@HasWifi,
                     @Description,@PhotoPath,@BranchID,@IsActive,@CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await _db.ExecuteScalarAsync<int>(sql, v);
        }

        public async Task<bool> UpdateAsync(BanquetVenue v)
        {
            var sql = @"
                UPDATE BanquetVenues SET
                    VenueName=@VenueName,VenueType=@VenueType,
                    CapacitySeated=@CapacitySeated,CapacityBuffet=@CapacityBuffet,
                    CapacityTheater=@CapacityTheater,CapacityCockTail=@CapacityCockTail,
                    Area_SqFt=@Area_SqFt,FloorId=@FloorId,
                    BaseRatePerDay=@BaseRatePerDay,BaseRatePerHalfDay=@BaseRatePerHalfDay,
                    GSTPercent=@GSTPercent,CGSTPercent=@CGSTPercent,SGSTPercent=@SGSTPercent,IGSTPercent=@IGSTPercent,
                    SACCode=@SACCode,GstSlabId=@GstSlabId,
                    IsAC=@IsAC,HasStage=@HasStage,HasProjector=@HasProjector,HasSoundSystem=@HasSoundSystem,
                    HasParking=@HasParking,HasCatering=@HasCatering,HasWifi=@HasWifi,
                    Description=@Description,IsActive=@IsActive,
                    UpdatedDate=SYSUTCDATETIME(),UpdatedBy=@UpdatedBy
                WHERE Id=@Id";
            return await _db.ExecuteAsync(sql, v) > 0;
        }

        public async Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null)
        {
            var sql = "SELECT COUNT(1) FROM BanquetVenues WHERE VenueCode=@Code AND BranchID=@BranchID AND (@ExcludeId IS NULL OR Id<>@ExcludeId)";
            return await _db.ExecuteScalarAsync<int>(sql, new { Code = code, BranchID = branchId, ExcludeId = excludeId }) > 0;
        }

        public async Task<bool> IsVenueAvailableAsync(int venueId, DateOnly eventDate, TimeOnly? startTime, TimeOnly? endTime, int? excludeBookingId = null)
        {
            // Only active/in-progress bookings block a venue slot.
            // Cancelled, NoShow, and CheckedOut bookings release the slot.
            var sql = @"
                SELECT COUNT(1) FROM BanquetBookings
                WHERE VenueId = @VenueId
                  AND EventDate = @EventDate
                  AND [Status] NOT IN ('Cancelled','NoShow','CheckedOut')
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                  AND (
                        @StartTime IS NULL OR @EndTime IS NULL
                        OR (
                            (EventStartTime IS NULL OR EventStartTime < @EndTime)
                            AND (EventEndTime IS NULL OR EventEndTime > @StartTime)
                        )
                  )";
            var count = await _db.ExecuteScalarAsync<int>(sql, new
            {
                VenueId = venueId,
                EventDate = eventDate,
                StartTime = startTime,
                EndTime = endTime,
                ExcludeId = excludeBookingId
            });
            return count == 0;
        }
    }

    // ── BanquetEventTypeRepository ────────────────────────────────────────────

    public class BanquetEventTypeRepository : IBanquetEventTypeRepository
    {
        private readonly IDbConnection _db;
        public BanquetEventTypeRepository(IDbConnection db) => _db = db;

        public async Task<IEnumerable<BanquetEventType>> GetByBranchAsync(int branchId, bool activeOnly = true)
        {
            var sql = $"SELECT * FROM BanquetEventTypes WHERE BranchID=@BranchID {(activeOnly ? "AND IsActive=1" : "")} ORDER BY EventTypeName";
            return await _db.QueryAsync<BanquetEventType>(sql, new { BranchID = branchId });
        }

        public async Task<BanquetEventType?> GetByIdAsync(int id) =>
            await _db.QueryFirstOrDefaultAsync<BanquetEventType>("SELECT * FROM BanquetEventTypes WHERE Id=@Id", new { Id = id });

        public async Task<int> CreateAsync(BanquetEventType e)
        {
            var sql = @"INSERT INTO BanquetEventTypes(EventTypeCode,EventTypeName,Description,IconClass,BranchID,IsActive,CreatedBy)
                        VALUES(@EventTypeCode,@EventTypeName,@Description,@IconClass,@BranchID,@IsActive,@CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await _db.ExecuteScalarAsync<int>(sql, e);
        }

        public async Task<bool> UpdateAsync(BanquetEventType e)
        {
            var sql = @"UPDATE BanquetEventTypes SET EventTypeName=@EventTypeName,Description=@Description,IconClass=@IconClass,
                        IsActive=@IsActive,UpdatedDate=SYSUTCDATETIME(),UpdatedBy=@UpdatedBy WHERE Id=@Id";
            return await _db.ExecuteAsync(sql, e) > 0;
        }

        public async Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null)
        {
            var sql = "SELECT COUNT(1) FROM BanquetEventTypes WHERE EventTypeCode=@Code AND BranchID=@BranchID AND (@ExcludeId IS NULL OR Id<>@ExcludeId)";
            return await _db.ExecuteScalarAsync<int>(sql, new { Code = code, BranchID = branchId, ExcludeId = excludeId }) > 0;
        }
    }

    // ── BanquetPackageRepository ──────────────────────────────────────────────

    public class BanquetPackageRepository : IBanquetPackageRepository
    {
        private readonly IDbConnection _db;
        public BanquetPackageRepository(IDbConnection db) => _db = db;

        public async Task<IEnumerable<BanquetPackage>> GetByBranchAsync(int branchId, bool activeOnly = true)
        {
            var sql = $"SELECT * FROM BanquetPackages WHERE BranchID=@BranchID {(activeOnly ? "AND IsActive=1" : "")} ORDER BY PackageName";
            return await _db.QueryAsync<BanquetPackage>(sql, new { BranchID = branchId });
        }

        public async Task<BanquetPackage?> GetByIdAsync(int id) =>
            await _db.QueryFirstOrDefaultAsync<BanquetPackage>("SELECT * FROM BanquetPackages WHERE Id=@Id", new { Id = id });

        public async Task<int> CreateAsync(BanquetPackage p)
        {
            var sql = @"INSERT INTO BanquetPackages
                        (PackageCode,PackageName,PackageType,PricePerPax,MinimumGuaranteePax,
                         GSTPercent,CGSTPercent,SGSTPercent,IGSTPercent,SACCode,GstSlabId,
                         IncludesStarter,IncludesMainCourse,IncludesDessert,IncludesBeverages,IncludesLive,
                         MenuDescription,BranchID,IsActive,CreatedBy)
                        VALUES
                        (@PackageCode,@PackageName,@PackageType,@PricePerPax,@MinimumGuaranteePax,
                         @GSTPercent,@CGSTPercent,@SGSTPercent,@IGSTPercent,@SACCode,@GstSlabId,
                         @IncludesStarter,@IncludesMainCourse,@IncludesDessert,@IncludesBeverages,@IncludesLive,
                         @MenuDescription,@BranchID,@IsActive,@CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await _db.ExecuteScalarAsync<int>(sql, p);
        }

        public async Task<bool> UpdateAsync(BanquetPackage p)
        {
            var sql = @"UPDATE BanquetPackages SET
                        PackageName=@PackageName,PackageType=@PackageType,PricePerPax=@PricePerPax,
                        MinimumGuaranteePax=@MinimumGuaranteePax,
                        GSTPercent=@GSTPercent,CGSTPercent=@CGSTPercent,SGSTPercent=@SGSTPercent,IGSTPercent=@IGSTPercent,
                        SACCode=@SACCode,GstSlabId=@GstSlabId,
                        IncludesStarter=@IncludesStarter,IncludesMainCourse=@IncludesMainCourse,
                        IncludesDessert=@IncludesDessert,IncludesBeverages=@IncludesBeverages,IncludesLive=@IncludesLive,
                        MenuDescription=@MenuDescription,IsActive=@IsActive,
                        UpdatedDate=SYSUTCDATETIME(),UpdatedBy=@UpdatedBy
                        WHERE Id=@Id";
            return await _db.ExecuteAsync(sql, p) > 0;
        }

        public async Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null)
        {
            var sql = "SELECT COUNT(1) FROM BanquetPackages WHERE PackageCode=@Code AND BranchID=@BranchID AND (@ExcludeId IS NULL OR Id<>@ExcludeId)";
            return await _db.ExecuteScalarAsync<int>(sql, new { Code = code, BranchID = branchId, ExcludeId = excludeId }) > 0;
        }
    }

    // ── BanquetAddonServiceRepository ────────────────────────────────────────

    public class BanquetAddonServiceRepository : IBanquetAddonServiceRepository
    {
        private readonly IDbConnection _db;
        public BanquetAddonServiceRepository(IDbConnection db) => _db = db;

        public async Task<IEnumerable<BanquetAddonService>> GetByBranchAsync(int branchId, bool activeOnly = true)
        {
            var sql = $"SELECT * FROM BanquetAddonServices WHERE BranchID=@BranchID {(activeOnly ? "AND IsActive=1" : "")} ORDER BY ServiceName";
            return await _db.QueryAsync<BanquetAddonService>(sql, new { BranchID = branchId });
        }

        public async Task<BanquetAddonService?> GetByIdAsync(int id) =>
            await _db.QueryFirstOrDefaultAsync<BanquetAddonService>("SELECT * FROM BanquetAddonServices WHERE Id=@Id", new { Id = id });

        public async Task<int> CreateAsync(BanquetAddonService s)
        {
            var sql = @"INSERT INTO BanquetAddonServices
                        (ServiceCode,ServiceName,ServiceType,Rate,RateType,
                         GSTPercent,CGSTPercent,SGSTPercent,IGSTPercent,SACCode,GstSlabId,Description,BranchID,IsActive,CreatedBy)
                        VALUES
                        (@ServiceCode,@ServiceName,@ServiceType,@Rate,@RateType,
                         @GSTPercent,@CGSTPercent,@SGSTPercent,@IGSTPercent,@SACCode,@GstSlabId,@Description,@BranchID,@IsActive,@CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await _db.ExecuteScalarAsync<int>(sql, s);
        }

        public async Task<bool> UpdateAsync(BanquetAddonService s)
        {
            var sql = @"UPDATE BanquetAddonServices SET
                        ServiceName=@ServiceName,ServiceType=@ServiceType,Rate=@Rate,RateType=@RateType,
                        GSTPercent=@GSTPercent,CGSTPercent=@CGSTPercent,SGSTPercent=@SGSTPercent,IGSTPercent=@IGSTPercent,
                        SACCode=@SACCode,GstSlabId=@GstSlabId,Description=@Description,IsActive=@IsActive,
                        UpdatedDate=SYSUTCDATETIME(),UpdatedBy=@UpdatedBy
                        WHERE Id=@Id";
            return await _db.ExecuteAsync(sql, s) > 0;
        }

        public async Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null)
        {
            var sql = "SELECT COUNT(1) FROM BanquetAddonServices WHERE ServiceCode=@Code AND BranchID=@BranchID AND (@ExcludeId IS NULL OR Id<>@ExcludeId)";
            return await _db.ExecuteScalarAsync<int>(sql, new { Code = code, BranchID = branchId, ExcludeId = excludeId }) > 0;
        }
    }

    // ── BanquetBookingRepository ──────────────────────────────────────────────

    public class BanquetBookingRepository : IBanquetBookingRepository
    {
        private readonly IDbConnection _db;
        public BanquetBookingRepository(IDbConnection db) => _db = db;

        private const string SelectBase = @"
            SELECT bb.*,
                   bv.VenueName, bv.VenueType,
                   et.EventTypeName,
                   pkg.PackageName,
                   u.FirstName + ' ' + u.LastName AS CreatedByName
            FROM BanquetBookings bb
            INNER JOIN BanquetVenues bv      ON bv.Id = bb.VenueId
            INNER JOIN BanquetEventTypes et  ON et.Id = bb.EventTypeId
            LEFT  JOIN BanquetPackages pkg   ON pkg.Id = bb.PackageId
            LEFT  JOIN Users u               ON u.Id  = bb.CreatedBy";

        public async Task<IEnumerable<BanquetBooking>> GetListAsync(int branchId, string? status, DateOnly? fromDate, DateOnly? toDate, int? venueId, string? customerType, int? b2bClientId)
        {
            var where = "WHERE bb.BranchID = @BranchID";
            if (!string.IsNullOrWhiteSpace(status)) where += " AND bb.[Status] = @Status";
            if (fromDate.HasValue) where += " AND bb.EventDate >= @FromDate";
            if (toDate.HasValue)   where += " AND bb.EventDate <= @ToDate";
            if (venueId.HasValue)  where += " AND bb.VenueId = @VenueId";
            if (!string.IsNullOrWhiteSpace(customerType)) where += " AND bb.CustomerType = @CustomerType";
            if (b2bClientId.HasValue) where += " AND bb.B2BClientId = @B2BClientId";

            var sql = $"{SelectBase} {where} ORDER BY bb.EventDate DESC, bb.Id DESC";
            return await _db.QueryAsync<BanquetBooking>(sql, new { BranchID = branchId, Status = status, FromDate = fromDate, ToDate = toDate, VenueId = venueId, CustomerType = customerType, B2BClientId = b2bClientId });
        }

        public async Task<BanquetBooking?> GetByIdAsync(int id)
        {
            var booking = await _db.QueryFirstOrDefaultAsync<BanquetBooking>($"{SelectBase} WHERE bb.Id=@Id", new { Id = id });
            if (booking == null) return null;
            await PopulateChildrenAsync(booking);
            return booking;
        }

        public async Task<BanquetBooking?> GetByNumberAsync(string number)
        {
            var booking = await _db.QueryFirstOrDefaultAsync<BanquetBooking>($"{SelectBase} WHERE bb.BanquetBookingNumber=@Number", new { Number = number });
            if (booking == null) return null;
            await PopulateChildrenAsync(booking);
            return booking;
        }

        private async Task PopulateChildrenAsync(BanquetBooking b)
        {
            b.PackageLines = (await _db.QueryAsync<BanquetBookingPackageLine>("SELECT * FROM BanquetBookingPackageLines WHERE BanquetBookingId=@Id ORDER BY Id", new { Id = b.Id })).ToList();
            b.AddonLines   = (await _db.QueryAsync<BanquetBookingAddonLine>("SELECT * FROM BanquetBookingAddonLines WHERE BanquetBookingId=@Id ORDER BY Id", new { Id = b.Id })).ToList();
            b.Payments     = (await GetPaymentsAsync(b.Id)).ToList();
            b.AuditLogs    = (await GetAuditLogsAsync(b.Id)).ToList();
        }

        public async Task<int> CreateAsync(BanquetBooking b, List<BanquetBookingPackageLine> packageLines, List<BanquetBookingAddonLine> addonLines, BanquetBookingPayment? advance)
        {
            if (_db.State != ConnectionState.Open) _db.Open();
            using var tx = _db.BeginTransaction();
            try
            {
                var insertSql = @"
                    INSERT INTO BanquetBookings
                    (BanquetBookingNumber,BranchID,EventDate,EventEndDate,EventStartTime,EventEndTime,SetupTime,TeardownTime,
                     VenueId,EventTypeId,EventName,AttendeeCount,GuaranteePax,ChildCount,MealType,
                     CustomerType,PrimaryGuestId,GuestName,GuestPhone,GuestEmail,GuestAddress,GuestGSTIN,
                     B2BClientId,B2BAgreementId,CompanyName,CompanyGSTIN,CompanyPAN,CompanyAddress,BillingTo,CreditDays,IsInterState,
                     VenueHireType,PackageId,PackagePricePerPax,PackageTotalPax,
                     PackageBaseAmount,PackageGSTAmount,PackageCGSTAmount,PackageSGSTAmount,PackageIGSTAmount,
                     VenueBaseAmount,VenueGSTAmount,VenueCGSTAmount,VenueSGSTAmount,VenueIGSTAmount,
                     AddonBaseAmount,AddonGSTAmount,AddonCGSTAmount,AddonSGSTAmount,AddonIGSTAmount,
                     TotalBaseAmount,TotalGSTAmount,TotalCGSTAmount,TotalSGSTAmount,TotalIGSTAmount,
                     ServiceChargeAmount,DiscountAmount,RoundOffAmount,TotalAmount,DepositAmount,BalanceAmount,
                     [Status],PaymentStatus,ApprovalStatus,
                     CancellationPolicyId,SpecialRequests,InternalNotes,CreatedBy)
                    VALUES
                    (@BanquetBookingNumber,@BranchID,@EventDate,@EventEndDate,@EventStartTime,@EventEndTime,@SetupTime,@TeardownTime,
                     @VenueId,@EventTypeId,@EventName,@AttendeeCount,@GuaranteePax,@ChildCount,@MealType,
                     @CustomerType,@PrimaryGuestId,@GuestName,@GuestPhone,@GuestEmail,@GuestAddress,@GuestGSTIN,
                     @B2BClientId,@B2BAgreementId,@CompanyName,@CompanyGSTIN,@CompanyPAN,@CompanyAddress,@BillingTo,@CreditDays,@IsInterState,
                     @VenueHireType,@PackageId,@PackagePricePerPax,@PackageTotalPax,
                     @PackageBaseAmount,@PackageGSTAmount,@PackageCGSTAmount,@PackageSGSTAmount,@PackageIGSTAmount,
                     @VenueBaseAmount,@VenueGSTAmount,@VenueCGSTAmount,@VenueSGSTAmount,@VenueIGSTAmount,
                     @AddonBaseAmount,@AddonGSTAmount,@AddonCGSTAmount,@AddonSGSTAmount,@AddonIGSTAmount,
                     @TotalBaseAmount,@TotalGSTAmount,@TotalCGSTAmount,@TotalSGSTAmount,@TotalIGSTAmount,
                     @ServiceChargeAmount,@DiscountAmount,@RoundOffAmount,@TotalAmount,@DepositAmount,@BalanceAmount,
                     @Status,@PaymentStatus,@ApprovalStatus,
                     @CancellationPolicyId,@SpecialRequests,@InternalNotes,@CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var bookingId = await _db.ExecuteScalarAsync<int>(insertSql, b, tx);

                foreach (var pl in packageLines)
                {
                    pl.BanquetBookingId = bookingId;
                    await _db.ExecuteAsync(@"
                        INSERT INTO BanquetBookingPackageLines
                        (BanquetBookingId,PackageId,PackageName,PackageType,MealType,PricePerPax,Pax,BaseAmount,
                         GSTPercent,CGSTPercent,SGSTPercent,IGSTPercent,GSTAmount,CGSTAmount,SGSTAmount,IGSTAmount,TotalAmount,MenuDescription,SACCode)
                        VALUES(@BanquetBookingId,@PackageId,@PackageName,@PackageType,@MealType,@PricePerPax,@Pax,@BaseAmount,
                               @GSTPercent,@CGSTPercent,@SGSTPercent,@IGSTPercent,@GSTAmount,@CGSTAmount,@SGSTAmount,@IGSTAmount,@TotalAmount,@MenuDescription,@SACCode)", pl, tx);
                }

                foreach (var al in addonLines)
                {
                    al.BanquetBookingId = bookingId;
                    await _db.ExecuteAsync(@"
                        INSERT INTO BanquetBookingAddonLines
                        (BanquetBookingId,AddonServiceId,ServiceName,ServiceType,Rate,RateType,Qty,BaseAmount,
                         GSTPercent,CGSTPercent,SGSTPercent,IGSTPercent,GSTAmount,CGSTAmount,SGSTAmount,IGSTAmount,TotalAmount,Notes,SACCode)
                        VALUES(@BanquetBookingId,@AddonServiceId,@ServiceName,@ServiceType,@Rate,@RateType,@Qty,@BaseAmount,
                               @GSTPercent,@CGSTPercent,@SGSTPercent,@IGSTPercent,@GSTAmount,@CGSTAmount,@SGSTAmount,@IGSTAmount,@TotalAmount,@Notes,@SACCode)", al, tx);
                }

                if (advance != null && advance.Amount > 0)
                {
                    advance.BanquetBookingId = bookingId;
                    advance.IsAdvancePayment = true;
                    await _db.ExecuteAsync(@"
                        INSERT INTO BanquetBookingPayments
                        (BanquetBookingId,ReceiptNumber,Amount,PaymentMethod,PaymentReference,[Status],PaidOn,BankId,IsAdvancePayment,IsRefund,DiscountAmount,RoundOffAmount,Remarks,CreatedBy)
                        VALUES(@BanquetBookingId,@ReceiptNumber,@Amount,@PaymentMethod,@PaymentReference,@Status,SYSUTCDATETIME(),@BankId,1,0,@DiscountAmount,@RoundOffAmount,@Remarks,@CreatedBy)", advance, tx);
                }

                // Audit: Created
                await _db.ExecuteAsync(@"
                    INSERT INTO BanquetBookingAuditLog(BanquetBookingId,BanquetBookingNumber,ActionType,ActionDescription,PerformedBy)
                    VALUES(@Id,@Number,'Created','Banquet booking created',@By)",
                    new { Id = bookingId, Number = b.BanquetBookingNumber, By = b.CreatedBy }, tx);

                tx.Commit();
                return bookingId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, string status, int updatedBy)
        {
            var sql = "UPDATE BanquetBookings SET [Status]=@Status,LastModifiedDate=SYSUTCDATETIME(),LastModifiedBy=@By WHERE Id=@Id";
            return await _db.ExecuteAsync(sql, new { Status = status, By = updatedBy, Id = id }) > 0;
        }

        public async Task<bool> UpdateFinancialsAsync(int id) => await RecalculateBalanceAsync(id);

        public async Task<bool> RecalculateBalanceAsync(int id)
        {
            var sql = @"
                UPDATE bb SET
                    DepositAmount = ISNULL(p.TotalPaid, 0),
                    BalanceAmount = bb.TotalAmount - ISNULL(p.TotalPaid, 0),
                    PaymentStatus = CASE
                        WHEN ISNULL(p.TotalPaid,0) = 0 THEN 'Pending'
                        WHEN ISNULL(p.TotalPaid,0) >= bb.TotalAmount THEN 'FullPaid'
                        ELSE 'PartialPaid'
                    END
                FROM BanquetBookings bb
                LEFT JOIN (
                    SELECT BanquetBookingId,
                           SUM(CASE WHEN IsRefund=0 THEN Amount + DiscountAmount ELSE -Amount END) AS TotalPaid
                    FROM BanquetBookingPayments
                    WHERE [Status] IN ('Captured','Success')
                    GROUP BY BanquetBookingId
                ) p ON p.BanquetBookingId = bb.Id
                WHERE bb.Id = @Id";
            return await _db.ExecuteAsync(sql, new { Id = id }) > 0;
        }

        public async Task AddAuditLogAsync(BanquetBookingAuditLog log)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO BanquetBookingAuditLog(BanquetBookingId,BanquetBookingNumber,ActionType,ActionDescription,OldValue,NewValue,PerformedBy)
                VALUES(@BanquetBookingId,@BanquetBookingNumber,@ActionType,@ActionDescription,@OldValue,@NewValue,@PerformedBy)", log);
        }

        public async Task<IEnumerable<BanquetBookingPayment>> GetPaymentsAsync(int bookingId)
        {
            var sql = @"SELECT p.*, b.BankName, u.FirstName+' '+u.LastName AS CreatedByName
                        FROM BanquetBookingPayments p
                        LEFT JOIN Banks b ON b.Id = p.BankId
                        LEFT JOIN Users u ON u.Id = p.CreatedBy
                        WHERE p.BanquetBookingId=@Id ORDER BY p.PaidOn";
            return await _db.QueryAsync<BanquetBookingPayment>(sql, new { Id = bookingId });
        }

        public async Task<BanquetBookingPayment?> GetPaymentByIdAsync(int paymentId) =>
            await _db.QueryFirstOrDefaultAsync<BanquetBookingPayment>("SELECT * FROM BanquetBookingPayments WHERE Id=@Id", new { Id = paymentId });

        public async Task<int> AddPaymentAsync(BanquetBookingPayment p)
        {
            if (_db.State != ConnectionState.Open) _db.Open();
            using var tx = _db.BeginTransaction();
            try
            {
                var id = await _db.ExecuteScalarAsync<int>(@"
                    INSERT INTO BanquetBookingPayments
                    (BanquetBookingId,ReceiptNumber,Amount,PaymentMethod,PaymentReference,[Status],PaidOn,
                     BankId,CardType,CardLastFourDigits,ChequeDate,IsAdvancePayment,IsRefund,DiscountAmount,RoundOffAmount,Remarks,CreatedBy)
                    VALUES(@BanquetBookingId,@ReceiptNumber,@Amount,@PaymentMethod,@PaymentReference,@Status,SYSUTCDATETIME(),
                           @BankId,@CardType,@CardLastFourDigits,@ChequeDate,@IsAdvancePayment,@IsRefund,@DiscountAmount,@RoundOffAmount,@Remarks,@CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", p, tx);

                // Recalculate balance
                await _db.ExecuteAsync(@"
                    UPDATE bb SET
                        DepositAmount = ISNULL(paid.TotalPaid,0),
                        BalanceAmount = bb.TotalAmount - ISNULL(paid.TotalPaid,0),
                        PaymentStatus = CASE
                            WHEN ISNULL(paid.TotalPaid,0)=0 THEN 'Pending'
                            WHEN ISNULL(paid.TotalPaid,0)>=bb.TotalAmount THEN 'FullPaid'
                            ELSE 'PartialPaid'
                        END,
                        LastModifiedDate = SYSUTCDATETIME()
                    FROM BanquetBookings bb
                    LEFT JOIN (
                        SELECT BanquetBookingId,
                               SUM(CASE WHEN IsRefund=0 THEN Amount + DiscountAmount ELSE -Amount END) AS TotalPaid
                        FROM BanquetBookingPayments WHERE [Status] IN ('Captured','Success') GROUP BY BanquetBookingId
                    ) paid ON paid.BanquetBookingId = bb.Id
                    WHERE bb.Id = @BId", new { BId = p.BanquetBookingId }, tx);

                // Audit
                var booking = await _db.QueryFirstOrDefaultAsync<BanquetBooking>("SELECT BanquetBookingNumber FROM BanquetBookings WHERE Id=@Id", new { Id = p.BanquetBookingId }, tx);
                await _db.ExecuteAsync(@"
                    INSERT INTO BanquetBookingAuditLog(BanquetBookingId,BanquetBookingNumber,ActionType,ActionDescription,NewValue,PerformedBy)
                    VALUES(@BId,@Num,'PaymentReceived',@Desc,@Amount,@By)",
                    new { BId = p.BanquetBookingId, Num = booking?.BanquetBookingNumber ?? "", Desc = $"Payment of ₹{p.Amount:0.00} via {p.PaymentMethod}", Amount = p.Amount.ToString("0.00"), By = p.CreatedBy }, tx);

                tx.Commit();
                return id;
            }
            catch { tx.Rollback(); throw; }
        }

        public async Task<IEnumerable<BanquetBookingAuditLog>> GetAuditLogsAsync(int bookingId)
        {
            var sql = @"SELECT l.*, u.FirstName+' '+u.LastName AS PerformedByName
                        FROM BanquetBookingAuditLog l
                        LEFT JOIN Users u ON u.Id = l.PerformedBy
                        WHERE l.BanquetBookingId=@Id ORDER BY l.PerformedAt";
            return await _db.QueryAsync<BanquetBookingAuditLog>(sql, new { Id = bookingId });
        }

        public async Task<string> GenerateNextReceiptNumberAsync(int branchId)
        {
            var sql = @"
                UPDATE BanquetReceiptCounter SET LastNumber = LastNumber + 1
                OUTPUT INSERTED.LastNumber
                WHERE BranchID = @BranchID;
                IF @@ROWCOUNT = 0 BEGIN
                    INSERT INTO BanquetReceiptCounter(BranchID, LastNumber) VALUES(@BranchID, 1);
                    SELECT 1;
                END";
            var num = await _db.ExecuteScalarAsync<int>(sql, new { BranchID = branchId });
            return $"BNQ-RCP-{DateTime.UtcNow:yyyyMM}-{num:D5}";
        }

        public async Task<IEnumerable<BanquetBooking>> GetCalendarEventsAsync(int branchId, DateOnly fromDate, DateOnly toDate, int? venueId = null)
        {
            var sql = @"SELECT bb.Id,bb.BanquetBookingNumber,bb.EventDate,bb.EventEndDate,bb.EventStartTime,bb.EventEndTime,
                               bb.EventName,bb.AttendeeCount,bb.GuaranteePax,bb.MealType,bb.VenueHireType,bb.[Status],bb.CustomerType,
                               bb.GuestName,bb.GuestPhone,bb.TotalAmount,bb.BalanceAmount,bb.DepositAmount,bb.VenueId,
                               bv.VenueName, et.EventTypeName
                        FROM BanquetBookings bb
                        INNER JOIN BanquetVenues bv ON bv.Id=bb.VenueId
                        INNER JOIN BanquetEventTypes et ON et.Id=bb.EventTypeId
                        WHERE bb.BranchID=@BranchID
                          AND bb.EventDate BETWEEN @FromDate AND @ToDate
                          AND bb.[Status] NOT IN ('Cancelled')
                          AND (@VenueId IS NULL OR bb.VenueId=@VenueId)
                        ORDER BY bb.EventDate, bb.EventStartTime";
            return await _db.QueryAsync<BanquetBooking>(sql, new { BranchID = branchId, FromDate = fromDate, ToDate = toDate, VenueId = venueId });
        }

        public async Task<BanquetDashboardKpi> GetDashboardKpiAsync(int branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

            var kpi = new BanquetDashboardKpi
            {
                TodaysEvents = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM BanquetBookings WHERE BranchID=@B AND EventDate=@Today AND [Status] NOT IN ('Cancelled','NoShow')",
                    new { B = branchId, Today = today }),
                UpcomingEvents7Days = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM BanquetBookings WHERE BranchID=@B AND EventDate BETWEEN @Today AND @End AND [Status] NOT IN ('Cancelled','NoShow')",
                    new { B = branchId, Today = today, End = today.AddDays(7) }),
                PendingConfirmations = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM BanquetBookings WHERE BranchID=@B AND [Status] IN ('Inquiry','Tentative')",
                    new { B = branchId }),
                ThisMonthRevenue = await _db.ExecuteScalarAsync<decimal>(
                    "SELECT ISNULL(SUM(Amount),0) FROM BanquetBookingPayments p INNER JOIN BanquetBookings b ON b.Id=p.BanquetBookingId WHERE b.BranchID=@B AND p.IsRefund=0 AND p.[Status] IN ('Captured','Success') AND CAST(p.PaidOn AS DATE) BETWEEN @Start AND @End",
                    new { B = branchId, Start = monthStart, End = monthEnd }),
                OutstandingBalance = await _db.ExecuteScalarAsync<decimal>(
                    "SELECT ISNULL(SUM(BalanceAmount),0) FROM BanquetBookings WHERE BranchID=@B AND BalanceAmount>0 AND [Status] NOT IN ('Cancelled')",
                    new { B = branchId }),
                ThisMonthBookings = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM BanquetBookings WHERE BranchID=@B AND [Status] NOT IN ('Cancelled') AND EventDate BETWEEN @Start AND @End",
                    new { B = branchId, Start = monthStart, End = monthEnd })
            };

            kpi.TodaysEventList = (await _db.QueryAsync<BanquetBooking>($@"
                {SelectBase}
                WHERE bb.BranchID=@B AND bb.EventDate=@Today AND bb.[Status] NOT IN ('Cancelled','NoShow')
                ORDER BY bb.EventStartTime", new { B = branchId, Today = today })).ToList();

            kpi.RecentBookings = (await _db.QueryAsync<BanquetBooking>($@"
                {SelectBase}
                WHERE bb.BranchID=@B ORDER BY bb.Id DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", new { B = branchId })).ToList();

            return kpi;
        }

        public async Task<BanquetBooking?> GetLastB2CGuestByPhoneAsync(string phone, int branchId)
        {
            var sql = @"SELECT TOP 1 GuestName, GuestEmail, GuestAddress, GuestGSTIN
                        FROM BanquetBookings
                        WHERE GuestPhone=@Phone AND BranchID=@BranchID AND CustomerType='B2C'
                        ORDER BY Id DESC";
            return await _db.QueryFirstOrDefaultAsync<BanquetBooking>(sql, new { Phone = phone, BranchID = branchId });
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var now = DateTime.Today;
            int fyStart = now.Month >= 4 ? now.Year : now.Year - 1;
            int fyEnd = fyStart + 1;
            var fy = $"{fyStart}-{fyEnd % 100:D2}";

            const string upsertSql = @"
                DECLARE @NextSeq INT;
                DECLARE @MaxBookingSeq INT = ISNULL((
                    SELECT MAX(CAST(RIGHT(InvoiceNumber, 5) AS INT))
                    FROM (
                        SELECT InvoiceNumber FROM Bookings       WHERE InvoiceNumber LIKE 'INV/' + @FinancialYear + '/%'
                        UNION ALL
                        SELECT InvoiceNumber FROM BanquetBookings WHERE InvoiceNumber LIKE 'INV/' + @FinancialYear + '/%'
                    ) AS AllInvoices
                ), 0);

                IF NOT EXISTS (SELECT 1 FROM InvoiceSequence WHERE FinancialYear = @FinancialYear AND BranchID = 0)
                BEGIN
                    DECLARE @MaxBranchSeq INT = ISNULL((SELECT MAX(LastSequence) FROM InvoiceSequence WHERE FinancialYear = @FinancialYear), 0);
                    DECLARE @SeedVal INT = CASE WHEN @MaxBranchSeq > @MaxBookingSeq THEN @MaxBranchSeq ELSE @MaxBookingSeq END;
                    INSERT INTO InvoiceSequence (FinancialYear, BranchID, LastSequence) VALUES (@FinancialYear, 0, @SeedVal + 1);
                    SET @NextSeq = @SeedVal + 1;
                END
                ELSE
                BEGIN
                    UPDATE InvoiceSequence WITH (HOLDLOCK)
                    SET LastSequence = CASE
                        WHEN LastSequence <= @MaxBookingSeq THEN @MaxBookingSeq + 1
                        ELSE LastSequence + 1
                    END
                    WHERE FinancialYear = @FinancialYear AND BranchID = 0;

                    SELECT @NextSeq = LastSequence FROM InvoiceSequence WHERE FinancialYear = @FinancialYear AND BranchID = 0;
                END

                SELECT @NextSeq;";

            var seq = await _db.ExecuteScalarAsync<int>(upsertSql, new { FinancialYear = fy });
            return $"INV/{fy}/{seq:D5}";
        }

        public async Task SetInvoiceNumberAsync(int id, string invoiceNumber)
        {
            const string sql = @"UPDATE BanquetBookings
                                 SET InvoiceNumber = @InvoiceNumber
                                 WHERE Id = @Id AND (InvoiceNumber IS NULL OR InvoiceNumber = '')";
            await _db.ExecuteAsync(sql, new { Id = id, InvoiceNumber = invoiceNumber });
        }
    }

    // ── BanquetCancellationRepository ─────────────────────────────────────────

    public class BanquetCancellationRepository : IBanquetCancellationRepository
    {
        private readonly IDbConnection _db;
        public BanquetCancellationRepository(IDbConnection db) => _db = db;

        public async Task<BanquetCancellationPreview> GetPreviewAsync(int bookingId)
        {
            var booking = await _db.QueryFirstOrDefaultAsync<BanquetBooking>("SELECT * FROM BanquetBookings WHERE Id=@Id", new { Id = bookingId });
            if (booking == null) throw new InvalidOperationException("Booking not found");

            var amountPaid = await _db.ExecuteScalarAsync<decimal>(@"
                SELECT ISNULL(SUM(CASE WHEN IsRefund=0 THEN Amount ELSE -Amount END),0)
                FROM BanquetBookingPayments WHERE BanquetBookingId=@Id AND [Status] IN ('Captured','Success')", new { Id = bookingId });

            var daysBeforeEvent = (booking.EventDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

            // Try to apply cancellation policy if set
            decimal refundPercent = 100, flatDeduction = 0;
            string policyName = "No Policy Applied";

            if (booking.CancellationPolicyId.HasValue)
            {
                // Use existing cancellation policy resolution via stored fields (snapshot)
                var snapshot = booking.CancellationPolicySnapshot;
                if (!string.IsNullOrWhiteSpace(snapshot))
                    policyName = "Policy Applied";
            }

            var deduction = amountPaid * (1 - refundPercent / 100m) + flatDeduction;
            var refundAmount = Math.Max(amountPaid - deduction - flatDeduction, 0);

            return new BanquetCancellationPreview
            {
                BanquetBookingId = bookingId,
                BanquetBookingNumber = booking.BanquetBookingNumber,
                AmountPaid = amountPaid,
                RefundPercent = refundPercent,
                FlatDeduction = flatDeduction,
                DeductionAmount = deduction,
                RefundAmount = refundAmount,
                PolicyName = policyName,
                DaysBeforeEvent = daysBeforeEvent
            };
        }

        public async Task<BanquetCancellation> CancelAsync(int bookingId, string reason, decimal flatDeduction, int cancelledBy)
        {
            var preview = await GetPreviewAsync(bookingId);
            var booking = await _db.QueryFirstOrDefaultAsync<BanquetBooking>("SELECT BanquetBookingNumber FROM BanquetBookings WHERE Id=@Id", new { Id = bookingId });

            if (_db.State != ConnectionState.Open) _db.Open();
            using var tx = _db.BeginTransaction();
            try
            {
                var cancellation = new BanquetCancellation
                {
                    BanquetBookingId = bookingId,
                    BanquetBookingNumber = preview.BanquetBookingNumber,
                    AmountPaid = preview.AmountPaid,
                    RefundPercent = preview.RefundPercent,
                    FlatDeduction = flatDeduction,
                    DeductionAmount = preview.DeductionAmount,
                    RefundAmount = preview.RefundAmount,
                    IsRefunded = false,
                    ApprovalStatus = "Pending",
                    CancellationReason = reason,
                    CancelledBy = cancelledBy
                };

                var id = await _db.ExecuteScalarAsync<int>(@"
                    INSERT INTO BanquetCancellations(BanquetBookingId,BanquetBookingNumber,AmountPaid,RefundPercent,FlatDeduction,
                        DeductionAmount,RefundAmount,IsRefunded,ApprovalStatus,CancellationReason,CancelledBy)
                    VALUES(@BanquetBookingId,@BanquetBookingNumber,@AmountPaid,@RefundPercent,@FlatDeduction,
                           @DeductionAmount,@RefundAmount,0,@ApprovalStatus,@CancellationReason,@CancelledBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", cancellation, tx);

                await _db.ExecuteAsync("UPDATE BanquetBookings SET [Status]='Cancelled',LastModifiedDate=SYSUTCDATETIME(),LastModifiedBy=@By WHERE Id=@Id",
                    new { By = cancelledBy, Id = bookingId }, tx);

                await _db.ExecuteAsync(@"
                    INSERT INTO BanquetBookingAuditLog(BanquetBookingId,BanquetBookingNumber,ActionType,ActionDescription,PerformedBy)
                    VALUES(@Id,@Num,'Cancelled',@Desc,@By)",
                    new { Id = bookingId, Num = booking?.BanquetBookingNumber ?? "", Desc = $"Booking cancelled. Reason: {reason}. Refund: ₹{preview.RefundAmount:0.00}", By = cancelledBy }, tx);

                tx.Commit();
                cancellation.Id = id;
                return cancellation;
            }
            catch { tx.Rollback(); throw; }
        }

        public async Task<string?> ProcessRefundAsync(int cancellationId, string paymentMethod, string reference, int processedBy, string refundNumber)
        {
            try
            {
                // Load the cancellation record to get booking details
                var cancellation = await _db.QueryFirstOrDefaultAsync<BanquetCancellation>(
                    "SELECT * FROM BanquetCancellations WHERE Id=@Id", new { Id = cancellationId });
                if (cancellation == null) return null;

                if (_db.State != ConnectionState.Open) _db.Open();
                using var tx = _db.BeginTransaction();
                try
                {
                    // Insert refund as a payment entry so it appears in Collection Register
                    await _db.ExecuteAsync(@"
                        INSERT INTO BanquetBookingPayments
                            (BanquetBookingId,ReceiptNumber,Amount,PaymentMethod,PaymentReference,
                             [Status],PaidOn,IsAdvancePayment,IsRefund,DiscountAmount,RoundOffAmount,CreatedBy)
                        VALUES
                            (@BanquetBookingId,@ReceiptNumber,@Amount,@Method,@Ref,
                             'Captured',SYSUTCDATETIME(),0,1,0,0,@CreatedBy)",
                        new
                        {
                            BanquetBookingId = cancellation.BanquetBookingId,
                            ReceiptNumber    = refundNumber,
                            Amount           = cancellation.RefundAmount,
                            Method           = paymentMethod,
                            Ref              = reference,
                            CreatedBy        = processedBy
                        }, tx);

                    // Update cancellation record with refund details
                    await _db.ExecuteAsync(@"
                        UPDATE BanquetCancellations
                        SET IsRefunded=1, RefundPaymentMethod=@Method, RefundReference=@Ref,
                            RefundNumber=@RefundNumber, RefundedOn=SYSUTCDATETIME(), ApprovalStatus='Approved'
                        WHERE Id=@Id",
                        new { Method = paymentMethod, Ref = reference, RefundNumber = refundNumber, Id = cancellationId }, tx);

                    tx.Commit();
                    return refundNumber;
                }
                catch
                {
                    tx.Rollback();
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<BanquetCancellation?> GetByBookingIdAsync(int bookingId) =>
            await _db.QueryFirstOrDefaultAsync<BanquetCancellation>(
                "SELECT * FROM BanquetCancellations WHERE BanquetBookingId=@Id ORDER BY Id DESC",
                new { Id = bookingId });
    }
}
