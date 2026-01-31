-- =============================================
-- Seed AuthorizationResources from NavMenuItems
-- Created: 2026-01-31
-- Notes:
--   - Creates Group resources for top-level menu parents
--   - Creates Page resources for menu children (Controller+Action)
-- =============================================

-- Insert GROUP resources for parent menus (ParentId is null)
INSERT INTO AuthorizationResources (ResourceType, ResourceKey, Title, ParentResourceId, SortOrder, IsActive)
SELECT
    'Group' as ResourceType,
    'GROUP:' + nmi.Code as ResourceKey,
    nmi.Title as Title,
    NULL as ParentResourceId,
    nmi.SortOrder,
    1
FROM NavMenuItems nmi
WHERE nmi.IsActive = 1
  AND nmi.ParentId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM AuthorizationResources ar WHERE ar.ResourceKey = 'GROUP:' + nmi.Code
  );
GO

-- Insert PAGE resources for child menu items (Controller/Action not null)
INSERT INTO AuthorizationResources (ResourceType, ResourceKey, Title, Controller, Action, ParentResourceId, SortOrder, IsActive)
SELECT
    'Page' as ResourceType,
    'PAGE:' + nmi.Controller + '.' + nmi.Action as ResourceKey,
    nmi.Title as Title,
    nmi.Controller,
    nmi.Action,
    grp.Id as ParentResourceId,
    nmi.SortOrder,
    1
FROM NavMenuItems nmi
LEFT JOIN NavMenuItems parentNmi ON parentNmi.Id = nmi.ParentId
LEFT JOIN AuthorizationResources grp ON grp.ResourceKey = 'GROUP:' + parentNmi.Code
WHERE nmi.IsActive = 1
  AND nmi.Controller IS NOT NULL
  AND nmi.Action IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM AuthorizationResources ar WHERE ar.ResourceKey = 'PAGE:' + nmi.Controller + '.' + nmi.Action
  );
GO
