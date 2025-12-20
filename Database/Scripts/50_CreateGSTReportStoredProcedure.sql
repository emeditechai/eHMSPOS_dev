-- =============================================
-- GST Report (Booking-wise) Stored Procedure
-- Description: GST register booking-wise, filtered by payment date (branch-wise)
-- Notes:
--  - Includes bookings that have at least one successful payment in the period
--  - Excludes Cancelled bookings
--  - Splits CGST/SGST proportionally from booking GST amounts based on paid amount
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetGstReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetGstReport;
GO

CREATE PROCEDURE sp_GetGstReport
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

    IF OBJECT_ID('tempdb..#B') IS NOT NULL DROP TABLE #B;

    CREATE TABLE #B (
        BookingId INT NOT NULL,
        PaymentDate DATETIME2 NOT NULL,
        BookingNumber NVARCHAR(25) NULL,
        GuestName NVARCHAR(205) NULL,
        RoomType NVARCHAR(200) NULL,
        Status NVARCHAR(50) NULL,
        PaymentStatus NVARCHAR(50) NULL,
        PaidAmount DECIMAL(18,2) NOT NULL,
        CGSTAmount DECIMAL(18,2) NOT NULL,
        SGSTAmount DECIMAL(18,2) NOT NULL,
        TaxableValue DECIMAL(18,2) NOT NULL,
        CreatedBy INT NULL
    );

    INSERT INTO #B (
        BookingId,
        PaymentDate,
        BookingNumber,
        GuestName,
        RoomType,
        Status,
        PaymentStatus,
        PaidAmount,
        CGSTAmount,
        SGSTAmount,
        TaxableValue,
        CreatedBy
    )
    SELECT
        b.Id AS BookingId,
        MAX(bp.PaidOn) AS PaymentDate,
        b.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName, ' ', b.PrimaryGuestLastName) AS GuestName,
        rt.TypeName AS RoomType,
        b.Status,
        b.PaymentStatus,
        CAST(ISNULL(SUM(bp.Amount), 0) AS DECIMAL(18,2)) AS PaidAmount,
        CAST(ISNULL(SUM(bp.Amount * ISNULL(b.CGSTAmount, 0) / NULLIF(b.TotalAmount, 0)), 0) AS DECIMAL(18,2)) AS CGSTAmount,
        CAST(ISNULL(SUM(bp.Amount * ISNULL(b.SGSTAmount, 0) / NULLIF(b.TotalAmount, 0)), 0) AS DECIMAL(18,2)) AS SGSTAmount,
        CAST(
            ISNULL(SUM(bp.Amount), 0)
            - ISNULL(SUM(bp.Amount * ISNULL(b.CGSTAmount, 0) / NULLIF(b.TotalAmount, 0)), 0)
            - ISNULL(SUM(bp.Amount * ISNULL(b.SGSTAmount, 0) / NULLIF(b.TotalAmount, 0)), 0)
            AS DECIMAL(18,2)
        ) AS TaxableValue,
        MAX(bp.CreatedBy) AS CreatedBy
    FROM Bookings b
    INNER JOIN BookingPayments bp ON bp.BookingId = b.Id
    LEFT JOIN RoomTypes rt ON b.RoomTypeId = rt.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN ('Completed', 'Captured', 'Success')
      AND ISNULL(b.Status, '') <> 'Cancelled'
    GROUP BY
        b.Id,
        b.BookingNumber,
        b.PrimaryGuestFirstName,
        b.PrimaryGuestLastName,
        rt.TypeName,
        b.Status,
        b.PaymentStatus,
        b.CGSTAmount,
        b.SGSTAmount,
        b.TotalAmount;

    -- Summary
    SELECT
        COUNT(1) AS TotalBookings,
        ISNULL(SUM(PaidAmount), 0) AS TotalPaidAmount,
        ISNULL(SUM(CGSTAmount), 0) AS TotalCGST,
        ISNULL(SUM(SGSTAmount), 0) AS TotalSGST,
        ISNULL(SUM(CGSTAmount + SGSTAmount), 0) AS TotalGST,
        ISNULL(SUM(TaxableValue), 0) AS TotalTaxableValue
    FROM #B;

    -- Details
    SELECT
        CAST(b.PaymentDate AS DATE) AS PaymentDate,
        b.PaymentDate AS PaidOn,
        BookingNumber,
        GuestName,
        RoomType,
        (CGSTAmount + SGSTAmount) AS GSTAmount,
        CGSTAmount,
        SGSTAmount,
        TaxableValue,
        PaidAmount,
        Status,
        PaymentStatus,
        COALESCE(u.FullName, CONCAT(u.FirstName, ' ', u.LastName), u.Username, '') AS CreatedBy
    FROM #B b
    LEFT JOIN Users u ON u.Id = b.CreatedBy
    ORDER BY b.PaymentDate DESC, b.BookingId DESC;

    DROP TABLE #B;

END;
GO

PRINT 'Stored Procedure sp_GetGstReport created successfully';
GO
