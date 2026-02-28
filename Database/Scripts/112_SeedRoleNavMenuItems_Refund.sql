-- =============================================
-- Grant RoleNavMenuItems access for BOOKINGS_REFUND
-- Roles: Administrator, Manager
-- Created: 2026-02-28
-- =============================================

USE HMS_dev;
GO

-- Administrator
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId       = (SELECT Id FROM Roles WHERE Name = 'Administrator')
      AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_REFUND')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Administrator'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_REFUND'),
        1
    );
    PRINT 'RoleNavMenuItems: Administrator -> BOOKINGS_REFUND inserted';
END
ELSE
    PRINT 'RoleNavMenuItems: Administrator -> BOOKINGS_REFUND already exists';
GO

-- Manager
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId       = (SELECT Id FROM Roles WHERE Name = 'Manager')
      AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_REFUND')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Manager'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_REFUND'),
        1
    );
    PRINT 'RoleNavMenuItems: Manager -> BOOKINGS_REFUND inserted';
END
ELSE
    PRINT 'RoleNavMenuItems: Manager -> BOOKINGS_REFUND already exists';
GO
