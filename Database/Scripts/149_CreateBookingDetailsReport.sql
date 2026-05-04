SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- Script  : 149_CreateBookingDetailsReport.sql
-- Purpose : Create sp_GetBookingDetailsReport stored procedure
--           and seed nav menu item "Booking Details Report"
--           under the REPORTS parent menu.
--
-- Report Layout (3 result sets):
--   RS1 – Summary  : TotalBookings, TotalBillAmount, TotalPaid, TotalDue
--   RS2 – Headers  : One row per booking (BookingNo, dates, guest,
--                    rooms, room types, B2C/B2B, amounts, status)
--   RS3 – Details  : One row per billing-line detail
--                    (Stay / Other Charges / Room Service)
--
-- Filters:
--   @FromDate / @ToDate   : CheckInDate range (defaults to today)
--   @BookingType          : NULL = All, 'B2C', 'B2B'
--   @Status               : NULL = All; e.g. 'Confirmed','CheckedIn','CheckedOut'
-- ============================================================

USE HMS_dev;
GO

-- ── Drop + Recreate SP ──────────────────────────────────────
IF OBJECT_ID('dbo.sp_GetBookingDetailsReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBookingDetailsReport;
GO

CREATE PROCEDURE dbo.sp_GetBookingDetailsReport
(
    @BranchID    INT,
    @FromDate    DATE         = NULL,
    @ToDate      DATE         = NULL,
    @BookingType NVARCHAR(10) = NULL,   -- NULL=All | 'B2C' | 'B2B'
    @Status      NVARCHAR(50) = NULL    -- NULL=All | 'Confirmed' | 'CheckedIn' | 'CheckedOut' | ...
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Default date range to today
    IF @FromDate IS NULL SET @FromDate = CAST(GETDATE() AS DATE);
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);
    IF @ToDate < @FromDate
    BEGIN
        DECLARE @TmpDate DATE = @FromDate;
        SET @FromDate = @ToDate;
        SET @ToDate   = @TmpDate;
    END;

    -- ── Working table: bookings in scope ──────────────────────
    CREATE TABLE #B (
        Id              INT NOT NULL PRIMARY KEY,
        BookingNumber   NVARCHAR(25)  NOT NULL,
        CheckInDate     DATE          NOT NULL,
        CheckOutDate    DATE          NOT NULL,
        Nights          INT           NOT NULL,
        PrimaryGuest    NVARCHAR(210) NOT NULL,
        GuestCount      INT           NOT NULL,   -- Adults + Children
        BookingType     NVARCHAR(5)   NOT NULL,   -- 'B2B' / 'B2C'
        B2BClientName   NVARCHAR(200) NULL,
        StayBillAmount  DECIMAL(12,2) NOT NULL,   -- b.TotalAmount (stay only)
        TotalPaid       DECIMAL(12,2) NOT NULL,   -- sum of all payments
        Status          NVARCHAR(50)  NOT NULL,
        PaymentStatus   NVARCHAR(50)  NOT NULL
    );

    INSERT INTO #B
    SELECT
        b.Id,
        b.BookingNumber,
        b.CheckInDate,
        b.CheckOutDate,
        b.Nights,
        LTRIM(RTRIM(b.PrimaryGuestFirstName + ' ' + b.PrimaryGuestLastName)) AS PrimaryGuest,
        ISNULL(b.Adults, 1) + ISNULL(b.Children, 0)                         AS GuestCount,
        CASE WHEN b.B2BClientId IS NOT NULL OR ISNULL(b.CustomerType,'') = 'B2B'
             THEN 'B2B' ELSE 'B2C' END                                       AS BookingType,
        ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(bc.ClientName, b.B2BClientName))),''), NULL)
                                                                             AS B2BClientName,
        b.TotalAmount                                                        AS StayBillAmount,
        ISNULL((
            SELECT SUM(bp.Amount)
            FROM   dbo.BookingPayments bp
            WHERE  bp.BookingId = b.Id
              AND  bp.Status IN ('Completed','Captured','Success')
              AND  ISNULL(bp.IsRefund,0) = 0
        ), 0)                                                                AS TotalPaid,
        b.Status,
        b.PaymentStatus
    FROM dbo.Bookings b
    LEFT JOIN dbo.B2BClients bc ON bc.Id = b.B2BClientId
    WHERE b.BranchID = @BranchID
      AND CAST(b.CheckInDate AS DATE) BETWEEN @FromDate AND @ToDate
      AND (@Status      IS NULL OR b.Status      = @Status)
      AND (@BookingType IS NULL
           OR (@BookingType = 'B2B' AND (b.B2BClientId IS NOT NULL OR ISNULL(b.CustomerType,'') = 'B2B'))
           OR (@BookingType = 'B2C' AND  b.B2BClientId IS NULL     AND ISNULL(b.CustomerType,'') <> 'B2B')
          );

    -- ── Rooms assigned per booking (comma-sep) ────────────────
    -- Use STUFF/FOR XML to concatenate room numbers and types
    ;WITH RoomData AS (
        SELECT
            br.BookingId,
            r.RoomNumber,
            rt.TypeName AS RoomTypeName
        FROM dbo.BookingRooms br
        INNER JOIN dbo.Rooms       r  ON r.Id  = br.RoomId
        INNER JOIN dbo.RoomTypes   rt ON rt.Id = r.RoomTypeId
        WHERE br.IsActive = 1
          AND br.BookingId IN (SELECT Id FROM #B)
    ),
    -- Also include the primary room from Bookings.RoomId if not in BookingRooms
    PrimaryRoom AS (
        SELECT
            b.Id AS BookingId,
            r.RoomNumber,
            rt.TypeName AS RoomTypeName
        FROM #B b
        INNER JOIN dbo.Bookings bk ON bk.Id = b.Id
        INNER JOIN dbo.Rooms    r  ON r.Id  = bk.RoomId
        INNER JOIN dbo.RoomTypes rt ON rt.Id = r.RoomTypeId
        WHERE bk.RoomId IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM dbo.BookingRooms br2
              WHERE br2.BookingId = b.Id AND br2.RoomId = bk.RoomId AND br2.IsActive = 1
          )
    ),
    AllRooms AS (
        SELECT BookingId, RoomNumber, RoomTypeName FROM RoomData
        UNION
        SELECT BookingId, RoomNumber, RoomTypeName FROM PrimaryRoom
    )
    SELECT DISTINCT BookingId, RoomNumber, RoomTypeName
    INTO #Rooms
    FROM AllRooms;

    -- ── OC totals per booking ─────────────────────────────────
    SELECT
        oc.BookingId,
        SUM(oc.Rate * ISNULL(oc.Qty,1))  AS OCBaseAmount,
        SUM(oc.GSTAmount)                 AS OCGSTAmount,
        SUM(oc.Rate * ISNULL(oc.Qty,1) + oc.GSTAmount) AS OCTotalAmount
    INTO #OCTotals
    FROM dbo.BookingOtherCharges oc
    WHERE oc.BookingId IN (SELECT Id FROM #B)
    GROUP BY oc.BookingId;

    -- ── Room Services totals per booking ──────────────────────
    SELECT
        rs.BookingID AS BookingId,
        SUM(rs.NetAmount)  AS RSBaseAmount,
        SUM(rs.GSTAmount)  AS RSGSTAmount,
        SUM(rs.ActualBillAmount) AS RSTotalAmount
    INTO #RSTotals
    FROM dbo.RoomServices rs
    WHERE rs.BookingID IN (SELECT Id FROM #B)
    GROUP BY rs.BookingID;

    -- ─────────────────────────────────────────────────────────
    -- RS 1: Summary
    -- ─────────────────────────────────────────────────────────
    SELECT
        COUNT(*)                                                              AS TotalBookings,
        ROUND(SUM(
            b.StayBillAmount
            + ISNULL(oc.OCTotalAmount, 0)
            + ISNULL(rs.RSTotalAmount, 0)
        ), 2)                                                                 AS TotalBillAmount,
        ROUND(SUM(b.TotalPaid), 2)                                            AS TotalPaidAmount,
        ROUND(SUM(
            CASE WHEN b.Status = 'Cancelled' THEN 0
                 ELSE b.StayBillAmount
                    + ISNULL(oc.OCTotalAmount, 0)
                    + ISNULL(rs.RSTotalAmount, 0)
                    - b.TotalPaid
            END
        ), 2)                                                                 AS TotalDueAmount
    FROM #B b
    LEFT JOIN #OCTotals oc ON oc.BookingId = b.Id
    LEFT JOIN #RSTotals rs ON rs.BookingId = b.Id;

    -- ─────────────────────────────────────────────────────────
    -- RS 2: Booking Headers
    -- ─────────────────────────────────────────────────────────
    SELECT
        b.Id                           AS BookingId,
        b.BookingNumber,
        b.CheckInDate,
        b.CheckOutDate,
        b.Nights,
        b.PrimaryGuest                 AS PrimaryGuestName,
        b.GuestCount                   AS IGuest,
        -- Comma-separated room numbers
        ISNULL(STUFF((
            SELECT DISTINCT ', ' + r2.RoomNumber
            FROM   #Rooms r2
            WHERE  r2.BookingId = b.Id
            FOR XML PATH(''), TYPE
        ).value('.','NVARCHAR(MAX)'), 1, 2, ''), 'N/A')
                                       AS RoomsAssigned,
        -- Comma-separated room type names (distinct)
        ISNULL(STUFF((
            SELECT DISTINCT ', ' + r3.RoomTypeName
            FROM   #Rooms r3
            WHERE  r3.BookingId = b.Id
            FOR XML PATH(''), TYPE
        ).value('.','NVARCHAR(MAX)'), 1, 2, ''), 'N/A')
                                       AS RoomTypes,
        b.BookingType,
        b.B2BClientName,
        -- Total Bill = Stay + OC + RS
        ROUND(
            b.StayBillAmount
            + ISNULL(oc.OCTotalAmount, 0)
            + ISNULL(rs.RSTotalAmount, 0)
        , 2)                           AS TotalBillAmount,
        ROUND(b.TotalPaid, 2)          AS TotalPaid,
        -- DueAmount: 0 for cancelled bookings (adjusted per cancellation policy)
        CASE WHEN b.Status = 'Cancelled' THEN CAST(0 AS DECIMAL(12,2))
             ELSE ROUND(
                b.StayBillAmount
                + ISNULL(oc.OCTotalAmount, 0)
                + ISNULL(rs.RSTotalAmount, 0)
                - b.TotalPaid, 2)
        END                            AS DueAmount,
        -- Cancellation fields (NULL for non-cancelled bookings)
        ROUND(ISNULL(canc.DeductionAmount, 0), 2)  AS CancellationDeduction,
        ROUND(ISNULL(canc.RefundAmount, 0), 2)      AS CancellationRefundAmount,
        ISNULL(canc.ApprovalStatus, '')             AS RefundApprovalStatus,
        CAST(ISNULL(canc.IsRefunded, 0) AS BIT)    AS IsRefunded,
        b.Status,
        b.PaymentStatus
    FROM #B b
    LEFT JOIN #OCTotals oc ON oc.BookingId = b.Id
    LEFT JOIN #RSTotals rs ON rs.BookingId = b.Id
    LEFT JOIN dbo.BookingCancellations canc ON canc.BookingId = b.Id
    ORDER BY b.CheckInDate, b.BookingNumber;

    -- ─────────────────────────────────────────────────────────
    -- RS 3: Billing-Head Summary per Booking
    --   One row per billing head that has data:
    --   'Stay' | 'OtherCharge' | 'RoomService'
    -- ─────────────────────────────────────────────────────────

    -- Stay Charges (always exists — one row per booking)
    SELECT
        bk.Id                                              AS BookingId,
        1                                                  AS SortOrder,
        'Stay'                                             AS LineType,
        CAST('Stay Charges'    AS NVARCHAR(200))           AS Description,
        CAST(bk.Nights AS DECIMAL(12,2))                   AS Qty,
        CASE WHEN bk.Nights > 0
             THEN ROUND(bk2.BaseAmount / bk.Nights, 2)
             ELSE bk2.BaseAmount END                       AS Rate,
        bk2.BaseAmount                                     AS BaseAmount,
        bk2.TaxAmount                                      AS GSTAmount,
        bk2.TotalAmount                                    AS TotalAmount
    FROM #B bk
    INNER JOIN dbo.Bookings bk2 ON bk2.Id = bk.Id

    UNION ALL

    -- Other Charges — one aggregated row per booking
    SELECT
        oc.BookingId,
        2                                                  AS SortOrder,
        'OtherCharge'                                      AS LineType,
        CAST('Other Charges'   AS NVARCHAR(200))           AS Description,
        CAST(SUM(ISNULL(oc.Qty,1)) AS DECIMAL(12,2))       AS Qty,
        0                                                  AS Rate,
        SUM(oc.Rate * ISNULL(oc.Qty,1))                    AS BaseAmount,
        SUM(oc.GSTAmount)                                  AS GSTAmount,
        SUM(oc.Rate * ISNULL(oc.Qty,1) + oc.GSTAmount)    AS TotalAmount
    FROM dbo.BookingOtherCharges oc
    WHERE oc.BookingId IN (SELECT Id FROM #B)
    GROUP BY oc.BookingId

    UNION ALL

    -- Room Services — one aggregated row per booking
    SELECT
        rs.BookingID                                       AS BookingId,
        3                                                  AS SortOrder,
        'RoomService'                                      AS LineType,
        CAST('Room Service'    AS NVARCHAR(200))           AS Description,
        CAST(SUM(ISNULL(rs.Qty,1)) AS DECIMAL(12,2))       AS Qty,
        0                                                  AS Rate,
        SUM(rs.NetAmount)                                  AS BaseAmount,
        SUM(rs.GSTAmount)                                  AS GSTAmount,
        SUM(rs.ActualBillAmount)                           AS TotalAmount
    FROM dbo.RoomServices rs
    WHERE rs.BookingID IN (SELECT Id FROM #B)
    GROUP BY rs.BookingID

    ORDER BY BookingId, SortOrder;

    -- ─────────────────────────────────────────────────────────
    -- RS 4: Drill-Down — Item-level lines per Booking
    --   Stay: one row per booking (same as RS3 Stay)
    --   OtherCharge: one row per OC item
    --   RoomService: one row per RS item
    -- ─────────────────────────────────────────────────────────

    -- Stay (one row per booking — same as billing head)
    SELECT
        bk.Id                                              AS BookingId,
        1                                                  AS SortOrder,
        'Stay'                                             AS LineType,
        CAST('Stay Charges'    AS NVARCHAR(200))           AS Description,
        CAST(bk.Nights AS DECIMAL(12,2))                   AS Qty,
        CASE WHEN bk.Nights > 0
             THEN ROUND(bk2.BaseAmount / bk.Nights, 2)
             ELSE bk2.BaseAmount END                       AS Rate,
        bk2.BaseAmount                                     AS BaseAmount,
        bk2.TaxAmount                                      AS GSTAmount,
        bk2.TotalAmount                                    AS TotalAmount
    FROM #B bk
    INNER JOIN dbo.Bookings bk2 ON bk2.Id = bk.Id

    UNION ALL

    -- Other Charges — individual item rows
    SELECT
        oc.BookingId,
        2                                                  AS SortOrder,
        'OtherCharge'                                      AS LineType,
        CAST(och.[Name] AS NVARCHAR(200))                  AS Description,
        CAST(ISNULL(oc.Qty,1) AS DECIMAL(12,2))            AS Qty,
        oc.Rate                                             AS Rate,
        oc.Rate * ISNULL(oc.Qty,1)                         AS BaseAmount,
        oc.GSTAmount                                        AS GSTAmount,
        oc.Rate * ISNULL(oc.Qty,1) + oc.GSTAmount          AS TotalAmount
    FROM dbo.BookingOtherCharges oc
    INNER JOIN dbo.OtherCharges och ON och.Id = oc.OtherChargeId
    WHERE oc.BookingId IN (SELECT Id FROM #B)

    UNION ALL

    -- Room Services — individual item rows
    SELECT
        rs.BookingID                                       AS BookingId,
        3                                                  AS SortOrder,
        'RoomService'                                      AS LineType,
        CAST(rs.MenuItem AS NVARCHAR(200))                 AS Description,
        CAST(ISNULL(rs.Qty,1) AS DECIMAL(12,2))            AS Qty,
        rs.Price                                            AS Rate,
        rs.NetAmount                                        AS BaseAmount,
        rs.GSTAmount                                        AS GSTAmount,
        rs.ActualBillAmount                                 AS TotalAmount
    FROM dbo.RoomServices rs
    WHERE rs.BookingID IN (SELECT Id FROM #B)

    ORDER BY BookingId, SortOrder, Description;

    -- Cleanup temp tables
    DROP TABLE IF EXISTS #B;
    DROP TABLE IF EXISTS #Rooms;
    DROP TABLE IF EXISTS #OCTotals;
    DROP TABLE IF EXISTS #RSTotals;
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- Seed Nav Menu: REPORTS > Booking Details Report
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS', 'Reports', 'fas fa-chart-bar', NULL, NULL, NULL, 60, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'REPORTS_BOOKING_DETAILS',
        'Booking Details Report',
        'fas fa-file-invoice',
        'Reports',
        'BookingDetailsReport',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'REPORTS'),
        65,
        1
    );
END
GO

-- Grant to Administrator and Manager roles
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
    AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Administrator'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS'),
        1
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Manager')
    AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Manager'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS'),
        1
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Receptionist')
    AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Receptionist'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS_BOOKING_DETAILS'),
        1
    );
END
GO

PRINT '149: sp_GetBookingDetailsReport created and nav menu seeded.';
GO
