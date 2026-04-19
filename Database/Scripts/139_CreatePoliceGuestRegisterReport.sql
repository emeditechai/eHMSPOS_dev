-- Migration 139: Create Police/Guest Register Report SP + Nav Menu
-- Government-compliant Police / Guest Register format (India)
-- =============================================

-- 1. Create stored procedure for Police/Guest Register report
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'sp_GetPoliceGuestRegister') AND type = 'P')
    DROP PROCEDURE sp_GetPoliceGuestRegister;
GO

CREATE PROCEDURE sp_GetPoliceGuestRegister
    @BranchID       INT,
    @FromDate       DATETIME,
    @ToDate         DATETIME,
    @RoomNumber     NVARCHAR(20)  = NULL,
    @Nationality    NVARCHAR(100) = NULL,
    @GuestName      NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Summary
    -- Only records with Room assigned AND ID proof provided
    SELECT
        COUNT(DISTINCT bg.Id)                                               AS TotalGuests,
        COUNT(DISTINCT b.Id)                                                AS TotalBookings,
        SUM(CASE WHEN n.Name IS NOT NULL AND n.Name <> 'Indian' THEN 1 ELSE 0 END) AS ForeignGuests,
        SUM(CASE WHEN n.Name IS NULL OR n.Name = 'Indian' THEN 1 ELSE 0 END)       AS IndianGuests
    FROM Bookings b
    INNER JOIN BookingGuests bg ON bg.BookingId = b.Id AND bg.IsActive = 1
    INNER JOIN Rooms r          ON r.Id = b.RoomId
    LEFT  JOIN Nationalities n  ON n.Id = bg.NationalityId
    LEFT  JOIN Guests g         ON g.Id = bg.GuestId
    WHERE b.BranchID = @BranchID
      AND b.Status NOT IN ('Cancelled')
      AND b.RoomId IS NOT NULL
      AND COALESCE(g.IdentityType, bg.IdentityType, '') <> ''
      AND COALESCE(g.IdentityNumber, bg.IdentityNumber, '') <> ''
      AND b.CheckInDate <= @ToDate
      AND b.CheckOutDate >= @FromDate
      AND (@RoomNumber IS NULL OR r.RoomNumber = @RoomNumber)
      AND (@Nationality IS NULL OR n.Name = @Nationality)
      AND (@GuestName IS NULL 
           OR bg.FullName LIKE '%' + @GuestName + '%'
           OR g.FirstName LIKE '%' + @GuestName + '%'
           OR g.LastName  LIKE '%' + @GuestName + '%');

    -- Result Set 2: Detail rows
    -- Only records with Room assigned AND ID proof provided
    SELECT
        ROW_NUMBER() OVER (ORDER BY b.CheckInDate, bg.Id)            AS SlNo,
        COALESCE(bg.FullName, g.FirstName + ' ' + g.LastName, '')    AS GuestName,
        COALESCE(bg.GuestType, 'Primary')                            AS GuestType,
        COALESCE(g.Gender, '')                                       AS Gender,
        COALESCE(g.Age, CASE WHEN g.DateOfBirth IS NOT NULL 
            THEN DATEDIFF(YEAR, g.DateOfBirth, GETDATE()) ELSE NULL END) AS Age,
        COALESCE(n.Name, 'Indian')                                   AS Nationality,
        COALESCE(g.Address, bg.Address, '')                          AS Address,
        COALESCE(g.City, bg.City, '')                                AS City,
        COALESCE(g.State, bg.State, '')                              AS [State],
        COALESCE(g.Country, bg.Country, '')                          AS Country,
        COALESCE(g.Pincode, bg.Pincode, '')                          AS Pincode,
        COALESCE(g.IdentityType, bg.IdentityType, '')               AS IdType,
        COALESCE(g.IdentityNumber, bg.IdentityNumber, '')            AS IdNumber,
        b.CheckInDate                                                AS CheckInDate,
        b.ActualCheckInDate                                          AS ActualCheckInDate,
        b.CheckOutDate                                               AS CheckOutDate,
        b.ActualCheckOutDate                                         AS ActualCheckOutDate,
        r.RoomNumber                                                 AS RoomNumber,
        b.Adults + b.Children                                        AS NumberOfGuests,
        COALESCE(g.PurposeOfVisit, bg.PurposeOfVisit, '')           AS PurposeOfVisit,
        COALESCE(bg.Phone, g.Phone, '')                              AS ContactNumber,
        COALESCE(g.ComingFrom, bg.ComingFrom, '')                    AS ComingFrom,
        COALESCE(g.GoingTo, bg.GoingTo, '')                          AS GoingTo,
        b.BookingNumber                                              AS BookingNumber,
        b.Status                                                     AS BookingStatus,
        CASE WHEN n.Name IS NOT NULL AND n.Name <> 'Indian' THEN 1 ELSE 0 END AS IsForeignGuest,
        bg.Email                                                     AS Email
    FROM Bookings b
    INNER JOIN BookingGuests bg ON bg.BookingId = b.Id AND bg.IsActive = 1
    INNER JOIN Rooms r          ON r.Id = b.RoomId
    LEFT  JOIN Nationalities n  ON n.Id = bg.NationalityId
    LEFT  JOIN Guests g         ON g.Id = bg.GuestId
    WHERE b.BranchID = @BranchID
      AND b.Status NOT IN ('Cancelled')
      AND b.RoomId IS NOT NULL
      AND COALESCE(g.IdentityType, bg.IdentityType, '') <> ''
      AND COALESCE(g.IdentityNumber, bg.IdentityNumber, '') <> ''
      AND b.CheckInDate <= @ToDate
      AND b.CheckOutDate >= @FromDate
      AND (@RoomNumber IS NULL OR r.RoomNumber = @RoomNumber)
      AND (@Nationality IS NULL OR n.Name = @Nationality)
      AND (@GuestName IS NULL 
           OR bg.FullName LIKE '%' + @GuestName + '%'
           OR g.FirstName LIKE '%' + @GuestName + '%'
           OR g.LastName  LIKE '%' + @GuestName + '%')
    ORDER BY b.CheckInDate, bg.Id;
END
GO

PRINT 'Created sp_GetPoliceGuestRegister stored procedure.';
GO

-- 2. Add navigation menu item under Reports
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_POLICE_GUEST_REGISTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'REPORTS_POLICE_GUEST_REGISTER',
        'Police/Guest Register',
        'fas fa-shield-alt',
        'Reports',
        'PoliceGuestRegister',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'REPORTS'),
        71,
        1
    );
    PRINT 'Inserted REPORTS_POLICE_GUEST_REGISTER nav menu item.';
END
ELSE
    PRINT 'REPORTS_POLICE_GUEST_REGISTER nav menu item already exists.';
GO

PRINT 'Migration 139 complete.';
