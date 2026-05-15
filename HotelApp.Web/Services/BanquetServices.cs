using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Services
{
    // ── Banquet Booking Number Generator ─────────────────────────────────────

    public interface IBanquetBookingNumberService
    {
        Task<string> GenerateAsync(int branchId);
    }

    public class BanquetBookingNumberService : IBanquetBookingNumberService
    {
        private readonly IDbConnection _db;

        public BanquetBookingNumberService(IDbConnection db) => _db = db;

        public async Task<string> GenerateAsync(int branchId)
        {
            // Uses BanquetBookingCounter (separate from BanquetReceiptCounter which is for receipts).
            // Year-based reset: if the stored Year != current year, reset LastNumber to 0 first.
            var currentYear = DateTime.Today.Year;

            var sql = @"
                DECLARE @next INT;
                IF EXISTS (SELECT 1 FROM BanquetBookingCounter WHERE BranchID = @BranchID)
                BEGIN
                    -- Reset counter on new year
                    UPDATE BanquetBookingCounter
                    SET LastNumber = 0, [Year] = @CurrentYear
                    WHERE BranchID = @BranchID AND [Year] <> @CurrentYear;

                    UPDATE BanquetBookingCounter
                    SET LastNumber = LastNumber + 1
                    OUTPUT INSERTED.LastNumber
                    WHERE BranchID = @BranchID;
                END
                ELSE
                BEGIN
                    INSERT INTO BanquetBookingCounter (BranchID, [Year], LastNumber)
                    VALUES (@BranchID, @CurrentYear, 1);
                    SELECT 1;
                END";

            var counter = await _db.ExecuteScalarAsync<int>(sql, new { BranchID = branchId, CurrentYear = currentYear });
            // Format: BNQ-2026-000001
            return $"BNQ-{currentYear}-{counter:D6}";
        }
    }

    // ── Banquet GST Calculation Service ──────────────────────────────────────
    // ALL GST rates come from user-configured master data (venue, package, addon).
    // This service applies the inter-state rule: if IsInterState → use IGST, else CGST+SGST.
    // No GST rates are hardcoded here.

    public interface IBanquetGSTService
    {
        BanquetGSTBreakdown CalculateLineGST(decimal baseAmount, decimal cgstPct, decimal sgstPct, decimal igstPct, bool isInterState);
        BanquetGSTBreakdown CalculateVenueGST(BanquetVenue venue, decimal baseAmount, bool isInterState);
        BanquetGSTBreakdown CalculatePackageGST(BanquetPackage package, decimal baseAmount, bool isInterState);
        BanquetGSTBreakdown CalculateAddonGST(BanquetAddonService addon, decimal baseAmount, bool isInterState);
    }

    public class BanquetGSTBreakdown
    {
        public decimal BaseAmount { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal CGSTPercent { get; set; }
        public decimal SGSTPercent { get; set; }
        public decimal IGSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal IGSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class BanquetGSTService : IBanquetGSTService
    {
        public BanquetGSTBreakdown CalculateLineGST(decimal baseAmount, decimal cgstPct, decimal sgstPct, decimal igstPct, bool isInterState)
        {
            var result = new BanquetGSTBreakdown
            {
                BaseAmount = baseAmount,
                CGSTPercent = cgstPct,
                SGSTPercent = sgstPct,
                IGSTPercent = igstPct,
                GSTPercent = isInterState ? igstPct : cgstPct + sgstPct
            };

            if (isInterState)
            {
                result.IGSTAmount = Math.Round(baseAmount * igstPct / 100m, 2);
                result.CGSTAmount = 0;
                result.SGSTAmount = 0;
                result.GSTAmount  = result.IGSTAmount;
            }
            else
            {
                result.CGSTAmount = Math.Round(baseAmount * cgstPct / 100m, 2);
                result.SGSTAmount = Math.Round(baseAmount * sgstPct / 100m, 2);
                result.IGSTAmount = 0;
                result.GSTAmount  = result.CGSTAmount + result.SGSTAmount;
            }

            result.TotalAmount = baseAmount + result.GSTAmount;
            return result;
        }

        public BanquetGSTBreakdown CalculateVenueGST(BanquetVenue venue, decimal baseAmount, bool isInterState) =>
            CalculateLineGST(baseAmount, venue.CGSTPercent, venue.SGSTPercent, venue.IGSTPercent, isInterState);

        public BanquetGSTBreakdown CalculatePackageGST(BanquetPackage package, decimal baseAmount, bool isInterState) =>
            CalculateLineGST(baseAmount, package.CGSTPercent, package.SGSTPercent, package.IGSTPercent, isInterState);

        public BanquetGSTBreakdown CalculateAddonGST(BanquetAddonService addon, decimal baseAmount, bool isInterState) =>
            CalculateLineGST(baseAmount, addon.CGSTPercent, addon.SGSTPercent, addon.IGSTPercent, isInterState);
    }
}
