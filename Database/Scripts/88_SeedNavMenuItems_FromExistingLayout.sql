-- =============================================
-- Seed NavMenuItems using the existing hardcoded layout menu
-- Created: 2026-01-31
-- =============================================

-- Helper: insert parent if missing

-- Dashboard
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('DASHBOARD', 'Dashboard', 'fas fa-home', 'Dashboard', 'Index', NULL, 10, 1);
END
GO

-- Rooms (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ROOMS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ROOMS', 'Rooms', 'fas fa-bed', NULL, NULL, NULL, 20, 1);
END
GO

-- Rooms children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ROOMS_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ROOMS_DASHBOARD', 'Room Dashboard', 'fas fa-th-large', 'Rooms', 'Dashboard', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 21, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ROOM_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ROOM_MASTER', 'Room Master', 'fas fa-door-open', 'RoomMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 22, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'RATE_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('RATE_MASTER', 'Rate Master', 'fas fa-tags', 'RateMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 23, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'FLOOR_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('FLOOR_MASTER', 'Floor Master', 'fas fa-layer-group', 'FloorMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 24, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ROOM_TYPE_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ROOM_TYPE_MASTER', 'Room Type Master', 'fas fa-cube', 'RoomTypeMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 25, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'AMENITIES_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('AMENITIES_MASTER', 'Amenities Master', 'fas fa-concierge-bell', 'AmenitiesMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'ROOMS'), 26, 1);
END
GO

-- Bookings (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('BOOKINGS', 'Bookings', 'fas fa-calendar-check', NULL, NULL, NULL, 30, 1);
END
GO

-- Bookings children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKINGS_LIST')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('BOOKINGS_LIST', 'Bookings', 'fas fa-list', 'Booking', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'), 31, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'GUESTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('GUESTS', 'Guests', 'fas fa-users', 'Guest', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'), 32, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ROOM_AVAILABILITY')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ROOM_AVAILABILITY', 'Room Availability', 'fas fa-calendar-alt', 'Booking', 'RoomAvailabilityCalendar', (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'), 33, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'PAYMENT_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('PAYMENT_DASHBOARD', 'Payment Dashboard', 'fas fa-chart-line', 'Booking', 'PaymentDashboard', (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'), 34, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'GUEST_FEEDBACK')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('GUEST_FEEDBACK', 'Guest Feedback', 'fas fa-comment-dots', 'GuestFeedback', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS'), 35, 1);
END
GO

-- Asset Management (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_MGMT')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_MGMT', 'Asset Management', 'fas fa-boxes-stacked', NULL, NULL, NULL, 40, 1);
END
GO

-- Asset Management children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_OVERVIEW')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_OVERVIEW', 'Overview', 'fas fa-gauge', 'AssetManagement', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 41, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_ITEMS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_ITEMS', 'Item Master', 'fas fa-box', 'AssetManagement', 'Items', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 42, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_MAKERS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_MAKERS', 'Makers', 'fas fa-industry', 'AssetManagement', 'Makers', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 43, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_DEPARTMENTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_DEPARTMENTS', 'Departments', 'fas fa-sitemap', 'AssetManagement', 'Departments', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 44, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_UNITS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_UNITS', 'Units', 'fas fa-ruler', 'AssetManagement', 'Units', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 45, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_CONSUMABLES')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_CONSUMABLES', 'Consumable Standards', 'fas fa-bottle-droplet', 'AssetManagement', 'ConsumableStandards', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 46, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_STOCK_MOVEMENT')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_STOCK_MOVEMENT', 'Stock Movement', 'fas fa-right-left', 'AssetManagement', 'CreateMovement', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 47, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_MOVEMENT_AUDIT')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_MOVEMENT_AUDIT', 'Movement Audit', 'fas fa-list', 'AssetManagement', 'MovementAudit', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 48, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_DAMAGE_LOSS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_DAMAGE_LOSS', 'Damage/Loss', 'fas fa-triangle-exclamation', 'AssetManagement', 'DamageLoss', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 49, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'ASSET_STOCK_REPORT')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('ASSET_STOCK_REPORT', 'Stock Report', 'fas fa-chart-column', 'AssetManagement', 'StockReport', (SELECT Id FROM NavMenuItems WHERE Code = 'ASSET_MGMT'), 50, 1);
END
GO

-- Reports (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS', 'Reports', 'fas fa-chart-bar', NULL, NULL, NULL, 60, 1);
END
GO

-- Reports children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_BA_DASHBOARD')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_BA_DASHBOARD', 'Business Analytics', 'fas fa-chart-line', 'Reports', 'BusinessAnalyticsDashboard', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 61, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_ROOM_TYPE_PERFORMANCE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_ROOM_TYPE_PERFORMANCE', 'Room Type Performance', 'fas fa-layer-group', 'Reports', 'RoomTypePerformance', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 62, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_OUTSTANDING_BALANCES')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_OUTSTANDING_BALANCES', 'Outstanding Balances', 'fas fa-hand-holding-dollar', 'Reports', 'OutstandingBalances', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 63, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_CHANNEL_SOURCE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_CHANNEL_SOURCE', 'Channel/Source Performance', 'fas fa-diagram-project', 'Reports', 'ChannelSourcePerformance', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 64, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_GUEST_DETAILS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_GUEST_DETAILS', 'Guest Details', 'fas fa-user', 'Reports', 'GuestDetails', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 65, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_ROOM_PRICE_DETAILS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_ROOM_PRICE_DETAILS', 'Room Price Details', 'fas fa-rupee-sign', 'Reports', 'RoomPriceDetails', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 66, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_DAILY_COLLECTION')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_DAILY_COLLECTION', 'Daily Collection Register', 'fas fa-cash-register', 'Reports', 'DailyCollectionRegister', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 67, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'REPORTS_GST')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('REPORTS_GST', 'GST Report', 'fas fa-file-invoice', 'Reports', 'GstReport', (SELECT Id FROM NavMenuItems WHERE Code = 'REPORTS'), 68, 1);
END
GO

-- Settings (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('SETTINGS', 'Settings', 'fas fa-cog', NULL, NULL, NULL, 70, 1);
END
GO

-- Settings children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'HOTEL_SETTINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('HOTEL_SETTINGS', 'Hotel Settings', 'fas fa-hotel', 'HotelSettings', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 71, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'MAIL_CONFIGURATION')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('MAIL_CONFIGURATION', 'Mail Configuration', 'fas fa-envelope', 'MailConfiguration', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 72, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BRANCH_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('BRANCH_MASTER', 'Branch Master', 'fas fa-code-branch', 'BranchMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 73, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'USER_MANAGEMENT')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('USER_MANAGEMENT', 'User Management', 'fas fa-users-cog', 'UserManagement', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 74, 1);
END
GO

-- Authorization mapping UI (admin can map roles to menus)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'AUTH_ROLE_MENU_MAPPING')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('AUTH_ROLE_MENU_MAPPING', 'Role Menu Mapping', 'fas fa-user-shield', 'Authorization', 'RoleMenuMapping', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 75, 1);
END
GO

-- Authorization Matrix UI (page + button visibility rules)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'AUTHORIZATION_MATRIX')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('AUTHORIZATION_MATRIX', 'Authorization Matrix', 'fas fa-shield-alt', 'AuthorizationMatrix', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 76, 1);
END
GO

-- Utility (parent)
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'UTILITY')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('UTILITY', 'Utility', 'fas fa-screwdriver-wrench', NULL, NULL, NULL, 80, 1);
END
GO

-- Utility children
IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'OTHER_CHARGES_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('OTHER_CHARGES_MASTER', 'Other Charges Master', 'fas fa-receipt', 'OtherChargesMaster', 'List', (SELECT Id FROM NavMenuItems WHERE Code = 'UTILITY'), 81, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'UPI_SETTINGS')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('UPI_SETTINGS', 'UPI Settings', 'fas fa-qrcode', 'UpiSettings', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'UTILITY'), 82, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BOOKING_RECEIPT_TEMPLATE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('BOOKING_RECEIPT_TEMPLATE', 'Booking Receipt Template Configuration', 'fas fa-file-invoice', 'BookingReceiptTemplate', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'UTILITY'), 83, 1);
END
GO
