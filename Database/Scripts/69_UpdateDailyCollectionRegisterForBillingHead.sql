-- =============================================
-- Migration: Update Daily Collection Register report for BillingHead-wise payments
--
-- Why:
--  - BookingPayments can now store multiple rows per payment transaction (BillingHead-wise)
--  - Report should display BillingHead and count receipts by ReceiptGroupNumber when present
--
-- Notes:
--  - Uses dynamic SQL to stay backward compatible if columns don't exist yet.
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

    DECLARE @HasReceiptGroup BIT = CASE WHEN COL_LENGTH('dbo.BookingPayments', 'ReceiptGroupNumber') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBillingHead  BIT = CASE WHEN COL_LENGTH('dbo.BookingPayments', 'BillingHead') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBillingHeads BIT = CASE WHEN OBJECT_ID('dbo.BillingHeads', 'U') IS NOT NULL THEN 1 ELSE 0 END;

    -- BillingHeads schema differs across environments:
    --  - Some DBs use: Id, BillingCode, DisplayName, IsActive
    --  - Others may use: Id, Code, Name, IsActive
    DECLARE @HasBH_DisplayName BIT = CASE WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads', 'DisplayName') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBH_Name        BIT = CASE WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads', 'Name') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBH_BillingCode BIT = CASE WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads', 'BillingCode') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasBH_Code        BIT = CASE WHEN @HasBillingHeads = 1 AND COL_LENGTH('dbo.BillingHeads', 'Code') IS NOT NULL THEN 1 ELSE 0 END;

    DECLARE @ReceiptExpr NVARCHAR(400);
    SET @ReceiptExpr = CASE WHEN @HasReceiptGroup = 1
        THEN 'ISNULL(bp.ReceiptGroupNumber, bp.ReceiptNumber)'
        ELSE 'bp.ReceiptNumber'
    END;

    DECLARE @BillingHeadSelect NVARCHAR(400);
    DECLARE @BillingHeadJoin NVARCHAR(800);

    DECLARE @BH_DisplayExpr NVARCHAR(64) = NULL;
    DECLARE @BH_CodeExpr NVARCHAR(64) = NULL;
    DECLARE @BH_FallbackMapExpr NVARCHAR(400);

    SET @BH_DisplayExpr = CASE
        WHEN @HasBH_DisplayName = 1 THEN 'bh.DisplayName'
        WHEN @HasBH_Name = 1 THEN 'bh.Name'
        ELSE NULL
    END;

    SET @BH_CodeExpr = CASE
        WHEN @HasBH_BillingCode = 1 THEN 'bh.BillingCode'
        WHEN @HasBH_Code = 1 THEN 'bh.Code'
        ELSE NULL
    END;

    -- Hard fallback to avoid showing raw codes if join doesn't match for any reason.
    SET @BH_FallbackMapExpr =
        'CASE UPPER(LTRIM(RTRIM(bp.BillingHead))) '
        + 'WHEN ''S'' THEN ''Stay Charges'' '
        + 'WHEN ''R'' THEN ''Room Services'' '
        + 'WHEN ''O'' THEN ''Other Charges'' '
        + 'ELSE NULL END';

    SET @BillingHeadSelect = CASE
        WHEN @HasBillingHead = 1 AND @HasBillingHeads = 1 AND @BH_DisplayExpr IS NOT NULL
            THEN 'COALESCE(' + @BH_DisplayExpr + ', ' + @BH_FallbackMapExpr + ', bp.BillingHead) AS BillingHead,'
        WHEN @HasBillingHead = 1
            THEN 'COALESCE(' + @BH_FallbackMapExpr + ', bp.BillingHead) AS BillingHead,'
        ELSE 'CAST(NULL AS NVARCHAR(200)) AS BillingHead,'
    END;

    SET @BillingHeadJoin = '';
    IF (@HasBillingHead = 1 AND @HasBillingHeads = 1)
    BEGIN
        SET @BillingHeadJoin = '
        LEFT JOIN dbo.BillingHeads bh
            ON (TRY_CONVERT(INT, bp.BillingHead) = bh.Id';

        IF (@BH_CodeExpr IS NOT NULL)
        BEGIN
            SET @BillingHeadJoin = @BillingHeadJoin + '
                OR UPPER(LTRIM(RTRIM(bp.BillingHead))) = UPPER(LTRIM(RTRIM(' + @BH_CodeExpr + ')))';
        END

        SET @BillingHeadJoin = @BillingHeadJoin + '
            )';
    END

    -- Summary
    DECLARE @SqlSummary NVARCHAR(MAX) = N'
        SELECT
            COUNT(DISTINCT ' + @ReceiptExpr + N') AS TotalReceipts,
            ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'', ''Captured'', ''Success'') THEN bp.Amount ELSE 0 END), 0) AS TotalCollected,
            ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'', ''Captured'', ''Success'') THEN (bp.Amount * b.TaxAmount / NULLIF(b.TotalAmount, 0)) ELSE 0 END), 0) AS TotalGST,
            ISNULL(SUM(ISNULL(bp.DiscountAmount, 0)), 0) AS TotalDiscount,
            ISNULL(SUM(CASE WHEN ISNULL(bp.IsRoundOffApplied, 0) = 1 THEN ISNULL(bp.RoundOffAmount, 0) ELSE 0 END), 0) AS TotalRoundOff
        FROM BookingPayments bp
        INNER JOIN Bookings b ON bp.BookingId = b.Id
        WHERE b.BranchID = @BranchID
          AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate;';

    EXEC sp_executesql
        @SqlSummary,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID = @BranchID,
        @FromDate = @FromDate,
        @ToDate = @ToDate;

    -- Daily totals
    DECLARE @SqlDaily NVARCHAR(MAX) = N'
        SELECT
            CAST(bp.PaidOn AS DATE) AS CollectionDate,
            COUNT(DISTINCT ' + @ReceiptExpr + N') AS ReceiptCount,
            ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'', ''Captured'', ''Success'') THEN bp.Amount ELSE 0 END), 0) AS CollectedAmount,
            ISNULL(SUM(CASE WHEN bp.Status IN (''Completed'', ''Captured'', ''Success'') THEN (bp.Amount * b.TaxAmount / NULLIF(b.TotalAmount, 0)) ELSE 0 END), 0) AS GSTAmount
        FROM BookingPayments bp
        INNER JOIN Bookings b ON bp.BookingId = b.Id
        WHERE b.BranchID = @BranchID
          AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        GROUP BY CAST(bp.PaidOn AS DATE)
        ORDER BY CollectionDate;';

    EXEC sp_executesql
        @SqlDaily,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID = @BranchID,
        @FromDate = @FromDate,
        @ToDate = @ToDate;

    -- Details
    DECLARE @SqlDetails NVARCHAR(MAX) = N'
        SELECT
            CAST(bp.PaidOn AS DATE) AS CollectionDate,
            bp.PaidOn,
            ' + @ReceiptExpr + N' AS ReceiptNumber,
            b.BookingNumber,
            CONCAT(b.PrimaryGuestFirstName, '' '', b.PrimaryGuestLastName) AS GuestName,
            b.PrimaryGuestPhone AS GuestPhone,
            rt.TypeName AS RoomType,
            ' + @BillingHeadSelect + N'
            bp.PaymentMethod,
            bk.BankName,
            bp.PaymentReference,
            bp.Amount AS ReceiptAmount,
            ISNULL(bp.DiscountAmount, 0) AS DiscountAmount,
            ISNULL(bp.DiscountPercent, 0) AS DiscountPercent,
            ISNULL(bp.RoundOffAmount, 0) AS RoundOffAmount,
            ISNULL(bp.IsRoundOffApplied, 0) AS IsRoundOffApplied,
            bp.Status,
            COALESCE(u.FullName, CONCAT(u.FirstName, '' '', u.LastName), u.Username, '''') AS CreatedBy
        FROM BookingPayments bp
        INNER JOIN Bookings b ON bp.BookingId = b.Id
        LEFT JOIN RoomTypes rt ON b.RoomTypeId = rt.Id
        LEFT JOIN Users u ON u.Id = bp.CreatedBy
        LEFT JOIN Banks bk ON bp.BankId = bk.Id
                ' + @BillingHeadJoin + N'
        WHERE b.BranchID = @BranchID
          AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
        ORDER BY bp.PaidOn DESC, bp.Id DESC;';

    EXEC sp_executesql
        @SqlDetails,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID = @BranchID,
        @FromDate = @FromDate,
        @ToDate = @ToDate;

END;
GO

PRINT 'Stored Procedure sp_GetDailyCollectionRegister updated for BillingHead-wise payments';
GO
