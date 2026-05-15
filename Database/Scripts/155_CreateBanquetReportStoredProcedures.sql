-- ============================================================
-- Script 155: Banquet Report Stored Procedures
-- ============================================================
SET NOCOUNT ON;
GO

-- --------------------------------------------------------
-- SP 1: Daily Collection Register for Banquet
-- --------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetBanquetCollectionRegister', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBanquetCollectionRegister;
GO

CREATE PROCEDURE dbo.sp_GetBanquetCollectionRegister
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Summary
    SELECT
        COUNT(DISTINCT bp.BanquetBookingId)       AS TotalReceipts,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END) AS TotalCollected,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END) AS TotalRefunded,
        SUM(bp.DiscountAmount)                    AS TotalDiscount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;

    -- Daily Totals
    SELECT
        CAST(bp.PaidOn AS DATE)                    AS CollectionDate,
        COUNT(*)                                   AS ReceiptCount,
        SUM(CASE WHEN bp.IsRefund = 0 THEN bp.Amount ELSE 0 END) AS CollectedAmount,
        SUM(CASE WHEN bp.IsRefund = 1 THEN bp.Amount ELSE 0 END) AS RefundedAmount
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = bp.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY CAST(bp.PaidOn AS DATE);

    -- Detail rows
    SELECT
        CAST(bp.PaidOn AS DATE)        AS CollectionDate,
        bp.ReceiptNumber,
        bb.BanquetBookingNumber,
        bb.GuestName                   AS ClientName,
        CASE WHEN bb.CustomerType = 'B2B' THEN ISNULL(bb.CompanyName, bb.GuestName) ELSE bb.GuestName END AS BilledTo,
        bv.VenueName,
        bb.EventDate,
        et.EventTypeName,
        bp.PaymentMethod,
        bp.Amount,
        bp.DiscountAmount,
        bp.IsAdvancePayment,
        bp.IsRefund,
        u.FirstName + ' ' + u.LastName AS CollectedBy
    FROM dbo.BanquetBookingPayments bp
    INNER JOIN dbo.BanquetBookings bb    ON bb.Id   = bp.BanquetBookingId
    INNER JOIN dbo.BanquetVenues bv      ON bv.Id   = bb.VenueId
    INNER JOIN dbo.BanquetEventTypes et  ON et.Id   = bb.EventTypeId
    LEFT  JOIN dbo.Users u               ON u.Id    = bp.CreatedBy
    WHERE bb.BranchID = @BranchID
      AND bp.[Status] IN ('Captured','Success')
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
    ORDER BY bp.PaidOn, bp.ReceiptNumber;
END
GO

-- --------------------------------------------------------
-- SP 2: Banquet GST Register
-- --------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetBanquetGSTRegister', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBanquetGSTRegister;
GO

CREATE PROCEDURE dbo.sp_GetBanquetGSTRegister
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Venue line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        CASE WHEN bb.CustomerType = 'B2B' THEN ISNULL(bb.CompanyName, bb.GuestName) ELSE bb.GuestName END AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Venue Hire' AS LineType,
        bv.SACCode,
        bb.VenueBaseAmount   AS TaxableValue,
        bb.VenueGSTAmount    AS TotalGST,
        bb.VenueCGSTAmount   AS CGST,
        bb.VenueSGSTAmount   AS SGST,
        bb.VenueIGSTAmount   AS IGST,
        CASE WHEN bb.IsInterState = 1 THEN bb.VenueIGSTAmount ELSE bb.VenueCGSTAmount + bb.VenueSGSTAmount END AS TaxCharged
    FROM dbo.BanquetBookings bb
    INNER JOIN dbo.BanquetVenues bv ON bv.Id = bb.VenueId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND bb.EventDate BETWEEN @FromDate AND @ToDate

    UNION ALL

    -- Package line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        CASE WHEN bb.CustomerType = 'B2B' THEN ISNULL(bb.CompanyName, bb.GuestName) ELSE bb.GuestName END AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Package (' + pl.PackageName + ')' AS LineType,
        pl.SACCode,
        pl.BaseAmount  AS TaxableValue,
        pl.GSTAmount   AS TotalGST,
        pl.CGSTAmount  AS CGST,
        pl.SGSTAmount  AS SGST,
        pl.IGSTAmount  AS IGST,
        CASE WHEN bb.IsInterState = 1 THEN pl.IGSTAmount ELSE pl.CGSTAmount + pl.SGSTAmount END AS TaxCharged
    FROM dbo.BanquetBookingPackageLines pl
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = pl.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND bb.EventDate BETWEEN @FromDate AND @ToDate

    UNION ALL

    -- Addon line items
    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        CASE WHEN bb.CustomerType = 'B2B' THEN ISNULL(bb.CompanyName, bb.GuestName) ELSE bb.GuestName END AS ClientName,
        bb.CustomerType,
        CASE WHEN bb.IsInterState = 1 THEN 'Inter-State' ELSE 'Intra-State' END AS SupplyType,
        'Addon (' + al.ServiceName + ')' AS LineType,
        al.SACCode,
        al.BaseAmount AS TaxableValue,
        al.GSTAmount  AS TotalGST,
        al.CGSTAmount AS CGST,
        al.SGSTAmount AS SGST,
        al.IGSTAmount AS IGST,
        CASE WHEN bb.IsInterState = 1 THEN al.IGSTAmount ELSE al.CGSTAmount + al.SGSTAmount END AS TaxCharged
    FROM dbo.BanquetBookingAddonLines al
    INNER JOIN dbo.BanquetBookings bb ON bb.Id = al.BanquetBookingId
    WHERE bb.BranchID = @BranchID
      AND bb.[Status] NOT IN ('Cancelled')
      AND bb.EventDate BETWEEN @FromDate AND @ToDate

    ORDER BY bb.EventDate, bb.BanquetBookingNumber;
END
GO

-- --------------------------------------------------------
-- SP 3: Venue Utilization Report
-- --------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetVenueUtilizationReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetVenueUtilizationReport;
GO

CREATE PROCEDURE dbo.sp_GetVenueUtilizationReport
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        bv.VenueName,
        bv.VenueType,
        bv.CapacitySeated,
        COUNT(bb.Id)                                                                  AS TotalBookings,
        COUNT(CASE WHEN bb.[Status] = 'EventComplete' THEN 1 END)                   AS CompletedEvents,
        COUNT(CASE WHEN bb.[Status] = 'Cancelled'    THEN 1 END)                    AS CancelledEvents,
        ISNULL(SUM(CASE WHEN bb.[Status] <> 'Cancelled' THEN bb.TotalAmount END),0) AS TotalRevenue,
        ISNULL(SUM(CASE WHEN bb.[Status] <> 'Cancelled' THEN bb.TotalBaseAmount END),0) AS BaseRevenue,
        ISNULL(SUM(CASE WHEN bb.[Status] <> 'Cancelled' THEN bb.TotalGSTAmount END),0)  AS TotalGST,
        ISNULL(AVG(CASE WHEN bb.[Status] <> 'Cancelled' THEN bb.AttendeeCount END),0)   AS AvgAttendees
    FROM dbo.BanquetVenues bv
    LEFT JOIN dbo.BanquetBookings bb ON bb.VenueId = bv.Id
        AND bb.EventDate BETWEEN @FromDate AND @ToDate
    WHERE bv.BranchID = @BranchID
      AND bv.IsActive = 1
    GROUP BY bv.Id, bv.VenueName, bv.VenueType, bv.CapacitySeated
    ORDER BY TotalRevenue DESC;
END
GO

-- --------------------------------------------------------
-- SP 4: Event Type Performance
-- --------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetEventTypePerformance', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetEventTypePerformance;
GO

CREATE PROCEDURE dbo.sp_GetEventTypePerformance
    @BranchID  INT,
    @FromDate  DATE,
    @ToDate    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        et.EventTypeName,
        COUNT(bb.Id)                                                                  AS TotalBookings,
        COUNT(CASE WHEN bb.[Status] = 'EventComplete' THEN 1 END)                    AS CompletedEvents,
        ISNULL(SUM(CASE WHEN bb.[Status] <> 'Cancelled' THEN bb.TotalAmount END),0)  AS TotalRevenue,
        ISNULL(AVG(CASE WHEN bb.[Status] <> 'Cancelled' THEN CAST(bb.AttendeeCount AS FLOAT) END),0) AS AvgAttendees
    FROM dbo.BanquetEventTypes et
    LEFT JOIN dbo.BanquetBookings bb ON bb.EventTypeId = et.Id
        AND bb.EventDate BETWEEN @FromDate AND @ToDate
    WHERE et.BranchID = @BranchID
      AND et.IsActive = 1
    GROUP BY et.Id, et.EventTypeName
    ORDER BY TotalRevenue DESC;
END
GO

-- --------------------------------------------------------
-- SP 5: Banquet Outstanding Balance (B2B credit)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.sp_GetBanquetOutstandingBalance', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBanquetOutstandingBalance;
GO

CREATE PROCEDURE dbo.sp_GetBanquetOutstandingBalance
    @BranchID  INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        bb.BanquetBookingNumber,
        bb.EventDate,
        bb.EventName,
        CASE WHEN bb.CustomerType = 'B2B' THEN ISNULL(bb.CompanyName, bb.GuestName) ELSE bb.GuestName END AS ClientName,
        bb.CustomerType,
        bb.GuestPhone,
        bb.GuestEmail,
        bb.TotalAmount,
        bb.DepositAmount,
        bb.BalanceAmount,
        bb.CreditDays,
        DATEDIFF(DAY, bb.EventDate, GETDATE())          AS DaysElapsed,
        bb.PaymentStatus,
        bb.[Status]
    FROM dbo.BanquetBookings bb
    WHERE bb.BranchID = @BranchID
      AND bb.BalanceAmount > 0
      AND bb.[Status] NOT IN ('Cancelled')
    ORDER BY bb.EventDate;
END
GO

PRINT 'Script 155 (Banquet Report SPs) completed successfully.';
GO
