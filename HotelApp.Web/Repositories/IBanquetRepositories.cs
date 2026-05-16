using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IBanquetVenueRepository
    {
        Task<IEnumerable<BanquetVenue>> GetByBranchAsync(int branchId, bool activeOnly = true);
        Task<BanquetVenue?> GetByIdAsync(int id);
        Task<int> CreateAsync(BanquetVenue venue);
        Task<bool> UpdateAsync(BanquetVenue venue);
        Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null);
        Task<bool> IsVenueAvailableAsync(int venueId, DateOnly eventDate, TimeOnly? startTime, TimeOnly? endTime, int? excludeBookingId = null);
    }

    public interface IBanquetEventTypeRepository
    {
        Task<IEnumerable<BanquetEventType>> GetByBranchAsync(int branchId, bool activeOnly = true);
        Task<BanquetEventType?> GetByIdAsync(int id);
        Task<int> CreateAsync(BanquetEventType eventType);
        Task<bool> UpdateAsync(BanquetEventType eventType);
        Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null);
    }

    public interface IBanquetPackageRepository
    {
        Task<IEnumerable<BanquetPackage>> GetByBranchAsync(int branchId, bool activeOnly = true);
        Task<BanquetPackage?> GetByIdAsync(int id);
        Task<int> CreateAsync(BanquetPackage package);
        Task<bool> UpdateAsync(BanquetPackage package);
        Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null);
    }

    public interface IBanquetAddonServiceRepository
    {
        Task<IEnumerable<BanquetAddonService>> GetByBranchAsync(int branchId, bool activeOnly = true);
        Task<BanquetAddonService?> GetByIdAsync(int id);
        Task<int> CreateAsync(BanquetAddonService service);
        Task<bool> UpdateAsync(BanquetAddonService service);
        Task<bool> CodeExistsAsync(string code, int branchId, int? excludeId = null);
    }

    public interface IBanquetBookingRepository
    {
        Task<IEnumerable<BanquetBooking>> GetListAsync(int branchId, string? status, DateOnly? fromDate, DateOnly? toDate, int? venueId, string? customerType, int? b2bClientId);
        Task<BanquetBooking?> GetByIdAsync(int id);
        Task<BanquetBooking?> GetByNumberAsync(string bookingNumber);
        Task<int> CreateAsync(BanquetBooking booking, List<BanquetBookingPackageLine> packageLines, List<BanquetBookingAddonLine> addonLines, BanquetBookingPayment? advancePayment);
        Task<bool> UpdateStatusAsync(int id, string status, int updatedBy);
        Task<bool> UpdateFinancialsAsync(int id);
        Task<bool> RecalculateBalanceAsync(int id);
        /// <summary>Replaces all package lines for a booking, then recalculates all booking totals.</summary>
        Task<bool> UpdatePackageAsync(int bookingId, int? packageId, BanquetBookingPackageLine? packageLine, int updatedBy, string oldSummary);
        /// <summary>Appends a single addon line and recalculates booking totals.</summary>
        Task<bool> AddAddonLineAsync(int bookingId, BanquetBookingAddonLine addonLine, int updatedBy);
        /// <summary>Removes one addon line and recalculates booking totals.</summary>
        Task<bool> RemoveAddonLineAsync(int bookingId, int addonLineId, int updatedBy);
        /// <summary>Full recalculation of all booking amounts from child lines + payments.</summary>
        Task<bool> RecalcTotalsAsync(int bookingId, int updatedBy);
        Task AddAuditLogAsync(BanquetBookingAuditLog log);
        Task<IEnumerable<BanquetBookingPayment>> GetPaymentsAsync(int bookingId);
        Task<int> AddPaymentAsync(BanquetBookingPayment payment);
        Task<IEnumerable<BanquetBookingAuditLog>> GetAuditLogsAsync(int bookingId);
        Task<string> GenerateNextReceiptNumberAsync(int branchId);
        Task<IEnumerable<BanquetBooking>> GetCalendarEventsAsync(int branchId, DateOnly fromDate, DateOnly toDate, int? venueId = null);
        Task<BanquetBookingPayment?> GetPaymentByIdAsync(int paymentId);
        // Dashboard KPIs
        Task<BanquetDashboardKpi> GetDashboardKpiAsync(int branchId);
        // Guest lookup by phone (B2C auto-populate)
        Task<BanquetBooking?> GetLastB2CGuestByPhoneAsync(string phone, int branchId);
        // Invoice number (global INV/{FY}/{seq} series shared with hotel bookings)
        Task<string> GenerateInvoiceNumberAsync();
        Task SetInvoiceNumberAsync(int id, string invoiceNumber);
        Task<BanquetHeadWiseDue> GetHeadWiseDueAsync(int bookingId);
    }

    public interface IBanquetCancellationRepository
    {
        Task<BanquetCancellationPreview> GetPreviewAsync(int bookingId);
        Task<BanquetCancellation> CancelAsync(int bookingId, string reason, decimal flatDeduction, int cancelledBy);
        Task<string?> ProcessRefundAsync(int cancellationId, string paymentMethod, string reference, int processedBy, string refundNumber);
        Task<BanquetCancellation?> GetByBookingIdAsync(int bookingId);
    }

    // ── Supporting types ──────────────────────────────────────────────────────

    public class BanquetDashboardKpi
    {
        public int TodaysEvents { get; set; }
        public int UpcomingEvents7Days { get; set; }
        public int PendingConfirmations { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal ThisMonthGST { get; set; }
        public int ThisMonthBookings { get; set; }
        public decimal OutstandingBalance { get; set; }
        public List<BanquetBooking> TodaysEventList { get; set; } = new();
        public List<BanquetBooking> RecentBookings { get; set; } = new();
    }

    public class BanquetCancellationPreview
    {
        public int BanquetBookingId { get; set; }
        public string BanquetBookingNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal RefundPercent { get; set; }
        public decimal FlatDeduction { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public int DaysBeforeEvent { get; set; }
    }
}
