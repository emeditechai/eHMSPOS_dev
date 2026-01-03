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

    /* ------------------------------------------------------------
       Schema detection (Billing Heads)
    ------------------------------------------------------------ */
    DECLARE @HasBillingHead  BIT = CASE WHEN COL_LENGTH('dbo.BookingPayments','BillingHead') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBillingHeads BIT = CASE WHEN OBJECT_ID('dbo.BillingHeads','U') IS NOT NULL THEN 1 ELSE 0 END;

    DECLARE @BH_DisplayExpr NVARCHAR(64) =
        CASE
            WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads','DisplayName') IS NOT NULL THEN 'bh.DisplayName'
            WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads','Name') IS NOT NULL        THEN 'bh.Name'
            ELSE NULL
        END;

    DECLARE @BH_CodeExpr NVARCHAR(64) =
        CASE
            WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads','BillingCode') IS NOT NULL THEN 'bh.BillingCode'
            WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads','Code') IS NOT NULL        THEN 'bh.Code'
            ELSE NULL
        END;

    DECLARE @BillingHeadJoin   NVARCHAR(MAX) = N'';
    DECLARE @BillingHeadExpr   NVARCHAR(MAX) = N'CAST(N''Stay Charges'' AS NVARCHAR(200))';
    DECLARE @HeadCodeExpr      NVARCHAR(300) = N'''S''';

    IF @HasBillingHead = 1
    BEGIN
        SET @BillingHeadExpr =
            N'CAST(COALESCE(' + ISNULL(@BH_DisplayExpr,'NULL') + N',
                CASE UPPER(LTRIM(RTRIM(bp.BillingHead)))
                    WHEN ''S'' THEN ''Stay Charges''
                    WHEN ''R'' THEN ''Room Services''
                    WHEN ''O'' THEN ''Other Charges''
                END,
                CAST(bp.BillingHead AS NVARCHAR(200))
            ) AS NVARCHAR(200))';

        IF @HasBillingHeads = 1
        BEGIN
            SET @BillingHeadJoin = N'
    LEFT JOIN dbo.BillingHeads bh
      ON TRY_CONVERT(INT, bp.BillingHead) = bh.Id';

            IF @BH_CodeExpr IS NOT NULL
                SET @BillingHeadJoin +=
                N' OR UPPER(LTRIM(RTRIM(bp.BillingHead))) = UPPER(LTRIM(RTRIM(' + @BH_CodeExpr + N')))';
        END;

        SET @HeadCodeExpr =
            CASE
                WHEN @HasBillingHeads = 1 AND @BH_CodeExpr IS NOT NULL
                    THEN N'COALESCE(UPPER(LTRIM(RTRIM(' + @BH_CodeExpr + N'))), UPPER(LTRIM(RTRIM(bp.BillingHead))))'
                ELSE N'UPPER(LTRIM(RTRIM(bp.BillingHead)))'
            END;
    END;

    /* ------------------------------------------------------------
       User join detection (CreatedBy may be INT or string)
    ------------------------------------------------------------ */
    DECLARE @UserJoin NVARCHAR(MAX) = N'';
    DECLARE @CreatedByExpr NVARCHAR(MAX) = N'CAST(bp.CreatedBy AS NVARCHAR(200))';

    IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    BEGIN
        SET @UserJoin = N'
    LEFT JOIN dbo.Users u ON u.Id = TRY_CAST(bp.CreatedBy AS INT)';
        SET @CreatedByExpr = N'CAST(ISNULL(u.FullName, CAST(bp.CreatedBy AS NVARCHAR(200))) AS NVARCHAR(200))';
    END;

    /* ------------------------------------------------------------
       Precompute charge totals (used for GST allocation)
    ------------------------------------------------------------ */
    IF OBJECT_ID('tempdb..#OC') IS NOT NULL DROP TABLE #OC;
    IF OBJECT_ID('tempdb..#RS') IS NOT NULL DROP TABLE #RS;

    SELECT
        BookingId,
        SUM(Rate * CASE WHEN ISNULL(Qty,0) <= 0 THEN 1 ELSE Qty END) AS BaseAmount,
        SUM(GSTAmount)  AS GSTAmount,
        SUM(CGSTAmount) AS CGSTAmount,
        SUM(SGSTAmount) AS SGSTAmount,
        SUM(Rate * CASE WHEN ISNULL(Qty,0) <= 0 THEN 1 ELSE Qty END) + SUM(GSTAmount) AS GrossAmount
    INTO #OC
    FROM dbo.BookingOtherCharges
    GROUP BY BookingId;

    SELECT
        BookingID AS BookingId,
        SUM(ActualBillAmount) AS GrossAmount,
        SUM(GSTAmount)  AS GSTAmount,
        SUM(CGSTAmount) AS CGSTAmount,
        SUM(SGSTAmount) AS SGSTAmount
    INTO #RS
    FROM dbo.RoomServices
    GROUP BY BookingID;

    DECLARE @Sql NVARCHAR(MAX) = N'';

    /* ------------------------------------------------------------
       1) Summary Statistics
    ------------------------------------------------------------ */
    SET @Sql += N'
    SELECT
        ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'',''Captured'',''Success'') THEN bp.Amount ELSE 0 END), 0) AS TotalPayments,
        ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'',''Captured'',''Success'')
            THEN ROUND(
                bp.Amount *
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GSTAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GSTAmount,0)
                    ELSE ISNULL(b.TaxAmount,0)
                END
                /
                NULLIF(
                    CASE
                        WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                        WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                        ELSE ISNULL(b.TotalAmount,0)
                    END, 0),
            2)
            ELSE 0 END), 0) AS TotalGST,
        ISNULL(SUM(CASE WHEN bp.PaymentMethod = ''Tips'' AND bp.Status IN (''Completed'',''Captured'',''Success'') THEN bp.Amount ELSE 0 END), 0) AS TotalTips,
        COUNT(CASE WHEN bp.Status IN (''Completed'',''Captured'',''Success'') THEN 1 END) AS PaymentCount,
        ISNULL(AVG(CASE WHEN bp.Status IN (''Completed'',''Captured'',''Success'') THEN bp.Amount END), 0) AS AveragePayment
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON bp.BookingId = b.Id
    ' + @BillingHeadJoin + N'
    LEFT JOIN #OC oc ON oc.BookingId = b.Id
    LEFT JOIN #RS rs ON rs.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND ISNULL(b.Status,'''') <> ''Cancelled'';
    ';

    /* ------------------------------------------------------------
       2) Payment Method Breakdown
    ------------------------------------------------------------ */
    SET @Sql += N'
    SELECT
        ISNULL(bp.PaymentMethod, ''Unknown'') AS PaymentMethod,
        COUNT(*) AS TransactionCount,
        SUM(bp.Amount) AS TotalAmount,
        ISNULL(AVG(bp.Amount), 0) AS AverageAmount
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled''
    GROUP BY bp.PaymentMethod
    ORDER BY TotalAmount DESC;
    ';

    /* ------------------------------------------------------------
       3) Billing Head Breakdown
    ------------------------------------------------------------ */
    SET @Sql += N'
    SELECT
        ' + @BillingHeadExpr + N' AS BillingHead,
        COUNT(*) AS TransactionCount,
        SUM(bp.Amount) AS TotalAmount,
        SUM(ROUND(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GSTAmount,0)
                ELSE ISNULL(b.TaxAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END, 0),
        2)) AS TotalGST
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON bp.BookingId = b.Id
    ' + @BillingHeadJoin + N'
    LEFT JOIN #OC oc ON oc.BookingId = b.Id
    LEFT JOIN #RS rs ON rs.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled''
    GROUP BY ' + @BillingHeadExpr + N'
    ORDER BY TotalAmount DESC;
    ';

    /* ------------------------------------------------------------
       4) Payment Details List
    ------------------------------------------------------------ */
    SET @Sql += N'
    SELECT
        bp.Id AS PaymentId,
        b.BookingNumber,
        bp.ReceiptNumber,
        bp.PaidOn,
        ISNULL(bp.PaymentMethod, ''Unknown'') AS PaymentMethod,
        ' + @BillingHeadExpr + N' AS BillingHead,
        bp.Amount,
        ROUND(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GSTAmount,0)
                ELSE ISNULL(b.TaxAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END, 0),
        2) AS GSTAmount,
        ' + @CreatedByExpr + N' AS CreatedBy,
        b.BalanceAmount AS BookingBalance
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON bp.BookingId = b.Id
    ' + @BillingHeadJoin + N'
    ' + @UserJoin + N'
    LEFT JOIN #OC oc ON oc.BookingId = b.Id
    LEFT JOIN #RS rs ON rs.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled''
    ORDER BY bp.PaidOn DESC;
    ';

    /* ------------------------------------------------------------
       5) Daily Payment Trend (for chart)
    ------------------------------------------------------------ */
    SET @Sql += N'
    SELECT
        CAST(bp.PaidOn AS DATE) AS PaymentDate,
        COUNT(*) AS TransactionCount,
        SUM(bp.Amount) AS TotalAmount
    FROM dbo.BookingPayments bp
    INNER JOIN dbo.Bookings b ON bp.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled''
    GROUP BY CAST(bp.PaidOn AS DATE)
    ORDER BY PaymentDate;
    ';

    EXEC sp_executesql
        @Sql,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID = @BranchID,
        @FromDate = @FromDate,
        @ToDate = @ToDate;

END;
GO

PRINT 'Stored Procedure sp_GetPaymentDashboardData created successfully';
GO
