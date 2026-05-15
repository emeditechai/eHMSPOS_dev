-- ============================================================
-- Script 157: Banquet Report Nav Menu Items + Role Mappings
-- Adds 5 report pages under the existing "Banquet & Events" parent
-- ============================================================
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_REPORT_COLLECTION')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_REPORT_COLLECTION', 'Banquet Collections', 'fas fa-money-bill-wave',
            'BanquetReports', 'CollectionRegister',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 20, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_REPORT_GST')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_REPORT_GST', 'Banquet GST Register', 'fas fa-file-invoice',
            'BanquetReports', 'GSTRegister',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 21, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_REPORT_VENUE')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_REPORT_VENUE', 'Venue Utilization', 'fas fa-chart-bar',
            'BanquetReports', 'VenueUtilization',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 22, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_REPORT_EVENT_PERF')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_REPORT_EVENT_PERF', 'Event Type Performance', 'fas fa-trophy',
            'BanquetReports', 'EventTypePerformance',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 23, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'BANQUET_REPORT_OUTSTANDING')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, [Action], ParentId, SortOrder, IsActive, CreatedDate)
    VALUES ('BANQUET_REPORT_OUTSTANDING', 'Banquet Outstanding', 'fas fa-clock',
            'BanquetReports', 'OutstandingBalance',
            (SELECT Id FROM NavMenuItems WHERE Code = 'BANQUET'), 24, 1, SYSUTCDATETIME());
END
GO

-- ============================================================
-- Role Mappings for report menus
-- ============================================================
DECLARE @ReportCodes TABLE (Code NVARCHAR(100));
INSERT INTO @ReportCodes VALUES
    ('BANQUET_REPORT_COLLECTION'),
    ('BANQUET_REPORT_GST'),
    ('BANQUET_REPORT_VENUE'),
    ('BANQUET_REPORT_EVENT_PERF'),
    ('BANQUET_REPORT_OUTSTANDING');

-- Administrator and Manager get all reports
INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.Id, nmi.Id, 1
FROM Roles r
CROSS JOIN NavMenuItems nmi
INNER JOIN @ReportCodes rc ON rc.Code = nmi.Code
WHERE r.Name IN ('Administrator', 'Manager')
  AND NOT EXISTS (
        SELECT 1 FROM RoleNavMenuItems x
        WHERE x.RoleId = r.Id AND x.NavMenuItemId = nmi.Id
    );

-- Supervisor gets collection and outstanding only
INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT r.Id, nmi.Id, 1
FROM Roles r
CROSS JOIN NavMenuItems nmi
WHERE r.Name = 'Supervisor'
  AND nmi.Code IN ('BANQUET_REPORT_COLLECTION', 'BANQUET_REPORT_OUTSTANDING')
  AND NOT EXISTS (
        SELECT 1 FROM RoleNavMenuItems x
        WHERE x.RoleId = r.Id AND x.NavMenuItemId = nmi.Id
    );

PRINT 'Script 157: Banquet report nav menus seeded.';
GO
