# Authorization Matrix (Page + Button Visibility)

Created: 2026-01-31

## What this adds

A standard, configurable authorization layer that supports:

- Page-wise access control (Controller/Action)
- Button/UI element visibility control (any HTML element)
- Mapping can be maintained:
  - Role-wise
  - User-wise (overrides role)
  - Branch-wise (optional branch scope)

Admin user bypass:
- Username `Admin` can access everything and sees all UI.

## Database

Run these scripts:

1) Create tables:
- Database/Scripts/89_CreateAuthorizationMatrixTables.sql

2) Seed resources from your NavMenu:
- Database/Scripts/90_SeedAuthorizationResources_FromNavMenu.sql

### Tables

- `AuthorizationResources`
  - ResourceType: `Group` | `Page` | `Ui`
  - ResourceKey examples:
    - `GROUP:ROOMS`
    - `PAGE:Booking.List`
    - `UI:Booking.Create.Save`

- `AuthorizationRolePermissions`
- `AuthorizationUserPermissions`

## Runtime enforcement

- Global filter: blocks page access when a `Page` resource exists and permissions evaluate to Deny.
- Default behavior if a resource has no rules: **Allow** (to avoid breaking pages until you configure rules).

## UI

Admin-only:
- `/AuthorizationMatrix/Index`

Use it to:
- Choose Scope: Role/User
- Choose Branch: Global or a specific branch
- Allow/Deny/Inherit per resource
- Add UI keys for button control

## Button visibility

Add attribute on any Razor element:

- `auth-key="UI:Booking.Create.Save"`

If denied, the element is not rendered.

## Next hardening steps

- Switch default from Allow → Deny after all pages are registered.
- Add bulk seed for common UI keys (Save/Delete/Approve) per page.
