using System.Data;
using Dapper;
using HotelApp.Web.Models;
using HotelApp.Web.ViewModels;

namespace HotelApp.Web.Repositories
{
    public class AssetManagementRepository : IAssetManagementRepository
    {
        private readonly IDbConnection _db;

        public AssetManagementRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<AssetDashboardSummary> GetDashboardSummaryAsync(int branchId, DateTime fromDate, DateTime toDate)
        {
            var fromUtc = fromDate.Date;
            var toUtcExclusive = toDate.Date.AddDays(1);

            // MovementType values:
            // IN: OpeningStockIn(1), ReturnIn(2), DamageRecoveryIn(3)
            // OUT: DepartmentIssueOut(10), RoomAllocationOut(11), GuestIssueOut(12), ConsumableUsageOut(13), AutoCheckoutConsumableOut(14)
            const string sql = @"
                SELECT
                    (SELECT COUNT(1) FROM AssetItems WHERE BranchID = @BranchID AND IsActive = 1) AS ActiveItemsCount,

                    (SELECT COUNT(1)
                     FROM AssetItems i
                     LEFT JOIN AssetStockBalances b ON b.BranchID = i.BranchID AND b.ItemId = i.Id
                     WHERE i.BranchID = @BranchID AND i.IsActive = 1 AND i.ThresholdQty IS NOT NULL AND ISNULL(b.OnHandQty, 0) < i.ThresholdQty
                    ) AS LowStockItemsCount,

                    (SELECT COUNT(1)
                     FROM AssetItems i
                     LEFT JOIN AssetStockBalances b ON b.BranchID = i.BranchID AND b.ItemId = i.Id
                     WHERE i.BranchID = @BranchID AND i.IsActive = 1 AND ISNULL(b.OnHandQty, 0) < 0
                    ) AS NegativeStockItemsCount,

                    (SELECT COUNT(1)
                     FROM AssetMovements m
                     WHERE m.BranchID = @BranchID AND m.MovementDate >= @FromDate AND m.MovementDate < @ToDateExclusive
                    ) AS MovementsCount,

                    (SELECT COUNT(1)
                     FROM AssetMovements m
                     WHERE m.BranchID = @BranchID AND m.MovementDate >= @FromDate AND m.MovementDate < @ToDateExclusive
                       AND m.MovementType IN (1,2,3)
                    ) AS MovementsInCount,

                    (SELECT COUNT(1)
                     FROM AssetMovements m
                     WHERE m.BranchID = @BranchID AND m.MovementDate >= @FromDate AND m.MovementDate < @ToDateExclusive
                       AND m.MovementType IN (10,11,12,13,14)
                    ) AS MovementsOutCount,

                    (SELECT ISNULL(SUM(l.Qty), 0)
                     FROM AssetMovements m
                     INNER JOIN AssetMovementLines l ON l.MovementId = m.Id
                     WHERE m.BranchID = @BranchID AND m.MovementDate >= @FromDate AND m.MovementDate < @ToDateExclusive
                       AND m.MovementType IN (1,2,3)
                    ) AS TotalInQty,

                    (SELECT ISNULL(SUM(l.Qty), 0)
                     FROM AssetMovements m
                     INNER JOIN AssetMovementLines l ON l.MovementId = m.Id
                     WHERE m.BranchID = @BranchID AND m.MovementDate >= @FromDate AND m.MovementDate < @ToDateExclusive
                       AND m.MovementType IN (10,11,12,13,14)
                    ) AS TotalOutQty,

                    (SELECT COUNT(1)
                     FROM AssetDamageLoss dl
                     WHERE dl.BranchID = @BranchID AND dl.ReportedOn >= @FromDate AND dl.ReportedOn < @ToDateExclusive
                    ) AS DamageLossCount,

                    (SELECT COUNT(1)
                     FROM AssetDamageLoss dl
                     WHERE dl.BranchID = @BranchID AND dl.Status = 1
                    ) AS DamageLossPendingCount,

                    (SELECT COUNT(1)
                     FROM AssetDamageLossRecoveries r
                     INNER JOIN AssetDamageLoss dl ON dl.Id = r.DamageLossId
                     WHERE dl.BranchID = @BranchID AND r.CreatedDate >= @FromDate AND r.CreatedDate < @ToDateExclusive
                    ) AS RecoveriesCount,

                    (SELECT ISNULL(SUM(r.Amount), 0)
                     FROM AssetDamageLossRecoveries r
                     INNER JOIN AssetDamageLoss dl ON dl.Id = r.DamageLossId
                     WHERE dl.BranchID = @BranchID AND r.CreatedDate >= @FromDate AND r.CreatedDate < @ToDateExclusive
                    ) AS RecoveryAmount;";

            return await _db.QueryFirstAsync<AssetDashboardSummary>(sql, new
            {
                BranchID = branchId,
                FromDate = fromUtc,
                ToDateExclusive = toUtcExclusive
            });
        }

        public async Task<IEnumerable<AssetDepartment>> GetDepartmentsAsync(int branchId)
        {
            const string sql = @"
                SELECT Id, BranchID, [Name], IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM AssetDepartments
                WHERE BranchID = @BranchID
                ORDER BY [Name]";

            return await _db.QueryAsync<AssetDepartment>(sql, new { BranchID = branchId });
        }

        public async Task<int> CreateDepartmentAsync(AssetDepartment row)
        {
            const string sql = @"
                INSERT INTO AssetDepartments (BranchID, [Name], IsActive, CreatedDate, CreatedBy)
                VALUES (@BranchID, @Name, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _db.ExecuteScalarAsync<int>(sql, row);
        }

        public async Task<bool> UpdateDepartmentAsync(AssetDepartment row)
        {
            const string sql = @"
                UPDATE AssetDepartments
                SET [Name] = @Name,
                    IsActive = @IsActive,
                    UpdatedDate = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND BranchID = @BranchID";

            return (await _db.ExecuteAsync(sql, row)) > 0;
        }

        public async Task<IEnumerable<AssetUnit>> GetUnitsAsync(int branchId)
        {
            const string sql = @"
                SELECT Id, BranchID, [Name], IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                FROM AssetUnits
                WHERE BranchID = @BranchID
                ORDER BY [Name]";

            return await _db.QueryAsync<AssetUnit>(sql, new { BranchID = branchId });
        }

        public async Task<int> CreateUnitAsync(AssetUnit row)
        {
            const string sql = @"
                INSERT INTO AssetUnits (BranchID, [Name], IsActive, CreatedDate, CreatedBy)
                VALUES (@BranchID, @Name, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _db.ExecuteScalarAsync<int>(sql, row);
        }

        public async Task<bool> UpdateUnitAsync(AssetUnit row)
        {
            const string sql = @"
                UPDATE AssetUnits
                SET [Name] = @Name,
                    IsActive = @IsActive,
                    UpdatedDate = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND BranchID = @BranchID";

            return (await _db.ExecuteAsync(sql, row)) > 0;
        }

        public async Task<IEnumerable<AssetItemLookupRow>> GetItemLookupAsync(int branchId)
        {
            const string sql = @"
                SELECT i.Id,
                       i.Code,
                       i.[Name],
                       u.[Name] AS UnitName,
                       i.Category,
                       i.RequiresCustodian,
                       i.IsChargeable
                FROM AssetItems i
                INNER JOIN AssetUnits u ON u.Id = i.UnitId
                WHERE i.BranchID = @BranchID AND i.IsActive = 1
                ORDER BY i.[Name], i.Code";

            return await _db.QueryAsync<AssetItemLookupRow>(sql, new { BranchID = branchId });
        }

        public async Task<bool> ItemCodeExistsAsync(int branchId, string code, int? excludeItemId = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM AssetItems
                WHERE BranchID = @BranchID
                  AND Code = @Code
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";

            var count = await _db.ExecuteScalarAsync<int>(sql, new { BranchID = branchId, Code = code?.Trim(), ExcludeId = excludeItemId });
            return count > 0;
        }

        public async Task<AssetItem?> GetItemByIdAsync(int id, int branchId)
        {
            const string sql = @"
                SELECT i.Id, i.BranchID, i.Code, i.[Name], i.Category, i.UnitId,
                       i.IsRoomEligible, i.IsChargeable, i.ThresholdQty, i.RequiresCustodian,
                       i.IsActive, i.CreatedDate, i.CreatedBy, i.UpdatedDate, i.UpdatedBy,
                       u.[Name] AS UnitName
                FROM AssetItems i
                INNER JOIN AssetUnits u ON u.Id = i.UnitId
                WHERE i.Id = @Id AND i.BranchID = @BranchID";

            var item = await _db.QueryFirstOrDefaultAsync<AssetItem>(sql, new { Id = id, BranchID = branchId });
            if (item == null)
            {
                return null;
            }

            const string deptSql = @"SELECT DepartmentId FROM AssetItemDepartments WHERE ItemId = @ItemId";
            item.EligibleDepartmentIds = (await _db.QueryAsync<int>(deptSql, new { ItemId = item.Id })).ToList();
            return item;
        }

        public async Task<int> CreateItemAsync(AssetItem item)
        {
            const string sql = @"
                INSERT INTO AssetItems
                    (BranchID, Code, [Name], Category, UnitId, IsRoomEligible, IsChargeable, ThresholdQty, RequiresCustodian, IsActive, CreatedDate, CreatedBy)
                VALUES
                    (@BranchID, @Code, @Name, @Category, @UnitId, @IsRoomEligible, @IsChargeable, @ThresholdQty, @RequiresCustodian, @IsActive, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _db.ExecuteScalarAsync<int>(sql, new
            {
                item.BranchID,
                item.Code,
                item.Name,
                Category = (int)item.Category,
                item.UnitId,
                item.IsRoomEligible,
                item.IsChargeable,
                item.ThresholdQty,
                item.RequiresCustodian,
                item.IsActive,
                item.CreatedBy
            });
        }

        public async Task<bool> UpdateItemAsync(AssetItem item)
        {
            const string sql = @"
                UPDATE AssetItems
                SET Code = @Code,
                    [Name] = @Name,
                    Category = @Category,
                    UnitId = @UnitId,
                    IsRoomEligible = @IsRoomEligible,
                    IsChargeable = @IsChargeable,
                    ThresholdQty = @ThresholdQty,
                    RequiresCustodian = @RequiresCustodian,
                    IsActive = @IsActive,
                    UpdatedDate = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND BranchID = @BranchID";

            var rows = await _db.ExecuteAsync(sql, new
            {
                item.Id,
                item.BranchID,
                item.Code,
                item.Name,
                Category = (int)item.Category,
                item.UnitId,
                item.IsRoomEligible,
                item.IsChargeable,
                item.ThresholdQty,
                item.RequiresCustodian,
                item.IsActive,
                UpdatedBy = item.UpdatedBy
            });

            return rows > 0;
        }

        public async Task SetItemDepartmentsAsync(int itemId, IReadOnlyCollection<int> departmentIds, int? performedBy)
        {
            if (_db.State != ConnectionState.Open)
            {
                _db.Open();
            }

            using var tx = _db.BeginTransaction();

            await _db.ExecuteAsync("DELETE FROM AssetItemDepartments WHERE ItemId = @ItemId", new { ItemId = itemId }, tx);

            if (departmentIds != null)
            {
                foreach (var deptId in departmentIds.Distinct())
                {
                    await _db.ExecuteAsync(
                        "INSERT INTO AssetItemDepartments (ItemId, DepartmentId, CreatedDate, CreatedBy) VALUES (@ItemId, @DepartmentId, SYSUTCDATETIME(), @CreatedBy)",
                        new { ItemId = itemId, DepartmentId = deptId, CreatedBy = performedBy },
                        tx);
                }
            }

            tx.Commit();
        }

        public async Task<IEnumerable<AssetConsumableStandard>> GetConsumableStandardsAsync(int branchId)
        {
            const string sql = @"
                SELECT cs.Id, cs.BranchID, cs.ItemId, cs.PerRoomPerDayQty, cs.PerStayQty, cs.IsActive,
                       cs.CreatedDate, cs.CreatedBy, cs.UpdatedDate, cs.UpdatedBy,
                       i.Code AS ItemCode, i.[Name] AS ItemName
                FROM AssetConsumableStandards cs
                INNER JOIN AssetItems i ON i.Id = cs.ItemId
                WHERE cs.BranchID = @BranchID
                ORDER BY i.[Name], i.Code";

            return await _db.QueryAsync<AssetConsumableStandard>(sql, new { BranchID = branchId });
        }

        public async Task<int> UpsertConsumableStandardAsync(AssetConsumableStandard row)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM AssetConsumableStandards WHERE BranchID = @BranchID AND ItemId = @ItemId)
                BEGIN
                    UPDATE AssetConsumableStandards
                    SET PerRoomPerDayQty = @PerRoomPerDayQty,
                        PerStayQty = @PerStayQty,
                        IsActive = @IsActive,
                        UpdatedDate = SYSUTCDATETIME(),
                        UpdatedBy = @UpdatedBy
                    WHERE BranchID = @BranchID AND ItemId = @ItemId;

                    SELECT Id FROM AssetConsumableStandards WHERE BranchID = @BranchID AND ItemId = @ItemId;
                END
                ELSE
                BEGIN
                    INSERT INTO AssetConsumableStandards
                        (BranchID, ItemId, PerRoomPerDayQty, PerStayQty, IsActive, CreatedDate, CreatedBy)
                    VALUES
                        (@BranchID, @ItemId, @PerRoomPerDayQty, @PerStayQty, @IsActive, SYSUTCDATETIME(), @CreatedBy);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END";

            return await _db.ExecuteScalarAsync<int>(sql, new
            {
                row.BranchID,
                row.ItemId,
                row.PerRoomPerDayQty,
                row.PerStayQty,
                row.IsActive,
                CreatedBy = row.CreatedBy,
                UpdatedBy = row.UpdatedBy
            });
        }

        public async Task<IEnumerable<RoomLookupRow>> GetRoomsLookupAsync(int branchId)
        {
            const string sql = @"
                SELECT Id, RoomNumber
                FROM Rooms
                WHERE BranchID = @BranchID
                ORDER BY RoomNumber";

            return await _db.QueryAsync<RoomLookupRow>(sql, new { BranchID = branchId });
        }

        private static bool IsStockIn(AssetMovementType t)
            => t is AssetMovementType.OpeningStockIn
                or AssetMovementType.ReturnIn
                or AssetMovementType.DamageRecoveryIn;

        private static bool IsStockOut(AssetMovementType t)
            => t is AssetMovementType.DepartmentIssueOut
                or AssetMovementType.RoomAllocationOut
                or AssetMovementType.GuestIssueOut
                or AssetMovementType.ConsumableUsageOut
                or AssetMovementType.AutoCheckoutConsumableOut;

        public async Task<(bool ok, string? errorMessage, int movementId)> CreateMovementAsync(AssetMovement movement)
        {
            if (movement == null || movement.Lines == null || movement.Lines.Count == 0)
            {
                return (false, "At least one line is required.", 0);
            }

            if (_db.State != ConnectionState.Open)
            {
                _db.Open();
            }

            using var tx = _db.BeginTransaction();

            try
            {
                const string insertMovementSql = @"
                    INSERT INTO AssetMovements
                        (BranchID, MovementType, MovementDate, BookingId, BookingNumber, RoomId, FromDepartmentId, ToDepartmentId, GuestName, CustodianName, Notes, AllowNegativeOverride, CreatedDate, CreatedBy)
                    VALUES
                        (@BranchID, @MovementType, SYSUTCDATETIME(), @BookingId, @BookingNumber, @RoomId, @FromDepartmentId, @ToDepartmentId, @GuestName, @CustodianName, @Notes, @AllowNegativeOverride, SYSUTCDATETIME(), @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var movementId = await _db.ExecuteScalarAsync<int>(insertMovementSql, new
                {
                    movement.BranchID,
                    MovementType = (int)movement.MovementType,
                    movement.BookingId,
                    movement.BookingNumber,
                    movement.RoomId,
                    movement.FromDepartmentId,
                    movement.ToDepartmentId,
                    movement.GuestName,
                    movement.CustodianName,
                    movement.Notes,
                    movement.AllowNegativeOverride,
                    movement.CreatedBy
                }, tx);

                const string itemInfoSql = @"
                    SELECT Id, Category
                    FROM AssetItems
                    WHERE BranchID = @BranchID AND Id IN @Ids";

                var itemInfos = (await _db.QueryAsync<(int Id, int Category)>(
                    itemInfoSql,
                    new { BranchID = movement.BranchID, Ids = movement.Lines.Select(l => l.ItemId).Distinct().ToArray() },
                    tx)).ToDictionary(x => x.Id, x => (AssetItemCategory)x.Category);

                const string insertLineSql = @"
                    INSERT INTO AssetMovementLines (MovementId, ItemId, Qty, SerialNumber, LineNote)
                    VALUES (@MovementId, @ItemId, @Qty, @SerialNumber, @LineNote);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                foreach (var line in movement.Lines)
                {
                    if (line.Qty <= 0)
                    {
                        tx.Rollback();
                        return (false, "Qty must be > 0.", 0);
                    }

                    await _db.ExecuteScalarAsync<int>(insertLineSql, new
                    {
                        MovementId = movementId,
                        line.ItemId,
                        line.Qty,
                        line.SerialNumber,
                        LineNote = line.LineNote
                    }, tx);

                    if (!itemInfos.TryGetValue(line.ItemId, out var category))
                    {
                        tx.Rollback();
                        return (false, "Invalid item selected.", 0);
                    }

                    decimal delta = 0m;
                    if (IsStockIn(movement.MovementType))
                    {
                        delta = line.Qty;
                    }
                    else if (IsStockOut(movement.MovementType))
                    {
                        delta = -line.Qty;
                    }

                    if (delta != 0m)
                    {
                        // Upsert + lock row
                        const string lockSql = @"
                            SELECT OnHandQty
                            FROM AssetStockBalances WITH (UPDLOCK, HOLDLOCK)
                            WHERE BranchID = @BranchID AND ItemId = @ItemId";

                        var current = await _db.QueryFirstOrDefaultAsync<decimal?>(lockSql, new { BranchID = movement.BranchID, ItemId = line.ItemId }, tx);
                        if (!current.HasValue)
                        {
                            await _db.ExecuteAsync(
                                "INSERT INTO AssetStockBalances (BranchID, ItemId, OnHandQty, UpdatedDate) VALUES (@BranchID, @ItemId, 0, SYSUTCDATETIME())",
                                new { BranchID = movement.BranchID, ItemId = line.ItemId },
                                tx);

                            current = 0m;
                        }

                        var newQty = current.Value + delta;

                        var isConsumable = category == AssetItemCategory.Consumable;
                        if (newQty < 0m)
                        {
                            if (!(isConsumable && movement.AllowNegativeOverride))
                            {
                                tx.Rollback();
                                return (false, "Stock would become negative. Operation blocked.", 0);
                            }
                        }

                        await _db.ExecuteAsync(
                            "UPDATE AssetStockBalances SET OnHandQty = @Qty, UpdatedDate = SYSUTCDATETIME() WHERE BranchID = @BranchID AND ItemId = @ItemId",
                            new { Qty = newQty, BranchID = movement.BranchID, ItemId = line.ItemId },
                            tx);
                    }

                    // Allocations (for issue types)
                    if (movement.MovementType == AssetMovementType.DepartmentIssueOut)
                    {
                        if (!movement.ToDepartmentId.HasValue)
                        {
                            tx.Rollback();
                            return (false, "To Department is required.", 0);
                        }

                        await _db.ExecuteAsync(@"
                            INSERT INTO AssetAllocations
                                (BranchID, AllocationType, ItemId, Qty, DepartmentId, CustodianName, IsFixed, IssuedOn, Status, SourceMovementId)
                            VALUES
                                (@BranchID, @AllocationType, @ItemId, @Qty, @DepartmentId, @CustodianName, 0, SYSUTCDATETIME(), @Status, @SourceMovementId)",
                            new
                            {
                                BranchID = movement.BranchID,
                                AllocationType = (int)AssetAllocationType.Department,
                                ItemId = line.ItemId,
                                Qty = line.Qty,
                                DepartmentId = movement.ToDepartmentId.Value,
                                CustodianName = movement.CustodianName ?? string.Empty,
                                Status = (int)AssetAllocationStatus.Open,
                                SourceMovementId = movementId
                            }, tx);
                    }
                    else if (movement.MovementType == AssetMovementType.RoomAllocationOut)
                    {
                        if (!movement.RoomId.HasValue)
                        {
                            tx.Rollback();
                            return (false, "Room is required.", 0);
                        }

                        await _db.ExecuteAsync(@"
                            INSERT INTO AssetAllocations
                                (BranchID, AllocationType, ItemId, Qty, RoomId, CustodianName, IsFixed, IssuedOn, Status, SourceMovementId)
                            VALUES
                                (@BranchID, @AllocationType, @ItemId, @Qty, @RoomId, @CustodianName, 1, SYSUTCDATETIME(), @Status, @SourceMovementId)",
                            new
                            {
                                BranchID = movement.BranchID,
                                AllocationType = (int)AssetAllocationType.Room,
                                ItemId = line.ItemId,
                                Qty = line.Qty,
                                RoomId = movement.RoomId.Value,
                                CustodianName = movement.CustodianName ?? string.Empty,
                                Status = (int)AssetAllocationStatus.Open,
                                SourceMovementId = movementId
                            }, tx);
                    }
                    else if (movement.MovementType == AssetMovementType.GuestIssueOut)
                    {
                        if (string.IsNullOrWhiteSpace(movement.BookingNumber))
                        {
                            tx.Rollback();
                            return (false, "Booking number is required for guest issue.", 0);
                        }

                        await _db.ExecuteAsync(@"
                            INSERT INTO AssetAllocations
                                (BranchID, AllocationType, ItemId, Qty, BookingId, BookingNumber, GuestName, CustodianName, IsFixed, IssuedOn, Status, SourceMovementId)
                            VALUES
                                (@BranchID, @AllocationType, @ItemId, @Qty, @BookingId, @BookingNumber, @GuestName, @CustodianName, 0, SYSUTCDATETIME(), @Status, @SourceMovementId)",
                            new
                            {
                                BranchID = movement.BranchID,
                                AllocationType = (int)AssetAllocationType.Guest,
                                ItemId = line.ItemId,
                                Qty = line.Qty,
                                BookingId = movement.BookingId,
                                BookingNumber = movement.BookingNumber,
                                GuestName = movement.GuestName,
                                CustodianName = movement.CustodianName ?? string.Empty,
                                Status = (int)AssetAllocationStatus.Open,
                                SourceMovementId = movementId
                            }, tx);
                    }
                }

                tx.Commit();
                return (true, null, movementId);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, ex.Message, 0);
            }
        }

        public async Task<IEnumerable<AssetMovementListRow>> GetMovementListAsync(int branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            const string sql = @"
                SELECT m.Id,
                       m.MovementDate,
                                             CASE m.MovementType
                                                        WHEN 1 THEN 'Opening Stock (IN)'
                                                        WHEN 2 THEN 'Return (IN)'
                                                        WHEN 3 THEN 'Transfer (IN)'
                                                        WHEN 4 THEN 'Damage Recovery (IN)'
                                                        WHEN 10 THEN 'Department Issue (OUT)'
                                                        WHEN 11 THEN 'Room Allocation (OUT)'
                                                        WHEN 12 THEN 'Guest Issue (OUT)'
                                                        WHEN 13 THEN 'Consumable Usage (OUT)'
                                                        WHEN 14 THEN 'Transfer (OUT)'
                                                        WHEN 20 THEN 'Auto Checkout Consumables (OUT)'
                                                        ELSE CONCAT('Type ', m.MovementType)
                                             END AS MovementType,
                       m.BookingNumber,
                       m.GuestName,
                       fd.[Name] AS FromDepartment,
                       td.[Name] AS ToDepartment,
                       m.Notes
                FROM AssetMovements m
                LEFT JOIN AssetDepartments fd ON fd.Id = m.FromDepartmentId
                LEFT JOIN AssetDepartments td ON td.Id = m.ToDepartmentId
                WHERE m.BranchID = @BranchID
                  AND (@FromDate IS NULL OR CAST(m.MovementDate AS DATE) >= @FromDate)
                  AND (@ToDate IS NULL OR CAST(m.MovementDate AS DATE) <= @ToDate)
                ORDER BY m.MovementDate DESC, m.Id DESC";

            // Friendly movement type mapping in code (View can translate ints too); here keep int string.
            return await _db.QueryAsync<AssetMovementListRow>(sql, new
            {
                BranchID = branchId,
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date
            });
        }

        public async Task<AssetMovement?> GetMovementByIdAsync(int id, int branchId)
        {
            const string sql = @"
                SELECT Id, BranchID, MovementType, MovementDate, BookingId, BookingNumber, RoomId,
                       FromDepartmentId, ToDepartmentId, GuestName, CustodianName, Notes, AllowNegativeOverride, CreatedDate, CreatedBy
                FROM AssetMovements
                WHERE Id = @Id AND BranchID = @BranchID";

            var movement = await _db.QueryFirstOrDefaultAsync<AssetMovement>(sql, new { Id = id, BranchID = branchId });
            if (movement == null)
            {
                return null;
            }

            const string linesSql = @"
                SELECT l.Id, l.MovementId, l.ItemId, l.Qty, l.SerialNumber, l.LineNote,
                       i.Code AS ItemCode, i.[Name] AS ItemName, u.[Name] AS UnitName
                FROM AssetMovementLines l
                INNER JOIN AssetItems i ON i.Id = l.ItemId
                INNER JOIN AssetUnits u ON u.Id = i.UnitId
                WHERE l.MovementId = @MovementId
                ORDER BY l.Id";

            movement.Lines = (await _db.QueryAsync<AssetMovementLine>(linesSql, new { MovementId = movement.Id })).ToList();
            return movement;
        }

        public async Task<IEnumerable<AssetStockReportRow>> GetStockReportAsync(int branchId)
        {
            const string sql = @"
                SELECT i.Id AS ItemId,
                       i.Code,
                       i.[Name],
                       CASE i.Category WHEN 1 THEN 'Asset' WHEN 2 THEN 'Reusable' WHEN 3 THEN 'Consumable' ELSE 'Other' END AS Category,
                       u.[Name] AS Unit,
                       ISNULL(b.OnHandQty, 0) AS OnHandQty,
                       i.ThresholdQty
                FROM AssetItems i
                INNER JOIN AssetUnits u ON u.Id = i.UnitId
                LEFT JOIN AssetStockBalances b ON b.BranchID = i.BranchID AND b.ItemId = i.Id
                WHERE i.BranchID = @BranchID AND i.IsActive = 1
                ORDER BY i.[Name], i.Code";

            return await _db.QueryAsync<AssetStockReportRow>(sql, new { BranchID = branchId });
        }

        public async Task<IEnumerable<AssetAllocation>> GetOpenAllocationsAsync(int branchId, AssetAllocationType? type = null)
        {
            const string sql = @"
                SELECT a.Id, a.BranchID, a.AllocationType, a.ItemId, a.Qty,
                       a.DepartmentId, a.RoomId, a.BookingId, a.BookingNumber, a.GuestName,
                       a.CustodianName, a.IsFixed, a.IssuedOn, a.ReturnedOn, a.Status, a.SourceMovementId,
                       i.Code AS ItemCode, i.[Name] AS ItemName,
                       d.[Name] AS DepartmentName,
                       r.RoomNumber
                FROM AssetAllocations a
                INNER JOIN AssetItems i ON i.Id = a.ItemId
                LEFT JOIN AssetDepartments d ON d.Id = a.DepartmentId
                LEFT JOIN Rooms r ON r.Id = a.RoomId
                WHERE a.BranchID = @BranchID
                  AND a.Status = 1
                  AND (@Type IS NULL OR a.AllocationType = @Type)
                ORDER BY a.IssuedOn DESC, a.Id DESC";

            return await _db.QueryAsync<AssetAllocation>(sql, new
            {
                BranchID = branchId,
                Type = type.HasValue ? (int)type.Value : (int?)null
            });
        }

        public async Task<int> CreateDamageLossAsync(AssetDamageLossRecord record)
        {
            const string sql = @"
                INSERT INTO AssetDamageLoss
                    (BranchID, ItemId, Qty, IssueType, Reason, BookingId, BookingNumber, RoomId, DepartmentId, GuestName,
                     Status, ReportedOn, ReportedBy, CreatedDate, CreatedBy)
                VALUES
                    (@BranchID, @ItemId, @Qty, @IssueType, @Reason, @BookingId, @BookingNumber, @RoomId, @DepartmentId, @GuestName,
                     @Status, SYSUTCDATETIME(), @ReportedBy, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _db.ExecuteScalarAsync<int>(sql, new
            {
                record.BranchID,
                record.ItemId,
                record.Qty,
                IssueType = (int)record.IssueType,
                record.Reason,
                record.BookingId,
                record.BookingNumber,
                record.RoomId,
                record.DepartmentId,
                record.GuestName,
                Status = (int)AssetDamageLossStatus.Pending,
                ReportedBy = record.ReportedBy,
                CreatedBy = record.CreatedBy
            });
        }

        public async Task<IEnumerable<AssetDamageLossRecord>> GetDamageLossListAsync(int branchId, AssetDamageLossStatus? status = null)
        {
            const string sql = @"
                SELECT dl.Id, dl.BranchID, dl.ItemId, dl.Qty, dl.IssueType, dl.Reason,
                       dl.BookingId, dl.BookingNumber, dl.RoomId, dl.DepartmentId, dl.GuestName,
                       dl.Status, dl.ReportedOn, dl.ReportedBy, dl.ApprovedOn, dl.ApprovedBy,
                       i.Code AS ItemCode, i.[Name] AS ItemName,
                       d.[Name] AS DepartmentName,
                       r.RoomNumber
                FROM AssetDamageLoss dl
                INNER JOIN AssetItems i ON i.Id = dl.ItemId
                LEFT JOIN AssetDepartments d ON d.Id = dl.DepartmentId
                LEFT JOIN Rooms r ON r.Id = dl.RoomId
                WHERE dl.BranchID = @BranchID
                  AND (@Status IS NULL OR dl.Status = @Status)
                ORDER BY dl.Id DESC";

            return await _db.QueryAsync<AssetDamageLossRecord>(sql, new
            {
                BranchID = branchId,
                Status = status.HasValue ? (int)status.Value : (int?)null
            });
        }

        public async Task<AssetDamageLossRecord?> GetDamageLossByIdAsync(int id, int branchId)
        {
            const string sql = @"
                SELECT dl.Id, dl.BranchID, dl.ItemId, dl.Qty, dl.IssueType, dl.Reason,
                       dl.BookingId, dl.BookingNumber, dl.RoomId, dl.DepartmentId, dl.GuestName,
                       dl.Status, dl.ReportedOn, dl.ReportedBy, dl.ApprovedOn, dl.ApprovedBy,
                       i.Code AS ItemCode, i.[Name] AS ItemName,
                       d.[Name] AS DepartmentName,
                       r.RoomNumber
                FROM AssetDamageLoss dl
                INNER JOIN AssetItems i ON i.Id = dl.ItemId
                LEFT JOIN AssetDepartments d ON d.Id = dl.DepartmentId
                LEFT JOIN Rooms r ON r.Id = dl.RoomId
                WHERE dl.Id = @Id AND dl.BranchID = @BranchID";

            var record = await _db.QueryFirstOrDefaultAsync<AssetDamageLossRecord>(sql, new { Id = id, BranchID = branchId });
            if (record == null)
            {
                return null;
            }

            const string recSql = @"
                SELECT Id, DamageLossId, RecoveryType, Amount, Notes, BookingOtherChargeId, CreatedDate, CreatedBy
                FROM AssetDamageLossRecoveries
                WHERE DamageLossId = @Id
                ORDER BY Id DESC";

            record.Recoveries = (await _db.QueryAsync<AssetDamageLossRecovery>(recSql, new { Id = record.Id })).ToList();
            return record;
        }

        public async Task<bool> ApproveDamageLossAsync(int id, int branchId, int approvedBy)
        {
            const string sql = @"
                UPDATE AssetDamageLoss
                SET Status = @Status,
                    ApprovedOn = SYSUTCDATETIME(),
                    ApprovedBy = @ApprovedBy
                WHERE Id = @Id AND BranchID = @BranchID AND Status = @Pending";

            var rows = await _db.ExecuteAsync(sql, new
            {
                Id = id,
                BranchID = branchId,
                Status = (int)AssetDamageLossStatus.Approved,
                ApprovedBy = approvedBy,
                Pending = (int)AssetDamageLossStatus.Pending
            });

            return rows > 0;
        }

        public async Task<int> CreateRecoveryAsync(AssetDamageLossRecovery recovery)
        {
            const string sql = @"
                INSERT INTO AssetDamageLossRecoveries
                    (DamageLossId, RecoveryType, Amount, Notes, BookingOtherChargeId, CreatedDate, CreatedBy)
                VALUES
                    (@DamageLossId, @RecoveryType, @Amount, @Notes, @BookingOtherChargeId, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _db.ExecuteScalarAsync<int>(sql, new
            {
                recovery.DamageLossId,
                RecoveryType = (int)recovery.RecoveryType,
                recovery.Amount,
                recovery.Notes,
                recovery.BookingOtherChargeId,
                recovery.CreatedBy
            });
        }
    }
}
