-- =============================================
-- Cancellation Register Report Stored Procedure
-- Description: Returns cancellations/refund register for a date range (branch-wise)
-- =============================================

IF OBJECT_ID('sp_GetCancellationRegister', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetCancellationRegister;
GO

CREATE PROCEDURE sp_GetCancellationRegister
    @BranchID INT,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF (@FromDate IS NULL) SET @FromDate = CAST(GETDATE() AS DATE);
    IF (@ToDate IS NULL) SET @ToDate = CAST(GETDATE() AS DATE);

    IF (@ToDate < @FromDate)
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate = @Tmp;
    END

    -- Summary
    SELECT
        COUNT(1) AS TotalCancellations,
        ISNULL(SUM(bc.AmountPaid), 0) AS TotalPaid,
        ISNULL(SUM(bc.RefundAmount), 0) AS TotalRefund,
        ISNULL(SUM(CASE WHEN bc.ApprovalStatus = 'Pending' THEN 1 ELSE 0 END), 0) AS PendingApprovals
    FROM BookingCancellations bc
    WHERE bc.BranchID = @BranchID
      AND CAST(bc.CancelRequestedAt AS DATE) BETWEEN @FromDate AND @ToDate;

    -- Details
    SELECT
        bc.CancelRequestedAt,
        bc.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
        b.PrimaryGuestPhone AS GuestPhone,
        b.CheckInDate,
        b.CheckOutDate,
        bc.RateType,
        bc.CancellationType,
        bc.AmountPaid,
        bc.RefundPercent,
        bc.FlatDeduction,
        bc.DeductionAmount,
        bc.RefundAmount,
        bc.ApprovalStatus,
        bc.Reason,
        bc.IsOverride,
        bc.OverrideReason,
        COALESCE(u.FullName, CONCAT(u.FirstName, ' ', u.LastName), u.Username, '') AS RequestedBy
    FROM BookingCancellations bc
    INNER JOIN Bookings b ON b.Id = bc.BookingId
    LEFT JOIN Users u ON u.Id = bc.CancelRequestedBy
    WHERE bc.BranchID = @BranchID
      AND CAST(bc.CancelRequestedAt AS DATE) BETWEEN @FromDate AND @ToDate
    ORDER BY bc.CancelRequestedAt DESC, bc.Id DESC;
END;
GO

PRINT 'Stored Procedure sp_GetCancellationRegister created successfully';
GO
