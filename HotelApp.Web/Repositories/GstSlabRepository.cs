using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public class GstSlabRepository : IGstSlabRepository
    {
        private readonly IDbConnection _connection;

        public GstSlabRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<GstSlab>> GetAllAsync(int branchId)
        {
            const string sql = @"
                SELECT gs.Id,
                       gs.BranchID,
                       gs.SlabCode,
                       gs.SlabName,
                       gs.EffectiveFrom,
                       gs.EffectiveTo,
                       gs.IsActive,
                       gs.CreatedDate,
                       gs.CreatedBy,
                       gs.UpdatedDate,
                       gs.UpdatedBy,
                       COUNT(CASE WHEN band.IsActive = 1 THEN 1 END) AS ActiveBandCount,
                       MIN(CASE WHEN band.IsActive = 1 THEN band.TariffFrom END) AS MinimumTariffFrom,
                       MAX(CASE WHEN band.IsActive = 1 THEN band.TariffTo END) AS MaximumTariffTo,
                       MAX(CASE WHEN band.IsActive = 1 THEN band.GstPercent END) AS MaximumGstPercent
                  FROM dbo.GstSlabs gs
             LEFT JOIN dbo.GstSlabBands band ON band.GstSlabId = gs.Id
                 WHERE gs.BranchID = @BranchID
              GROUP BY gs.Id,
                       gs.BranchID,
                       gs.SlabCode,
                       gs.SlabName,
                       gs.EffectiveFrom,
                       gs.EffectiveTo,
                       gs.IsActive,
                       gs.CreatedDate,
                       gs.CreatedBy,
                       gs.UpdatedDate,
                       gs.UpdatedBy
              ORDER BY gs.SlabName, gs.SlabCode;";

            return await _connection.QueryAsync<GstSlab>(sql, new { BranchID = branchId });
        }

        public async Task<GstSlab?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT gs.Id,
                       gs.SlabCode,
                       gs.SlabName,
                       gs.EffectiveFrom,
                       gs.EffectiveTo,
                       gs.IsActive,
                       gs.CreatedDate,
                       gs.CreatedBy,
                       gs.UpdatedDate,
                       gs.UpdatedBy,
                       COUNT(CASE WHEN band.IsActive = 1 THEN 1 END) AS ActiveBandCount,
                       MIN(CASE WHEN band.IsActive = 1 THEN band.TariffFrom END) AS MinimumTariffFrom,
                       MAX(CASE WHEN band.IsActive = 1 THEN band.TariffTo END) AS MaximumTariffTo,
                       MAX(CASE WHEN band.IsActive = 1 THEN band.GstPercent END) AS MaximumGstPercent
                  FROM dbo.GstSlabs gs
             LEFT JOIN dbo.GstSlabBands band ON band.GstSlabId = gs.Id
                 WHERE gs.Id = @Id
              GROUP BY gs.Id,
                       gs.SlabCode,
                       gs.SlabName,
                       gs.EffectiveFrom,
                       gs.EffectiveTo,
                       gs.IsActive,
                       gs.CreatedDate,
                       gs.CreatedBy,
                       gs.UpdatedDate,
                       gs.UpdatedBy;

                SELECT band.Id,
                       band.GstSlabId,
                       gs.SlabCode,
                       gs.SlabName,
                       band.TariffFrom,
                       band.TariffTo,
                       band.GstPercent,
                       band.CgstPercent,
                       band.SgstPercent,
                       band.IgstPercent,
                       band.SortOrder,
                       band.IsActive
                  FROM dbo.GstSlabBands band
                  JOIN dbo.GstSlabs gs ON gs.Id = band.GstSlabId
                 WHERE band.GstSlabId = @Id
              ORDER BY band.SortOrder, band.TariffFrom;";

            using var multi = await _connection.QueryMultipleAsync(sql, new { Id = id });
            var slab = await multi.ReadFirstOrDefaultAsync<GstSlab>();
            if (slab == null)
            {
                return null;
            }

            slab.TariffBands = (await multi.ReadAsync<GstSlabBand>()).ToList();
            return slab;
        }

        public async Task<int> CreateAsync(GstSlab slab)
        {
            const string sql = @"
                INSERT INTO dbo.GstSlabs
                    (SlabCode, SlabName, EffectiveFrom, EffectiveTo, IsActive, BranchID, CreatedDate, CreatedBy)
                VALUES
                    (@SlabCode, @SlabName, @EffectiveFrom, @EffectiveTo, @IsActive, @BranchID, SYSUTCDATETIME(), @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            var shouldCloseConnection = _connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                _connection.Open();
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                var slabId = await _connection.ExecuteScalarAsync<int>(sql, slab, transaction);
                await InsertBandsAsync(slabId, slab.TariffBands, transaction);
                transaction.Commit();
                return slabId;
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

        public async Task<bool> UpdateAsync(GstSlab slab)
        {
            const string sql = @"
                UPDATE dbo.GstSlabs
                   SET SlabCode = @SlabCode,
                       SlabName = @SlabName,
                       EffectiveFrom = @EffectiveFrom,
                       EffectiveTo = @EffectiveTo,
                       IsActive = @IsActive,
                       UpdatedDate = SYSUTCDATETIME(),
                       UpdatedBy = @UpdatedBy
                 WHERE Id = @Id AND BranchID = @BranchID;";

            const string deleteBandsSql = @"DELETE FROM dbo.GstSlabBands WHERE GstSlabId = @GstSlabId;";

            var shouldCloseConnection = _connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                _connection.Open();
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                var updated = await _connection.ExecuteAsync(sql, slab, transaction) > 0;
                await _connection.ExecuteAsync(deleteBandsSql, new { GstSlabId = slab.Id }, transaction);
                await InsertBandsAsync(slab.Id, slab.TariffBands, transaction);
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

        public async Task<bool> CodeExistsAsync(string slabCode, int branchId, int? excludeId = null)
        {
            var sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM dbo.GstSlabs WHERE SlabCode = @SlabCode AND BranchID = @BranchID AND Id <> @ExcludeId"
                : "SELECT COUNT(1) FROM dbo.GstSlabs WHERE SlabCode = @SlabCode AND BranchID = @BranchID";

            var count = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                SlabCode = slabCode,
                BranchID = branchId,
                ExcludeId = excludeId
            });

            return count > 0;
        }

        public async Task<GstSlabBand?> ResolveBandAsync(decimal tariffAmount, DateTime stayDate, int? gstSlabId = null)
        {
            var sql = @"
                SELECT TOP (1)
                       band.Id,
                       band.GstSlabId,
                       gs.SlabCode,
                       gs.SlabName,
                       band.TariffFrom,
                       band.TariffTo,
                       band.GstPercent,
                       band.CgstPercent,
                       band.SgstPercent,
                       band.IgstPercent,
                       band.SortOrder,
                       band.IsActive
                  FROM dbo.GstSlabBands band
                  JOIN dbo.GstSlabs gs ON gs.Id = band.GstSlabId
                 WHERE gs.IsActive = 1
                   AND band.IsActive = 1
                   AND gs.EffectiveFrom <= @StayDate
                   AND (gs.EffectiveTo IS NULL OR gs.EffectiveTo >= @StayDate)
                   AND @TariffAmount >= band.TariffFrom
                   AND (band.TariffTo IS NULL OR @TariffAmount <= band.TariffTo)";

            if (gstSlabId.HasValue)
            {
                sql += " AND gs.Id = @GstSlabId";
            }

            sql += " ORDER BY gs.EffectiveFrom DESC, band.SortOrder, band.TariffFrom;";

            return await _connection.QueryFirstOrDefaultAsync<GstSlabBand>(sql, new
            {
                TariffAmount = tariffAmount,
                StayDate = stayDate.Date,
                GstSlabId = gstSlabId
            });
        }

        private async Task InsertBandsAsync(int gstSlabId, IEnumerable<GstSlabBand>? bands, IDbTransaction transaction)
        {
            const string sql = @"
                INSERT INTO dbo.GstSlabBands
                    (GstSlabId, TariffFrom, TariffTo, GstPercent, CgstPercent, SgstPercent, IgstPercent, SortOrder, IsActive)
                VALUES
                    (@GstSlabId, @TariffFrom, @TariffTo, @GstPercent, @CgstPercent, @SgstPercent, @IgstPercent, @SortOrder, @IsActive);";

            if (bands == null)
            {
                return;
            }

            foreach (var band in bands)
            {
                await _connection.ExecuteAsync(sql, new
                {
                    GstSlabId = gstSlabId,
                    band.TariffFrom,
                    band.TariffTo,
                    band.GstPercent,
                    band.CgstPercent,
                    band.SgstPercent,
                    band.IgstPercent,
                    band.SortOrder,
                    band.IsActive
                }, transaction);
            }
        }
    }
}