-- =============================================
-- Seed Cancellation & Refund Register under Reports navigation
-- Created: 2026-02-28
-- =============================================

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_CANCELLATION_REFUND_REGISTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'REPORTS_CANCELLATION_REFUND_REGISTER',
        'Cancellation & Refund Register',
        'fas fa-file-invoice-dollar',
        'Reports',
        'CancellationRefundRegister',
        (SELECT TOP 1 Id FROM NavMenuItems WHERE Code = 'REPORTS'),
        70,
        1
    );
    PRINT 'REPORTS_CANCELLATION_REFUND_REGISTER menu item inserted';
END
ELSE
    PRINT 'REPORTS_CANCELLATION_REFUND_REGISTER menu item already exists';
GO
