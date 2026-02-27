-- =============================================
-- Seed Refund menu item under Bookings
-- Created: 2026-02-27
-- =============================================

USE HMS_dev;
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS_REFUND')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'BOOKINGS_REFUND',
        'Refunds',
        'fas fa-undo-alt',
        'Refund',
        'Index',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'BOOKINGS'),
        38,
        1
    );
    PRINT 'BOOKINGS_REFUND menu item inserted';
END
ELSE
    PRINT 'BOOKINGS_REFUND menu item already exists';
GO
