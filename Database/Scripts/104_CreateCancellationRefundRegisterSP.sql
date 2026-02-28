-- =============================================
-- Cancellation & Refund Register Report Stored Procedure
-- Description: Returns combined cancellation + refund register with type filter
-- Parameters:
--   @BranchID   INT        - Branch to filter
--   @FromDate   DATE       - Start of cancel date range
--   @ToDate     DATE       - End of cancel date range
--   @ReportType NVARCHAR   - 'All' | 'Cancelled' (pending/no refund) | 'Refunded'
-- Result Sets: 1) Summary  2) Detail rows
-- Created: 2026-02-28
-- =============================================

IF OBJECT_ID('sp_GetCancellationRefundRegister', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetCancellationRefundRegister;
GO

CREATE PROCEDURE sp_GetCancellationRefundRegister
    @BranchID   INT,
    @FromDate   DATE,
    @ToDate     DATE,
    @ReportType NVARCHAR(20) = 'All'   -- 'All' | 'Cancelled' | 'Refunded'
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalise date range
    IF (@FromDate IS NULL) SET @FromDate = CAST(GETDATE() AS DATE);
    IF (@ToDate   IS NULL) SET @ToDate   = CAST(GETDATE() AS DATE);
    IF (@ToDate < @FromDate)
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate   = @Tmp;
    END

    -- Normalise type filter
    IF (@ReportType IS NULL OR @ReportType NOT IN ('Cancelled','Refunded'))
        SET @ReportType = 'All';

    -- ── Result Set 1: Summary ──────────────────────────────────────────────
    SELECT
        COUNT(1)                                                                       AS TotalRecords,
        ISNULL(SUM(bc.AmountPaid), 0)                                                 AS TotalAmountPaid,
        ISNULL(SUM(bc.AmountPaid - bc.RefundAmount), 0)                               AS TotalDeducted,
        ISNULL(SUM(CASE WHEN ISNULL(bc.IsRefunded,0) = 0 AND bc.RefundAmount > 0
                        THEN bc.RefundAmount ELSE 0 END), 0)                          AS TotalRefundPending,
        ISNULL(SUM(CASE WHEN ISNULL(bc.IsRefunded,0) = 1
                        THEN bc.RefundAmount ELSE 0 END), 0)                          AS TotalRefunded,
        ISNULL(SUM(CASE WHEN ISNULL(bc.IsRefunded,0) = 1 THEN 1 ELSE 0 END), 0)      AS RefundedCount,
        ISNULL(SUM(CASE WHEN ISNULL(bc.IsRefunded,0) = 0 AND bc.RefundAmount > 0
                        THEN 1 ELSE 0 END), 0)                                        AS PendingRefundCount
    FROM BookingCancellations bc
    WHERE bc.BranchID = @BranchID
      AND CAST(bc.CancelRequestedAt AS DATE) BETWEEN @FromDate AND @ToDate
      AND (
            @ReportType = 'All'
            OR (@ReportType = 'Cancelled' AND ISNULL(bc.IsRefunded, 0) = 0)
            OR (@ReportType = 'Refunded'  AND ISNULL(bc.IsRefunded, 0) = 1)
          );

    -- ── Result Set 2: Detail ───────────────────────────────────────────────
    SELECT
        bc.CancelRequestedAt,
        bc.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName)   AS GuestName,
        b.PrimaryGuestPhone                                             AS GuestPhone,
        b.CheckInDate,
        b.CheckOutDate,
        b.Nights,
        rt.TypeName                                                     AS RoomType,
        r.RoomNumber,
        bc.AmountPaid,
        bc.AmountPaid - bc.RefundAmount                                AS DeductionAmount,
        bc.RefundAmount,
        bc.RefundPercent,
        ISNULL(bc.IsRefunded, 0)                                       AS IsRefunded,
        bc.RefundedAt,
        bc.RefundPaymentMethod,
        bc.RefundReference,
        ISNULL(bc.ApprovalStatus, 'None')                              AS ApprovalStatus,
        bc.Reason,
        COALESCE(
            u.FullName,
            NULLIF(LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))), ''),
            u.Username,
            ''
        )                                                               AS RequestedBy
    FROM BookingCancellations bc
    INNER JOIN Bookings    b  ON b.Id        = bc.BookingId
    LEFT JOIN  RoomTypes   rt ON rt.Id       = b.RoomTypeId
    LEFT JOIN  Rooms       r  ON r.Id        = b.RoomId
    LEFT JOIN  Users       u  ON u.Id        = bc.CancelRequestedBy
    WHERE bc.BranchID = @BranchID
      AND CAST(bc.CancelRequestedAt AS DATE) BETWEEN @FromDate AND @ToDate
      AND (
            @ReportType = 'All'
            OR (@ReportType = 'Cancelled' AND ISNULL(bc.IsRefunded, 0) = 0)
            OR (@ReportType = 'Refunded'  AND ISNULL(bc.IsRefunded, 0) = 1)
          )
    ORDER BY bc.CancelRequestedAt DESC, bc.Id DESC;

END;
GO

PRINT 'Stored Procedure sp_GetCancellationRefundRegister created successfully';
GO
