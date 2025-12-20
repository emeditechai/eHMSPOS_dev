-- =============================================
-- Daily Collection Register Report Stored Procedure
-- Description: Returns payment collections for a date range (branch-wise)
-- Notes:
--  - Uses BookingPayments.PaidOn as the collection date
--  - Includes all payments; summary uses successful statuses
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetDailyCollectionRegister', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetDailyCollectionRegister;
GO

CREATE PROCEDURE sp_GetDailyCollectionRegister
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
        COUNT(1) AS TotalReceipts,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount ELSE 0 END), 0) AS TotalCollected,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN (bp.Amount * b.TaxAmount / NULLIF(b.TotalAmount, 0)) ELSE 0 END), 0) AS TotalGST,
        ISNULL(SUM(ISNULL(bp.DiscountAmount, 0)), 0) AS TotalDiscount,
        ISNULL(SUM(CASE WHEN ISNULL(bp.IsRoundOffApplied, 0) = 1 THEN ISNULL(bp.RoundOffAmount, 0) ELSE 0 END), 0) AS TotalRoundOff
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;

    -- Daily totals (for cards/table)
    SELECT
        CAST(bp.PaidOn AS DATE) AS CollectionDate,
        COUNT(1) AS ReceiptCount,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount ELSE 0 END), 0) AS CollectedAmount,
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN (bp.Amount * b.TaxAmount / NULLIF(b.TotalAmount, 0)) ELSE 0 END), 0) AS GSTAmount
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY CollectionDate;

    -- Details
    SELECT
        CAST(bp.PaidOn AS DATE) AS CollectionDate,
        bp.PaidOn,
        bp.ReceiptNumber,
        b.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
        b.PrimaryGuestPhone AS GuestPhone,
        rt.TypeName AS RoomType,
        bp.PaymentMethod,
        bk.BankName,
        bp.PaymentReference,
        bp.Amount AS ReceiptAmount,
        ISNULL(bp.DiscountAmount, 0) AS DiscountAmount,
        ISNULL(bp.DiscountPercent, 0) AS DiscountPercent,
        ISNULL(bp.RoundOffAmount, 0) AS RoundOffAmount,
        ISNULL(bp.IsRoundOffApplied, 0) AS IsRoundOffApplied,
        bp.Status,
        COALESCE(u.FullName, CONCAT(u.FirstName, ' ', u.LastName), u.Username, '') AS CreatedBy
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    LEFT JOIN RoomTypes rt ON b.RoomTypeId = rt.Id
    LEFT JOIN Users u ON u.Id = bp.CreatedBy
    LEFT JOIN Banks bk ON bp.BankId = bk.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    ORDER BY bp.PaidOn DESC, bp.Id DESC;

END;
GO

PRINT 'Stored Procedure sp_GetDailyCollectionRegister created successfully';
GO
