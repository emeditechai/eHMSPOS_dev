-- =============================================
-- Seed Room Service Settle menu item under Bookings
-- Created: 2026-06-05
-- =============================================

USE HMS_dev;
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS_ROOM_SERVICE_SETTLE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'BOOKINGS_ROOM_SERVICE_SETTLE',
        'Room Service Settle',
        'fas fa-concierge-bell',
        'Booking',
        'RoomServiceSettle',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'BOOKINGS'),
        45,
        1
    );
    PRINT 'BOOKINGS_ROOM_SERVICE_SETTLE menu item inserted';
END
ELSE
    PRINT 'BOOKINGS_ROOM_SERVICE_SETTLE menu item already exists';
GO

-- Assign to Administrator role
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
      AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_ROOM_SERVICE_SETTLE')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Administrator'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_ROOM_SERVICE_SETTLE'),
        1
    );
    PRINT 'RoleNavMenuItems: Administrator -> BOOKINGS_ROOM_SERVICE_SETTLE inserted.';
END
ELSE
    PRINT 'RoleNavMenuItems: Administrator -> BOOKINGS_ROOM_SERVICE_SETTLE already exists.';
GO

-- Assign to Manager role
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Manager')
      AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_ROOM_SERVICE_SETTLE')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Manager'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_ROOM_SERVICE_SETTLE'),
        1
    );
    PRINT 'RoleNavMenuItems: Manager -> BOOKINGS_ROOM_SERVICE_SETTLE inserted.';
END
ELSE
    PRINT 'RoleNavMenuItems: Manager -> BOOKINGS_ROOM_SERVICE_SETTLE already exists.';
GO
