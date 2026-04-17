-- =============================================
-- Script: 132_SeedNavMenuItem_RoleDashboardConfig.sql
-- Description: Adds "Role Dashboard Config" under the Settings
--              nav group and grants access to Administrator role only.
-- Created: 2026-04-17
-- =============================================

USE HMS_dev;
GO

-- Insert NavMenuItem (under SETTINGS parent)
IF NOT EXISTS (
    SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_ROLE_DASHBOARD'
)
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'SETTINGS_ROLE_DASHBOARD',
        'Role Dashboard Config',
        'fas fa-tachometer-alt',
        'RoleDashboardConfig',
        'Index',
        (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'),
        76,
        1
    );
    PRINT 'NavMenuItem SETTINGS_ROLE_DASHBOARD inserted.';
END
ELSE
    PRINT 'NavMenuItem SETTINGS_ROLE_DASHBOARD already exists.';
GO

-- Grant access to Administrator
IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId        = (SELECT Id FROM Roles WHERE Name = 'Administrator')
      AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_ROLE_DASHBOARD')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES (
        (SELECT Id FROM Roles WHERE Name = 'Administrator'),
        (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_ROLE_DASHBOARD'),
        1
    );
    PRINT 'RoleNavMenuItems: Administrator -> SETTINGS_ROLE_DASHBOARD inserted.';
END
ELSE
    PRINT 'RoleNavMenuItems: Administrator -> SETTINGS_ROLE_DASHBOARD already exists.';
GO
