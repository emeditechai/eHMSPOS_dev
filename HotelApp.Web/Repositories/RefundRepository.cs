using System.Data;
using Dapper;
using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories;

public class RefundRepository : IRefundRepository
{
    private readonly IDbConnection _db;

    public RefundRepository(IDbConnection db)
    {
        _db = db;
    }

    private static List<int> ParseLineIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new List<int>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private async Task<(decimal TotalAmount, decimal TaxAmount, decimal CGSTAmount, decimal SGSTAmount)> ResolveCancellationTaxReferenceAsync(
        int bookingId,
        bool isPartial,
        string? cancelledRoomLineIds,
        decimal fallbackTotal,
        decimal fallbackTax,
        decimal fallbackCgst,
        decimal fallbackSgst,
        IDbTransaction? tx = null)
    {
        if (isPartial)
        {
            var ids = ParseLineIds(cancelledRoomLineIds);
            if (ids.Count > 0)
            {
                var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT
                        ISNULL(SUM(GrandTotal), 0) AS TotalAmount,
                        ISNULL(SUM(TaxAmount), 0) AS TaxAmount
                      FROM B2BBookingRoomLines
                      WHERE BookingId = @BookingId
                        AND Id IN @Ids",
                    new { BookingId = bookingId, Ids = ids },
                    tx);

                var lineTotal = row?.TotalAmount is decimal d1 ? d1 : Convert.ToDecimal(row?.TotalAmount ?? 0m);
                var lineTax = row?.TaxAmount is decimal d2 ? d2 : Convert.ToDecimal(row?.TaxAmount ?? 0m);

                if (lineTotal > 0m)
                {
                    var lineCgst = Math.Round(lineTax / 2m, 2, MidpointRounding.AwayFromZero);
                    var lineSgst = lineTax - lineCgst;
                    return (lineTotal, lineTax, lineCgst, lineSgst);
                }
            }
        }

        return (fallbackTotal, fallbackTax, fallbackCgst, fallbackSgst);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // List all pending refunds for the branch
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<RefundListItem>> GetPendingRefundsAsync(int branchId)
    {
        const string sql = @"
            SELECT
                bc.Id                                                       AS CancellationId,
                bc.BookingId,
                bc.BookingNumber,
                CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
                b.PrimaryGuestPhone                                         AS GuestPhone,
                rt.TypeName                                                 AS RoomType,
                bc.AmountPaid,
                bc.RefundAmount,
                bc.AmountPaid - bc.RefundAmount                              AS DeductionAmount,
                bc.RefundPercent,
                ISNULL(bc.ApprovalStatus, 'None')                           AS ApprovalStatus,
                bc.CreatedDate                                              AS CancelledOn,
                bc.Reason,
                DATEDIFF(DAY, bc.CreatedDate, GETDATE())                    AS DaysSinceCancellation
            FROM BookingCancellations bc
            INNER JOIN Bookings b ON b.Id = bc.BookingId
            LEFT JOIN RoomTypes rt ON rt.Id = b.RoomTypeId
            WHERE bc.BranchID = @BranchId
              AND bc.RefundAmount > 0
              AND ISNULL(bc.IsRefunded, 0) = 0
            ORDER BY bc.CreatedDate ASC";

        return await _db.QueryAsync<RefundListItem>(sql, new { BranchId = branchId });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pending refunds filtered by cancellation date range
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<RefundListItem>> GetPendingRefundsByDateAsync(int branchId, DateTime fromDate, DateTime toDate)
    {
        const string sql = @"
            SELECT
                bc.Id                                                       AS CancellationId,
                bc.BookingId,
                bc.BookingNumber,
                CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
                b.PrimaryGuestPhone                                         AS GuestPhone,
                rt.TypeName                                                 AS RoomType,
                bc.AmountPaid,
                bc.RefundAmount,
                bc.AmountPaid - bc.RefundAmount                              AS DeductionAmount,
                bc.RefundPercent,
                ISNULL(bc.ApprovalStatus, 'None')                           AS ApprovalStatus,
                bc.CreatedDate                                              AS CancelledOn,
                bc.Reason,
                DATEDIFF(DAY, bc.CreatedDate, GETDATE())                    AS DaysSinceCancellation
            FROM BookingCancellations bc
            INNER JOIN Bookings b ON b.Id = bc.BookingId
            LEFT JOIN RoomTypes rt ON rt.Id = b.RoomTypeId
            WHERE bc.BranchID = @BranchId
              AND bc.RefundAmount > 0
              AND ISNULL(bc.IsRefunded, 0) = 0
              AND CAST(bc.CreatedDate AS DATE) BETWEEN @FromDate AND @ToDate
            ORDER BY bc.CreatedDate ASC";

        return await _db.QueryAsync<RefundListItem>(sql,
            new { BranchId = branchId, FromDate = fromDate.Date, ToDate = toDate.Date });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Total of already-refunded transactions in a date range (by RefundedAt)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<(decimal TotalRefunded, int RefundedCount)> GetCompletedRefundsTotalAsync(
        int branchId, DateTime fromDate, DateTime toDate)
    {
        const string sql = @"
            SELECT
                ISNULL(SUM(bc.RefundAmount), 0) AS TotalRefunded,
                COUNT(*)                        AS RefundedCount
            FROM BookingCancellations bc
            WHERE bc.BranchID = @BranchId
              AND ISNULL(bc.IsRefunded, 0) = 1
              AND CAST(ISNULL(bc.RefundedAt, bc.CreatedDate) AS DATE) BETWEEN @FromDate AND @ToDate";

        var row = await _db.QueryFirstAsync<(decimal TotalRefunded, int RefundedCount)>(
            sql, new { BranchId = branchId, FromDate = fromDate.Date, ToDate = toDate.Date });
        return row;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Full detail for a single pending refund
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<RefundDetailViewModel?> GetRefundDetailAsync(int cancellationId, int branchId)
    {
        const string sql = @"
            SELECT
                bc.Id                                                           AS CancellationId,
                bc.BookingId,
                ISNULL(bc.IsPartial, 0)                                        AS IsPartial,
                bc.CancelledRoomLineIds,
                bc.BookingNumber,
                CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)   AS GuestName,
                b.PrimaryGuestPhone                                             AS GuestPhone,
                b.PrimaryGuestEmail                                             AS GuestEmail,
                rt.TypeName                                                     AS RoomType,
                r.RoomNumber,
                b.CheckInDate,
                b.CheckOutDate,
                b.Nights,
                b.TotalAmount                                                   AS BookingTotalAmount,
                b.TaxAmount                                                     AS BookingTaxAmount,
                ISNULL(b.CGSTAmount, b.TaxAmount / 2)                           AS BookingCGSTAmount,
                b.BaseAmount                                                    AS BookingBaseAmount,
                bc.AmountPaid,
                bc.RefundPercent,
                bc.AmountPaid - bc.RefundAmount                                  AS DeductionAmount,
                bc.RefundAmount,
                ISNULL(bc.ApprovalStatus, 'None')                               AS ApprovalStatus,
                bc.Reason,
                ISNULL(bc.IsOverride, 0)                                        AS IsOverride,
                bc.OverrideReason,
                bc.CreatedDate                                                  AS CancelledOn,
                bc.HoursBeforeCheckIn
            FROM BookingCancellations bc
            INNER JOIN Bookings b ON b.Id = bc.BookingId
            LEFT JOIN RoomTypes rt ON rt.Id = b.RoomTypeId
            LEFT JOIN Rooms r ON r.Id = b.RoomId
            WHERE bc.Id = @CancellationId
              AND bc.BranchID = @BranchId
              AND bc.RefundAmount > 0
              AND ISNULL(bc.IsRefunded, 0) = 0";

        var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
            sql, new { CancellationId = cancellationId, BranchId = branchId });

        if (row == null)
            return null;

        var detail = new RefundDetailViewModel
        {
            CancellationId = row.CancellationId,
            BookingId = row.BookingId,
            BookingNumber = row.BookingNumber,
            GuestName = row.GuestName,
            GuestPhone = row.GuestPhone,
            GuestEmail = row.GuestEmail,
            RoomType = row.RoomType,
            RoomNumber = row.RoomNumber,
            CheckInDate = row.CheckInDate,
            CheckOutDate = row.CheckOutDate,
            Nights = row.Nights,
            BookingTotalAmount = row.BookingTotalAmount,
            BookingTaxAmount = row.BookingTaxAmount,
            BookingCGSTAmount = row.BookingCGSTAmount,
            BookingBaseAmount = row.BookingBaseAmount,
            AmountPaid = row.AmountPaid,
            RefundPercent = row.RefundPercent,
            DeductionAmount = row.DeductionAmount,
            RefundAmount = row.RefundAmount,
            ApprovalStatus = row.ApprovalStatus,
            Reason = row.Reason,
            IsOverride = row.IsOverride,
            OverrideReason = row.OverrideReason,
            CancelledOn = row.CancelledOn,
            HoursBeforeCheckIn = row.HoursBeforeCheckIn
        };

        bool isPartial = row.IsPartial is bool b && b;
        string? cancelledLineIds = row.CancelledRoomLineIds as string;

        var taxRef = await ResolveCancellationTaxReferenceAsync(
            detail.BookingId,
            isPartial,
            cancelledLineIds,
            detail.BookingTotalAmount,
            detail.BookingTaxAmount,
            detail.BookingCGSTAmount,
            detail.BookingTaxAmount - detail.BookingCGSTAmount,
            tx: null);

        var referenceTotal = taxRef.TotalAmount;
        var referenceTax = taxRef.TaxAmount;
        var referenceCgst = taxRef.CGSTAmount;
        var referenceSgst = taxRef.SGSTAmount;

        // Surface the reference block used for this cancellation in the detail card.
        detail.BookingTotalAmount = referenceTotal;
        detail.BookingTaxAmount = referenceTax;
        detail.BookingCGSTAmount = referenceCgst;
        detail.BookingBaseAmount = Math.Round(referenceTotal - referenceTax, 2, MidpointRounding.AwayFromZero);

        // Calculate proportional GST on refund amount (credit-note reference values)
        if (referenceTotal > 0m)
        {
            var fraction = detail.RefundAmount / referenceTotal;
            detail.RefundCGSTAmount = Math.Round(referenceCgst * fraction, 2, MidpointRounding.AwayFromZero);
            detail.RefundSGSTAmount = Math.Round(referenceSgst * fraction, 2, MidpointRounding.AwayFromZero);
            detail.RefundBaseAmount = Math.Round(detail.RefundAmount - detail.RefundCGSTAmount - detail.RefundSGSTAmount, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            detail.RefundBaseAmount = detail.RefundAmount;
        }

        return detail;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Process a refund:
    //  1. Insert negative BookingPayment  (IsRefund = 1, Amount = -RefundAmount)
    //  2. Mark BookingCancellations as refunded
    //  3. Audit log
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ProcessRefundResult> ProcessRefundAsync(
        ProcessRefundRequest request, int branchId, int performedBy)
    {
        if (_db.State != ConnectionState.Open)
            _db.Open();

        using var tx = _db.BeginTransaction();
        try
        {
            int? performedByNullable = performedBy > 0 ? (int?)performedBy : null;

            // Validate cancellation record
            const string getCancSql = @"
                SELECT
                    bc.Id, bc.BookingId, bc.BookingNumber, bc.BranchID,
                    ISNULL(bc.IsPartial, 0) AS IsPartial,
                    bc.CancelledRoomLineIds,
                    bc.RefundAmount, ISNULL(bc.IsRefunded, 0) AS IsRefunded,
                    bc.AmountPaid, bc.DeductionAmount,
                    b.TotalAmount, b.TaxAmount,
                    ISNULL(b.CGSTAmount, b.TaxAmount / 2) AS CGSTAmount,
                    ISNULL(b.SGSTAmount, b.TaxAmount / 2) AS SGSTAmount
                FROM BookingCancellations bc
                INNER JOIN Bookings b ON b.Id = bc.BookingId
                WHERE bc.Id = @CancellationId
                  AND bc.BranchID = @BranchId";

            var canc = await _db.QueryFirstOrDefaultAsync<dynamic>(
                getCancSql,
                new { CancellationId = request.CancellationId, BranchId = branchId },
                tx);

            if (canc == null)
                return Fail("Cancellation record not found.");

            if ((bool)canc.IsRefunded)
                return Fail("This refund has already been processed.");

            // Must be approved before processing
            const string approvalCheckSql = @"
                SELECT ISNULL(ApprovalStatus,'None') FROM BookingCancellations WHERE Id = @Id";
            var approvalStatus = await _db.ExecuteScalarAsync<string>(
                approvalCheckSql, new { Id = request.CancellationId }, tx);
            if (approvalStatus != "Approved")
                return Fail("Refund has not been approved yet. Please get manager approval first.");

            decimal refundAmount = (decimal)canc.RefundAmount;
            if (refundAmount <= 0)
                return Fail("Eligible refund amount is ₹0. No refund to process.");

            string bookingNumber = (string)canc.BookingNumber;
            int bookingId = (int)canc.BookingId;

            bool isPartial = canc.IsPartial is bool bp && bp;
            string? cancelledLineIds = canc.CancelledRoomLineIds as string;

            var fallbackTotal = (decimal)canc.TotalAmount;
            var fallbackTax = (decimal)canc.TaxAmount;
            var fallbackCgst = (decimal)canc.CGSTAmount;
            var fallbackSgst = (decimal)canc.SGSTAmount;

            var taxRef = await ResolveCancellationTaxReferenceAsync(
                bookingId,
                isPartial,
                cancelledLineIds,
                fallbackTotal,
                fallbackTax,
                fallbackCgst,
                fallbackSgst,
                tx);

            // Calculate proportional GST (credit note reference values)
            decimal totalAmt = taxRef.TotalAmount;
            decimal taxAmt = taxRef.TaxAmount;
            decimal cgstAmt = taxRef.CGSTAmount;
            decimal sgstAmt = taxRef.SGSTAmount;

            decimal cgstOnRefund = 0m, sgstOnRefund = 0m, baseOnRefund = 0m;
            if (totalAmt > 0m)
            {
                var ratio = refundAmount / totalAmt;
                cgstOnRefund = Math.Round(cgstAmt * ratio, 2, MidpointRounding.AwayFromZero);
                sgstOnRefund = Math.Round(sgstAmt * ratio, 2, MidpointRounding.AwayFromZero);
                baseOnRefund = Math.Round(refundAmount - cgstOnRefund - sgstOnRefund, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                baseOnRefund = refundAmount;
            }

            var gstNote = cgstOnRefund > 0
                ? $" | GST Credit: Base ₹{baseOnRefund:N2}, CGST ₹{cgstOnRefund:N2}, SGST ₹{sgstOnRefund:N2}"
                : string.Empty;

            var notes = $"REFUND for cancellation of booking {bookingNumber}." +
                        (string.IsNullOrWhiteSpace(request.Remarks) ? string.Empty : $" Remarks: {request.Remarks.Trim()}") +
                        gstNote;

            // Generate receipt number
            var receiptNumber = await GenerateReceiptNumberAsync(tx);

            // Insert negative payment entry (refund outflow)
            const string insertPaymentSql = @"
                INSERT INTO BookingPayments (
                    BookingId, ReceiptNumber,
                    Amount,
                    PaymentMethod, PaymentReference, Status, PaidOn, Notes,
                    CardType, CardLastFourDigits, BankId, ChequeDate,
                    CreatedBy, IsAdvancePayment, IsRefund
                )
                VALUES (
                    @BookingId, @ReceiptNumber,
                    @Amount,
                    @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes,
                    @CardType, @CardLastFourDigits, @BankId, @ChequeDate,
                    @CreatedBy, 0, 1
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newPaymentId;
            try
            {
                newPaymentId = await _db.ExecuteScalarAsync<int>(
                    insertPaymentSql,
                    new
                    {
                        BookingId = bookingId,
                        ReceiptNumber = receiptNumber,
                        Amount = -refundAmount,        // negative = outflow
                        PaymentMethod = request.PaymentMethod,
                        PaymentReference = request.PaymentReference?.Trim(),
                        Status = "Refunded",
                        PaidOn = DateTime.Now,
                        Notes = notes,
                        CardType = request.CardType,
                        CardLastFourDigits = request.CardLastFourDigits,
                        BankId = request.BankId,
                        ChequeDate = request.ChequeDate,
                        CreatedBy = performedByNullable
                    },
                    tx);
            }
            catch (Exception ex) when (
                ex.Message.Contains("IsRefund", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
            {
                // DB hasn't run migration 102 yet — fall back without IsRefund
                const string legacySql = @"
                    INSERT INTO BookingPayments (
                        BookingId, ReceiptNumber, Amount,
                        PaymentMethod, PaymentReference, Status, PaidOn, Notes,
                        CardType, CardLastFourDigits, BankId, ChequeDate,
                        CreatedBy, IsAdvancePayment
                    )
                    VALUES (
                        @BookingId, @ReceiptNumber, @Amount,
                        @PaymentMethod, @PaymentReference, @Status, @PaidOn, @Notes,
                        @CardType, @CardLastFourDigits, @BankId, @ChequeDate,
                        @CreatedBy, 0
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                newPaymentId = await _db.ExecuteScalarAsync<int>(
                    legacySql,
                    new
                    {
                        BookingId = bookingId,
                        ReceiptNumber = receiptNumber,
                        Amount = -refundAmount,
                        PaymentMethod = request.PaymentMethod,
                        PaymentReference = request.PaymentReference?.Trim(),
                        Status = "Refunded",
                        PaidOn = DateTime.Now,
                        Notes = notes,
                        CardType = request.CardType,
                        CardLastFourDigits = request.CardLastFourDigits,
                        BankId = request.BankId,
                        ChequeDate = request.ChequeDate,
                        CreatedBy = performedByNullable
                    },
                    tx);
            }

            // Mark cancellation as refunded (graceful if columns missing)
            try
            {
                await _db.ExecuteAsync(@"
                    UPDATE BookingCancellations
                    SET IsRefunded          = 1,
                        RefundedAt          = GETDATE(),
                        RefundedBy          = @RefundedBy,
                        RefundPaymentId     = @PaymentId,
                        RefundPaymentMethod = @PaymentMethod,
                        RefundReference     = @Reference,
                        RefundRemarks       = @Remarks
                    WHERE Id = @Id",
                    new
                    {
                        Id = request.CancellationId,
                        RefundedBy = performedByNullable,
                        PaymentId = newPaymentId,
                        PaymentMethod = request.PaymentMethod,
                        Reference = request.PaymentReference?.Trim(),
                        Remarks = request.Remarks?.Trim()
                    },
                    tx);
            }
            catch
            {
                // Best-effort — at minimum the IsRefunded flag
                try
                {
                    await _db.ExecuteAsync(
                        "UPDATE BookingCancellations SET IsRefunded = 1, RefundedAt = GETDATE() WHERE Id = @Id",
                        new { Id = request.CancellationId },
                        tx);
                }
                catch { /* swallow */ }
            }

            // Audit log
            const string auditSql = @"
                INSERT INTO BookingAuditLog
                    (BookingId, BookingNumber, ActionType, ActionDescription, OldValue, NewValue, PerformedBy)
                VALUES
                    (@BookingId, @BookingNumber, @ActionType, @Description, @OldValue, @NewValue, @PerformedBy)";

            await _db.ExecuteAsync(auditSql, new
            {
                BookingId = bookingId,
                BookingNumber = bookingNumber,
                ActionType = "Refunded",
                Description = $"Refund of ₹{refundAmount:N2} processed via {request.PaymentMethod}. Receipt: {receiptNumber}",
                OldValue = "Pending Refund",
                NewValue = $"Refunded ₹{refundAmount:N2}",
                PerformedBy = performedByNullable
            }, tx);

            tx.Commit();

            return new ProcessRefundResult
            {
                Success = true,
                Message = $"Refund of ₹{refundAmount:N2} processed successfully via {request.PaymentMethod}.",
                RefundAmount = refundAmount,
                ReceiptNumber = receiptNumber
            };
        }
        catch (Exception ex)
        {
            try { tx.Rollback(); } catch { /* best-effort */ }
            return Fail(ex.GetBaseException().Message);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Approve a refund (sets ApprovalStatus = 'Approved')
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> ApproveRefundAsync(
        int cancellationId, int branchId, int approvedBy)
    {
        const string checkSql = @"
            SELECT ISNULL(ApprovalStatus,'None') AS ApprovalStatus,
                   ISNULL(IsRefunded,0)          AS IsRefunded
            FROM BookingCancellations
            WHERE Id = @Id AND BranchID = @BranchId";

        var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
            checkSql, new { Id = cancellationId, BranchId = branchId });

        if (row == null)
            return (false, "Refund record not found.");
        if ((bool)row.IsRefunded)
            return (false, "This refund has already been processed.");
        if ((string)row.ApprovalStatus == "Approved")
            return (false, "Refund has already been approved.");

        int? approvedByNullable = approvedBy > 0 ? (int?)approvedBy : null;

        await _db.ExecuteAsync(@"
            UPDATE BookingCancellations
            SET ApprovalStatus = 'Approved',
                ApprovedBy     = @ApprovedBy,
                ApprovedAt     = GETDATE()
            WHERE Id = @Id AND BranchID = @BranchId",
            new { Id = cancellationId, BranchId = branchId, ApprovedBy = approvedByNullable });

        return (true, "Refund approved successfully.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private static ProcessRefundResult Fail(string message) =>
        new() { Success = false, Message = message };

    private async Task<string> GenerateReceiptNumberAsync(IDbTransaction tx)
    {
        try
        {
            var prefix = $"RFD-{DateTime.Now:yyyyMMdd}-";
            const string sql = @"
                SELECT ISNULL(MAX(CAST(SUBSTRING(ReceiptNumber,
                    CHARINDEX('-', ReceiptNumber, CHARINDEX('-', ReceiptNumber) + 1) + 1,
                    LEN(ReceiptNumber)) AS INT)), 0) + 1
                FROM BookingPayments
                WHERE ReceiptNumber LIKE @Prefix + '%'
                  AND TRY_CAST(SUBSTRING(ReceiptNumber,
                      CHARINDEX('-', ReceiptNumber, CHARINDEX('-', ReceiptNumber) + 1) + 1,
                      LEN(ReceiptNumber)) AS INT) IS NOT NULL";

            var seq = await _db.ExecuteScalarAsync<int>(sql, new { Prefix = prefix }, tx);
            return $"{prefix}{seq:D4}";
        }
        catch
        {
            return $"RFD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }
}
