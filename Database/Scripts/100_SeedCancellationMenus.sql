-- =============================================
-- Seed Cancellation Policies + Cancellation Register menu items
-- Created: 2026-02-08
-- =============================================

-- Ensure parent menus exist (some environments may not have been seeded yet)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('SETTINGS', 'Settings', 'fas fa-cog', NULL, NULL, NULL, 70, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS', 'Reports', 'fas fa-chart-bar', NULL, NULL, NULL, 60, 1);
END
GO

-- Cancellation Policies under SETTINGS
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_CANCELLATION_POLICIES')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'SETTINGS_CANCELLATION_POLICIES',
        'Cancellation Policies',
        'fas fa-ban',
        'CancellationPolicy',
        'Index',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'SETTINGS'),
        77,
        1
    );
END
GO

-- Cancellation Register under REPORTS
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_CANCELLATION_REGISTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'REPORTS_CANCELLATION_REGISTER',
        'Cancellation Register',
        'fas fa-receipt',
        'Reports',
        'CancellationRegister',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'REPORTS'),
        69,
        1
    );
END
GO
