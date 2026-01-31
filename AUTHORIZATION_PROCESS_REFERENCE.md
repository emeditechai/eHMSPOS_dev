# Authorization Process (Dynamic Menu + Role Mapping)

Created: 2026-01-31

## Goal

Move the navbar menu from hardcoded Razor to dynamic server-side rendering driven by database tables, and provide a UI to map `Roles` → menu/sub-menu items.

## Database

### Tables

- `NavMenuItems`
  - Stores menu items in a parent/child hierarchy.
  - Parent rows have `Controller=NULL` and `Action=NULL`.
  - Child rows have `Controller` + `Action` and appear in the dropdown.

- `RoleNavMenuItems`
  - Many-to-many mapping between `Roles` and `NavMenuItems`.

### Scripts

- `Database/Scripts/87_CreateNavMenuAuthorizationTables.sql`
- `Database/Scripts/88_SeedNavMenuItems_FromExistingLayout.sql`

Run these against your DB to create and seed the menu.

## Server-side Navbar Rendering

- The hardcoded menu in `HotelApp.Web/Views/Shared/_Layout.cshtml` is replaced with a ViewComponent.
- ViewComponent: `HotelApp.Web/ViewComponents/NavBarMenuViewComponent.cs`
- View: `HotelApp.Web/Views/Shared/Components/NavBarMenu/Default.cshtml`

### Admin behavior

- If the logged-in username is `Admin` (case-insensitive), all active menus are returned (no role filtering).

### Non-admin behavior

- Menus are filtered by user roles (`UserRoles`) and role-menu mappings (`RoleNavMenuItems`).
- Parents are auto-included if any child is allowed.

## Role ↔ Menu Mapping UI

- Page: `/Authorization/RoleMenuMapping`
- Controller: `HotelApp.Web/Controllers/AuthorizationController.cs`
- Views:
  - `HotelApp.Web/Views/Authorization/RoleMenuMapping.cshtml`
  - `HotelApp.Web/Views/Authorization/_MenuTree.cshtml`

Access is currently restricted to `Admin`.

## Next Steps (for full authorization)

- Add controller/action authorization checks (beyond menu visibility).
- Add a policy/attribute that validates page access against the same `RoleNavMenuItems` mapping.
- Optionally add branch-level scoping for menu visibility.
