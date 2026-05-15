using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public class CreditNoteRepository : ICreditNoteRepository
{
    private readonly IDbConnection _db;

    public CreditNoteRepository(IDbConnection db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Create
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<(int CreditNoteId, string? CreditNoteNumber)> CreateAsync(
        int cancellationId,
        int? refundPaymentId,
        string? refundPaymentMethod,
        string? refundPaymentReference,
        int branchId,
        int performedBy)
    {
        try
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            // ── 1. Fetch header data from BookingCancellations + Bookings ───
            const string dataSql = @"
                SELECT
                    bc.Id               AS CancellationId,
                    bc.BookingId,
                    bc.BookingNumber,
                    bc.BranchID,
                    bc.RefundAmount,
                    bc.AmountPaid,
                    bc.DeductionAmount,
                    ISNULL(bc.Reason, '') AS CancellationReason,
                    bc.CreatedDate        AS CancellationDate,
                    ISNULL(bc.IsPartial, 0) AS IsPartial,
                    bc.CancelledRoomLineIds,

                    b.CustomerType,
                    b.PrimaryGuestFirstName,
                    b.PrimaryGuestLastName,
                    b.PrimaryGuestEmail,
                    b.PrimaryGuestPhone,
                    b.B2BClientName      AS CompanyName,
                    b.CompanyGstNo,
                    b.BillingAddress,
                    b.InvoiceNumber      AS OriginalInvoiceNumber,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Nights,
                    b.TaxAmount,
                    ISNULL(b.CGSTAmount, b.TaxAmount / 2) AS CGSTAmount,
                    ISNULL(b.SGSTAmount, b.TaxAmount / 2) AS SGSTAmount,

                    -- For entire cancellation: b.TotalAmount is still the original total
                    -- For partial cancellation: b.TotalAmount is the remaining (already reduced),
                    --   so we snapshot bc.AmountPaid + bc.DeductionAmount as the cancelled portion total
                    CASE WHEN ISNULL(bc.IsPartial, 0) = 1
                         THEN bc.AmountPaid + bc.DeductionAmount
                         ELSE b.TotalAmount
                    END                 AS OriginalTotalAmount,

                    -- Fallback room type from Bookings (used for B2C or single-room-type bookings)
                    rt.TypeName         AS PrimaryRoomType

                FROM BookingCancellations bc
                INNER JOIN Bookings b   ON b.Id  = bc.BookingId
                LEFT  JOIN RoomTypes rt ON rt.Id = b.RoomTypeId
                WHERE bc.Id = @CancellationId
                  AND bc.BranchID = @BranchId";

            var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
                dataSql, new { CancellationId = cancellationId, BranchId = branchId });

            if (row == null)
                return (0, null);

            // Prevent duplicate credit notes for the same cancellation
            var existing = await GetByCancellationIdAsync(cancellationId);
            if (existing != null)
                return (existing.Id, existing.CreditNoteNumber);

            // ── 2. Resolve room type names correctly ─────────────────────────
            bool isPartial     = Convert.ToBoolean(row.IsPartial);
            string customerType = (string?)row.CustomerType ?? "B2C";
            string? cancelledRoomLineIds = (string?)row.CancelledRoomLineIds;
            int bookingId = (int)row.BookingId;

            string resolvedRoomType = (string?)row.PrimaryRoomType ?? string.Empty;

            if (isPartial && !string.IsNullOrWhiteSpace(cancelledRoomLineIds))
            {
                // Partial: show only the cancelled room type(s)
                var lineIds = cancelledRoomLineIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .ToArray();

                if (lineIds.Length > 0)
                {
                    var names = await _db.QueryAsync<string>(
                        @"SELECT DISTINCT RoomTypeName
                          FROM B2BBookingRoomLines
                          WHERE Id IN @Ids AND BookingId = @BookingId",
                        new { Ids = lineIds, BookingId = bookingId });
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    if (nameList.Any())
                        resolvedRoomType = string.Join(" + ", nameList);
                }
            }
            else
            {
                // Entire cancellation — aggregate all room lines for this booking (B2B multi-room)
                var allNames = await _db.QueryAsync<string>(
                    @"SELECT DISTINCT RoomTypeName
                      FROM B2BBookingRoomLines
                      WHERE BookingId = @BookingId",
                    new { BookingId = bookingId });
                var allNameList = allNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                if (allNameList.Any())
                    resolvedRoomType = string.Join(" + ", allNameList);
                // else keep PrimaryRoomType (B2C or legacy booking)
            }

            // ── 3. Compute GST on the refund ────────────────────────────────
            // For partial: use bc.AmountPaid + bc.DeductionAmount as the cancelled-portion base
            // For entire: b.TotalAmount is the original total (unchanged by full cancel)
            decimal originalPortionTotal = Convert.ToDecimal(row.OriginalTotalAmount ?? 0m);
            decimal refundTotal = Convert.ToDecimal(row.RefundAmount ?? 0m);
            decimal amountPaid  = Convert.ToDecimal(row.AmountPaid ?? 0m);
            decimal deduction   = Convert.ToDecimal(row.DeductionAmount ?? 0m);

            decimal cgstFull, sgstFull;

            if (isPartial && !string.IsNullOrWhiteSpace(cancelledRoomLineIds))
            {
                // For partial cancellations: b.CGSTAmount/SGSTAmount has already been reduced
                // to reflect only the remaining active lines — NOT the cancelled ones.
                // So we must source GST directly from the cancelled B2BBookingRoomLines.
                var lineIds = cancelledRoomLineIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .ToArray();

                if (lineIds.Length > 0)
                {
                    var cancelledLineTax = await _db.QueryFirstOrDefaultAsync<decimal>(
                        "SELECT ISNULL(SUM(TaxAmount), 0) FROM B2BBookingRoomLines WHERE Id IN @Ids AND BookingId = @BookingId",
                        new { Ids = lineIds, BookingId = bookingId });
                    cgstFull = Math.Round(cancelledLineTax / 2m, 2, MidpointRounding.AwayFromZero);
                    sgstFull = cancelledLineTax - cgstFull;
                }
                else
                {
                    cgstFull = Convert.ToDecimal(row.CGSTAmount ?? 0m);
                    sgstFull = Convert.ToDecimal(row.SGSTAmount ?? 0m);
                }
            }
            else
            {
                // Entire cancellation: booking-level CGST/SGST is still the original total
                cgstFull = Convert.ToDecimal(row.CGSTAmount ?? 0m);
                sgstFull = Convert.ToDecimal(row.SGSTAmount ?? 0m);
            }

            // Original full-booking total for GST proportion
            decimal fullBookingTotal = originalPortionTotal;  // cancelled-portion amount (tax-inclusive)

            decimal refundCgst, refundSgst;
            if (fullBookingTotal > 0 && (cgstFull + sgstFull) > 0)
            {
                // Proportion: how much of the eligible GST is being refunded
                var totalCancelledTax = cgstFull + sgstFull;
                var totalCancelledBase = fullBookingTotal - totalCancelledTax;
                var refundBase0 = totalCancelledBase > 0
                    ? Math.Round(refundTotal * (totalCancelledBase / fullBookingTotal), 2, MidpointRounding.AwayFromZero)
                    : refundTotal;
                var refundTax0 = refundTotal - refundBase0;
                refundCgst = Math.Round(refundTax0 / 2m, 2, MidpointRounding.AwayFromZero);
                refundSgst = refundTax0 - refundCgst;
            }
            else if (fullBookingTotal > 0)
            {
                // No GST applicable for this room type (e.g. exempt room)
                refundCgst = 0m;
                refundSgst = 0m;
            }
            else
            {
                // Fallback: use standard 6% CGST + 6% SGST on base
                decimal baseApprox = Math.Round(refundTotal / 1.12m, 2);
                refundCgst = Math.Round(baseApprox * 0.06m, 2);
                refundSgst = refundCgst;
            }

            decimal refundBase = Math.Round(refundTotal - refundCgst - refundSgst, 2, MidpointRounding.AwayFromZero);

            int? performedByNullable = performedBy > 0 ? (int?)performedBy : null;
            string guestName = $"{row.PrimaryGuestFirstName} {row.PrimaryGuestLastName}".Trim();

            // ── 4. Insert ────────────────────────────────────────────────────
            const string insertSql = @"
                INSERT INTO CreditNotes (
                    CreditNoteNumber, BookingId, BookingNumber, CancellationId, RefundPaymentId,
                    BranchID, CustomerType,
                    GuestName, GuestEmail, GuestPhone, CompanyName, CompanyGstNo, BillingAddress,
                    OriginalInvoiceNumber, CheckInDate, CheckOutDate, Nights, RoomType,
                    OriginalTotalAmount, RefundBaseAmount, RefundCGSTAmount, RefundSGSTAmount, RefundTotalAmount,
                    CancellationReason, CancellationDate,
                    RefundPaymentMethod, RefundPaymentReference,
                    GeneratedAt, GeneratedBy, IsActive
                )
                VALUES (
                    @CreditNoteNumber, @BookingId, @BookingNumber, @CancellationId, @RefundPaymentId,
                    @BranchID, @CustomerType,
                    @GuestName, @GuestEmail, @GuestPhone, @CompanyName, @CompanyGstNo, @BillingAddress,
                    @OriginalInvoiceNumber, @CheckInDate, @CheckOutDate, @Nights, @RoomType,
                    @OriginalTotalAmount, @RefundBaseAmount, @RefundCGSTAmount, @RefundSGSTAmount, @RefundTotalAmount,
                    @CancellationReason, @CancellationDate,
                    @RefundPaymentMethod, @RefundPaymentReference,
                    GETDATE(), @GeneratedBy, 1
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var creditNoteNumber = await GenerateCreditNoteNumberAsync();

            int newId = await _db.ExecuteScalarAsync<int>(insertSql, new
            {
                CreditNoteNumber      = creditNoteNumber,
                BookingId             = bookingId,
                BookingNumber         = (string)row.BookingNumber,
                CancellationId        = cancellationId,
                RefundPaymentId       = refundPaymentId,
                BranchID              = branchId,
                CustomerType          = customerType,
                GuestName             = guestName,
                GuestEmail            = (string?)row.PrimaryGuestEmail,
                GuestPhone            = (string?)row.PrimaryGuestPhone,
                CompanyName           = (string?)row.CompanyName,
                CompanyGstNo          = (string?)row.CompanyGstNo,
                BillingAddress        = (string?)row.BillingAddress,
                OriginalInvoiceNumber = (string?)row.OriginalInvoiceNumber,
                CheckInDate           = (DateTime)row.CheckInDate,
                CheckOutDate          = (DateTime)row.CheckOutDate,
                Nights                = (int)row.Nights,
                RoomType              = resolvedRoomType,
                OriginalTotalAmount   = originalPortionTotal,
                RefundBaseAmount      = refundBase,
                RefundCGSTAmount      = refundCgst,
                RefundSGSTAmount      = refundSgst,
                RefundTotalAmount     = refundTotal,
                CancellationReason    = (string?)row.CancellationReason,
                CancellationDate      = (DateTime)row.CancellationDate,
                RefundPaymentMethod   = refundPaymentMethod,
                RefundPaymentReference = refundPaymentReference,
                GeneratedBy           = performedByNullable
            });

            return (newId, creditNoteNumber);
        }
        catch
        {
            return (0, null);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fetch
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<CreditNote?> GetByIdAsync(int id)
    {
        return await _db.QueryFirstOrDefaultAsync<CreditNote>(
            "SELECT * FROM CreditNotes WHERE Id = @Id AND IsActive = 1",
            new { Id = id });
    }

    public async Task<CreditNote?> GetByCancellationIdAsync(int cancellationId)
    {
        return await _db.QueryFirstOrDefaultAsync<CreditNote>(
            "SELECT TOP 1 * FROM CreditNotes WHERE CancellationId = @CancellationId AND IsActive = 1",
            new { CancellationId = cancellationId });
    }

    public async Task<IEnumerable<CreditNote>> GetByDateRangeAsync(int branchId, DateTime from, DateTime to)
    {
        return await _db.QueryAsync<CreditNote>(@"
            SELECT * FROM CreditNotes
            WHERE BranchID = @BranchId
              AND IsActive  = 1
              AND CAST(GeneratedAt AS DATE) BETWEEN @From AND @To
            ORDER BY GeneratedAt DESC",
            new { BranchId = branchId, From = from.Date, To = to.Date });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Credit note number generation  →  CR-FYFY-NNNNN  (e.g. CR-2627-00001)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<string> GenerateCreditNoteNumberAsync()
    {
        var now = DateTime.Today;
        int fyStart = now.Month >= 4 ? now.Year : now.Year - 1;
        int fyEnd   = fyStart + 1;
        // Short FY key: "2627" for 2026-27
        var fy = $"{fyStart % 100:D2}{fyEnd % 100:D2}";

        const string upsertSql = @"
            DECLARE @NextSeq INT;
            DECLARE @MaxCnSeq INT = ISNULL((
                SELECT MAX(CAST(RIGHT(CreditNoteNumber, 5) AS INT))
                FROM CreditNotes
                WHERE CreditNoteNumber LIKE 'CR-' + @FY + '-%'
            ), 0);

            IF NOT EXISTS (SELECT 1 FROM CreditNoteSequence WHERE FinancialYear = @FY AND BranchID = 0)
            BEGIN
                INSERT INTO CreditNoteSequence (FinancialYear, BranchID, LastSequence)
                VALUES (@FY, 0, @MaxCnSeq + 1);
                SET @NextSeq = @MaxCnSeq + 1;
            END
            ELSE
            BEGIN
                UPDATE CreditNoteSequence WITH (HOLDLOCK)
                SET LastSequence = CASE
                    WHEN LastSequence <= @MaxCnSeq THEN @MaxCnSeq + 1
                    ELSE LastSequence + 1
                END
                WHERE FinancialYear = @FY AND BranchID = 0;

                SELECT @NextSeq = LastSequence
                FROM CreditNoteSequence
                WHERE FinancialYear = @FY AND BranchID = 0;
            END

            SELECT @NextSeq;";

        var seq = await _db.ExecuteScalarAsync<int>(upsertSql, new { FY = fy });
        return $"CR-{fy}-{seq:D5}";
    }
}
