USE HMS_dev;
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS_B2B_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES (
        'BOOKINGS_B2B_DASHBOARD',
        'B2B Booking',
        'fas fa-briefcase',
        'B2BBooking',
        'Index',
        (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'),
        36,
        1
    );
END
GO