USE HMS_dev;
GO

IF OBJECT_ID('dbo.sp_GetGstReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGstReport;
GO

CREATE PROCEDURE dbo.sp_GetGstReport
(
    @BranchID INT,
    @FromDate DATE = NULL,
    @ToDate   DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = CAST(GETDATE() AS DATE);
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    IF @ToDate < @FromDate
    BEGIN
        DECLARE @Tmp DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate = @Tmp;
    END;

    /* ------------------------------------------------------------
       Schema detection
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
    DECLARE @BillingHeadSelect NVARCHAR(MAX) = N'CAST(NULL AS NVARCHAR(200)) AS BillingHead,';

    IF @HasBillingHead = 1
    BEGIN
        SET @BillingHeadSelect =
        N'COALESCE(' + ISNULL(@BH_DisplayExpr,'NULL') + N',
            CASE UPPER(LTRIM(RTRIM(bp.BillingHead)))
                WHEN ''S'' THEN ''Stay Charges''
                WHEN ''R'' THEN ''Room Services''
                WHEN ''O'' THEN ''Other Charges''
            END,
            bp.BillingHead
        ) AS BillingHead,';

        IF @HasBillingHeads = 1
        BEGIN
            SET @BillingHeadJoin = N'
            LEFT JOIN dbo.BillingHeads bh
              ON TRY_CONVERT(INT, bp.BillingHead) = bh.Id';

            IF @BH_CodeExpr IS NOT NULL
                SET @BillingHeadJoin +=
                N' OR UPPER(LTRIM(RTRIM(bp.BillingHead))) = UPPER(LTRIM(RTRIM(' + @BH_CodeExpr + N')))';
        END;
    END;

    DECLARE @HeadCodeExpr NVARCHAR(300) =
        CASE
            WHEN @HasBillingHead = 1 AND @HasBillingHeads = 1 AND @BH_CodeExpr IS NOT NULL
                THEN N'COALESCE(UPPER(LTRIM(RTRIM(' + @BH_CodeExpr + N'))), UPPER(LTRIM(RTRIM(bp.BillingHead))))'
            WHEN @HasBillingHead = 1
                THEN N'UPPER(LTRIM(RTRIM(bp.BillingHead)))'
            ELSE N'''S'''
        END;

    /* ------------------------------------------------------------
       SUMMARY QUERY
    ------------------------------------------------------------ */
    DECLARE @SqlSummary NVARCHAR(MAX) = N'
    WITH OC AS (
        SELECT BookingId,
               SUM(Rate * CASE WHEN ISNULL(Qty,0)<=0 THEN 1 ELSE Qty END) AS BaseAmount,
               SUM(GSTAmount)  AS GSTAmount,
               SUM(CGSTAmount) AS CGSTAmount,
               SUM(SGSTAmount) AS SGSTAmount,
               SUM(Rate * CASE WHEN ISNULL(Qty,0)<=0 THEN 1 ELSE Qty END) + SUM(GSTAmount) AS GrossAmount
        FROM dbo.BookingOtherCharges
        GROUP BY BookingId
    ),
    RS AS (
        SELECT BookingID AS BookingId,
               SUM(ActualBillAmount) AS GrossAmount,
               SUM(GSTAmount)  AS GSTAmount,
               SUM(CGSTAmount) AS CGSTAmount,
               SUM(SGSTAmount) AS SGSTAmount
        FROM dbo.RoomServices
        GROUP BY BookingID
    )
    SELECT
        COUNT(DISTINCT b.Id) AS TotalBookings,
        SUM(bp.Amount) AS TotalPaidAmount,

        ROUND(SUM(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.CGSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.CGSTAmount,0)
                ELSE ISNULL(b.CGSTAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END,0)
        ),2) AS TotalCGST,

        ROUND(SUM(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.SGSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.SGSTAmount,0)
                ELSE ISNULL(b.SGSTAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END,0)
        ),2) AS TotalSGST,

        ROUND(SUM(
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
                END,0)
        ),2) AS TotalGST,

        ROUND(SUM(
            bp.Amount -
            (
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
                    END,0)
            )
        ),2) AS TotalTaxableValue
    FROM dbo.Bookings b
    INNER JOIN dbo.BookingPayments bp ON bp.BookingId = b.Id
    ' + @BillingHeadJoin + N'
    LEFT JOIN OC oc ON oc.BookingId = b.Id
    LEFT JOIN RS rs ON rs.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled'';';

    EXEC sp_executesql
        @SqlSummary,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID, @FromDate, @ToDate;

    /* ------------------------------------------------------------
       DETAILS QUERY
    ------------------------------------------------------------ */
    DECLARE @SqlDetails NVARCHAR(MAX) = N'
    WITH OC AS (
        SELECT BookingId,
               SUM(Rate * CASE WHEN ISNULL(Qty,0)<=0 THEN 1 ELSE Qty END) AS BaseAmount,
               SUM(GSTAmount)  AS GSTAmount,
               SUM(CGSTAmount) AS CGSTAmount,
               SUM(SGSTAmount) AS SGSTAmount,
               SUM(Rate * CASE WHEN ISNULL(Qty,0)<=0 THEN 1 ELSE Qty END) + SUM(GSTAmount) AS GrossAmount
        FROM dbo.BookingOtherCharges
        GROUP BY BookingId
    ),
    RS AS (
        SELECT BookingID AS BookingId,
               SUM(ActualBillAmount) AS GrossAmount,
               SUM(GSTAmount)  AS GSTAmount,
               SUM(CGSTAmount) AS CGSTAmount,
               SUM(SGSTAmount) AS SGSTAmount
        FROM dbo.RoomServices
        GROUP BY BookingID
    )
    SELECT
        CAST(bp.PaidOn AS DATE) AS PaymentDate,
        bp.PaidOn,
        b.BookingNumber,
        CONCAT(b.PrimaryGuestFirstName,'' '',b.PrimaryGuestLastName) AS GuestName,
        ' + @BillingHeadSelect + N'
        ROUND(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.CGSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.CGSTAmount,0)
                ELSE ISNULL(b.CGSTAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END,0),
        2) AS CGSTAmount,

        ROUND(
            bp.Amount *
            CASE
                WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.SGSTAmount,0)
                WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.SGSTAmount,0)
                ELSE ISNULL(b.SGSTAmount,0)
            END
            /
            NULLIF(
                CASE
                    WHEN ' + @HeadCodeExpr + N' = ''O'' THEN ISNULL(oc.GrossAmount,0)
                    WHEN ' + @HeadCodeExpr + N' = ''R'' THEN ISNULL(rs.GrossAmount,0)
                    ELSE ISNULL(b.TotalAmount,0)
                END,0),
        2) AS SGSTAmount,

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
                END,0),
        2) AS GSTAmount,

        ROUND(
            bp.Amount -
            (
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
                    END,0)
            ),
        2) AS TaxableValue,

        CAST(bp.Amount AS DECIMAL(18,2)) AS PaidAmount
    FROM dbo.Bookings b
    INNER JOIN dbo.BookingPayments bp ON bp.BookingId = b.Id
    ' + @BillingHeadJoin + N'
    LEFT JOIN OC oc ON oc.BookingId = b.Id
    LEFT JOIN RS rs ON rs.BookingId = b.Id
    WHERE b.BranchID = @BranchID
      AND CAST(bp.PaidOn AS DATE) BETWEEN @FromDate AND @ToDate
      AND bp.Status IN (''Completed'',''Captured'',''Success'')
      AND ISNULL(b.Status,'''') <> ''Cancelled''
    ORDER BY bp.PaidOn DESC, bp.Id DESC;';

    EXEC sp_executesql
        @SqlDetails,
        N'@BranchID INT, @FromDate DATE, @ToDate DATE',
        @BranchID, @FromDate, @ToDate;
END;
GO

PRINT 'sp_GetGstReport – fully corrected and stable';
GO
