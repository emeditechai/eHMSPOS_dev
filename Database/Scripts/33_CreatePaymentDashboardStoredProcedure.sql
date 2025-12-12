-- =============================================
-- Payment Dashboard Stored Procedure
-- Description: Fetches payment statistics and details for dashboard
-- =============================================

USE HMS_dev;
GO

-- Drop existing procedure if exists
IF OBJECT_ID('sp_GetPaymentDashboardData', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPaymentDashboardData;
GO

CREATE PROCEDURE sp_GetPaymentDashboardData
    @BranchID INT,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Summary Statistics
    SELECT 
        -- Total Payments
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount ELSE 0 END), 0) AS TotalPayments,
        
        -- Total GST
        ISNULL(SUM(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') 
            THEN (bp.Amount * b.TaxAmount / NULLIF(b.TotalAmount, 0)) ELSE 0 END), 0) AS TotalGST,
        
        -- Total Tips (assuming tips are stored separately or as a payment type)
        ISNULL(SUM(CASE WHEN bp.PaymentMethod = 'Tips' AND bp.Status IN ('Completed', 'Captured', 'Success') 
            THEN bp.Amount ELSE 0 END), 0) AS TotalTips,
        
        -- Payment Count
        COUNT(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN 1 END) AS PaymentCount,
        
        -- Average Payment
        ISNULL(AVG(CASE WHEN bp.Status IN ('Completed', 'Captured', 'Success') THEN bp.Amount END), 0) AS AveragePayment
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;

    -- Payment Method Breakdown
    SELECT 
        ISNULL(bp.PaymentMethod, 'Unknown') AS PaymentMethod,
        COUNT(*) AS TransactionCount,
        SUM(bp.Amount) AS TotalAmount,
        AVG(bp.Amount) AS AverageAmount
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        AND bp.Status IN ('Completed', 'Captured', 'Success')
    GROUP BY bp.PaymentMethod
    ORDER BY TotalAmount DESC;

    -- Payment Details List
    SELECT 
        bp.Id AS PaymentId,
        b.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
        b.PrimaryGuestPhone AS GuestPhone,
        bp.PaymentMethod,
        bp.Amount,
        bp.Status,
        bp.PaidOn,
        bp.PaymentReference,
        bp.BankId,
        bk.BankName,
        bp.Notes,
        bp.ReceiptNumber,
        COALESCE(u.FullName, CONCAT(u.FirstName, ' ', u.LastName), u.Username, '') AS CreatedBy,
        b.TotalAmount AS BookingTotal,
        b.BalanceAmount AS BookingBalance,
        rt.TypeName AS RoomType
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    LEFT JOIN Users u ON u.Id = bp.CreatedBy
    LEFT JOIN Banks bk ON bp.BankId = bk.Id
    LEFT JOIN RoomTypes rt ON b.RoomTypeId = rt.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    ORDER BY bp.PaidOn DESC;

    -- Daily Payment Trend (for chart)
    SELECT 
        CAST(bp.PaidOn AS DATE) AS PaymentDate,
        COUNT(*) AS TransactionCount,
        SUM(bp.Amount) AS TotalAmount
    FROM BookingPayments bp
    INNER JOIN Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
        AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        AND bp.Status IN ('Completed', 'Captured', 'Success')
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY PaymentDate;

END;
GO

PRINT 'Stored Procedure sp_GetPaymentDashboardData created successfully';
GO
