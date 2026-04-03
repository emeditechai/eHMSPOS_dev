USE HMS_dev;
GO

INSERT INTO RoleNavMenuItems (RoleId, NavMenuItemId, IsActive)
SELECT DISTINCT
    source.RoleId,
    target.Id,
    1
FROM RoleNavMenuItems source
CROSS JOIN (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_B2B_DASHBOARD') target
WHERE source.NavMenuItemId = (SELECT Id FROM NavMenuItems WHERE Code = 'BOOKINGS_LIST')
  AND ISNULL(source.IsActive, 1) = 1
  AND NOT EXISTS (
        SELECT 1
        FROM RoleNavMenuItems existing
        WHERE existing.RoleId = source.RoleId
          AND existing.NavMenuItemId = target.Id
    );
GO