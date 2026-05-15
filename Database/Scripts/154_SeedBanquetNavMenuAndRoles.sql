-- ============================================================
-- Script 154: Banquet Nav Menu Items + Role Mappings
-- Creates a top-level "Banquet & Events" parent nav section
-- with all master and booking pages beneath it.
-- ============================================================
SET NOCOUNT ON;
GO

-- ============================================================
-- SECTION A: Parent Nav Menu - "Banquet & Events"
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET', 'Banquet & Events', 'fas fa-champagne-glasses', NULL, NULL, NULL, 70, 1, SYSUTCDATETIME());
    PRINT 'Inserted BANQUET parent menu';
END
GO

-- ============================================================
-- SECTION B: Child pages under Banquet & Events
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_DASHBOARD', 'Banquet Dashboard', 'fas fa-chart-column',
            'BanquetBooking', 'Index',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 1, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_BOOKING_CREATE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_BOOKING_CREATE', 'New Event Booking', 'fas fa-circle-plus',
            'BanquetBooking', 'Create',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 2, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_BOOKING_LIST')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_BOOKING_LIST', 'Event Bookings', 'fas fa-list-check',
            'BanquetBooking', 'List',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 3, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_AVAILABILITY_CALENDAR')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_AVAILABILITY_CALENDAR', 'Availability Calendar', 'fas fa-calendar-days',
            'BanquetBooking', 'AvailabilityCalendar',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 4, 1, SYSUTCDATETIME());
END
GO

-- Masters sub-section
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_VENUE_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_VENUE_MASTER', 'Venue Master', 'fas fa-building',
            'BanquetVenueMaster', 'List',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 10, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_EVENT_TYPE_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_EVENT_TYPE_MASTER', 'Event Types', 'fas fa-tags',
            'BanquetEventTypeMaster', 'List',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 11, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_PACKAGE_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_PACKAGE_MASTER', 'Menu Packages', 'fas fa-utensils',
            'BanquetPackageMaster', 'List',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 12, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_ADDON_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_ADDON_MASTER', 'Addon Services', 'fas fa-puzzle-piece',
            'BanquetAddonServiceMaster', 'List',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 13, 1, SYSUTCDATETIME());
END
GO

-- ============================================================
-- SECTION C: Role-Menu Mappings
-- ============================================================

-- Codes of all banquet menu items
DECLARE @BanquetCodes TABLE (Code NVARCHAR(100));
INSERT INTO @BanquetCodes VALUES
    ('BANQUET'),
    ('BANQUET_DASHBOARD'),
    ('BANQUET_BOOKING_CREATE'),
    ('BANQUET_BOOKING_LIST'),
    ('BANQUET_AVAILABILITY_CALENDAR'),
    ('BANQUET_VENUE_MASTER'),
    ('BANQUET_EVENT_TYPE_MASTER'),
    ('BANQUET_PACKAGE_MASTER'),
    ('BANQUET_ADDON_MASTER');

-- Roles that get full access
DECLARE @FullAccessRoles TABLE (RoleName NVARCHAR(100));
INSERT INTO @FullAccessRoles VALUES ('Administrator'), ('Manager');

-- Roles that get read-only / operational access (no masters)
DECLARE @OpsRoles TABLE (RoleName NVARCHAR(100));
INSERT INTO @OpsRoles VALUES ('Supervisor');

-- Cashier gets list + payment related (no create/masters)
DECLARE @CashierCodes TABLE (Code NVARCHAR(100));
INSERT INTO @CashierCodes VALUES
    ('BANQUET'),
    ('BANQUET_DASHBOARD'),
    ('BANQUET_BOOKING_LIST'),
    ('BANQUET_AVAILABILITY_CALENDAR');

-- Grant full access to Administrator + Manager
INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.Id, m.Id, 1
FROM Roles r
CROSS JOIN NavMenuItems m
INNER JOIN @FullAccessRoles fr ON r.Name = fr.RoleName
INNER JOIN @BanquetCodes bc    ON m.Code = bc.Code
WHERE NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems rn
    WHERE rn.RoleId = r.Id AND rn.NavMenuItemId = m.Id
);

-- Grant ops codes to Supervisor
INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.Id, m.Id, 1
FROM Roles r
CROSS JOIN NavMenuItems m
INNER JOIN @OpsRoles opr       ON r.Name = opr.RoleName
INNER JOIN @BanquetCodes bc    ON m.Code = bc.Code
WHERE NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems rn
    WHERE rn.RoleId = r.Id AND rn.NavMenuItemId = m.Id
);

-- Grant cashier codes to Cashier
INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.Id, m.Id, 1
FROM Roles r
CROSS JOIN NavMenuItems m
INNER JOIN @CashierCodes cc    ON m.Code = cc.Code
WHERE r.Name = 'Cashier'
  AND NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems rn
    WHERE rn.RoleId = r.Id AND rn.NavMenuItemId = m.Id
);

PRINT 'Script 154 (Nav Menu + Role Mappings) completed successfully.';
GO
