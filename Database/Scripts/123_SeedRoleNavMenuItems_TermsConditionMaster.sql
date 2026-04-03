-- Grant Terms & Conditions Master access to Administrator and Manager

USE HMS_dev;
GO

IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
    AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES ((SELECT Id FROM Roles WHERE Name = 'Administrator'), (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER'), 1);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM RoleNavMenuItems
    WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Manager')
    AND NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER')
)
BEGIN
    INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
    VALUES ((SELECT Id FROM Roles WHERE Name = 'Manager'), (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER'), 1);
END
GO