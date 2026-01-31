-- =============================================
-- Add Authorization Matrix menu under Settings
-- Created: 2026-01-31
-- Notes:
--   - Safe to run multiple times
-- =============================================

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('SETTINGS', 'Settings', 'fas fa-cog', NULL, NULL, NULL, 70, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'AUTHORIZATION_MATRIX')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('AUTHORIZATION_MATRIX', 'Authorization Matrix', 'fas fa-shield-alt', 'AuthorizationMatrix', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 76, 1);
END
GO
