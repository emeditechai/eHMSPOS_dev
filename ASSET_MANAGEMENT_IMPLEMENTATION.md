# Asset Management Module (Internal Inventory)

## Scope

**In-scope**
- Asset/Reusable/Consumable item masters
- Department + Unit masters
- Consumable standards (Per Room/Day + Per Stay)
- Stock movements (IN/OUT) with audit trail
- Allocations to Department / Room / Guest via movements
- Damage/Loss log, approval, and recovery register

**Out-of-scope**
- Procurement / Purchase orders
- Vendor management

## Database

Run scripts in order:
- `Database/Scripts/80_CreateAssetManagementTables.sql`
- `Database/Scripts/81_SeedAssetManagementMasters.sql`

Tables:
- `AssetDepartments`, `AssetUnits`, `AssetItems`, `AssetItemDepartments`
- `AssetConsumableStandards`
- `AssetStockBalances`
- `AssetMovements`, `AssetMovementLines`
- `AssetAllocations`
- `AssetDamageLoss`, `AssetDamageLossRecoveries`

## UI Navigation

Top navbar → **Asset Management**
- Overview
- Item Master
- Departments
- Units
- Consumable Standards
- Stock Movement
- Movement Audit
- Damage/Loss
- Stock Report

## Stock Rules

- Stock is maintained in `AssetStockBalances` per Branch + Item.
- `CreateMovement` enforces:
  - Qty must be > 0
  - Non-negative stock (blocked if it would go below 0)
  - Exception: **Consumables** can go negative only when **Admin override** is enabled.

## Movement Types

Supported types (from `AssetMovementType`):
- `OpeningStockIn`, `ReturnIn`, `DamageRecoveryIn`
- `DepartmentIssueOut`, `RoomAllocationOut`, `GuestIssueOut`, `ConsumableUsageOut`, `AutoCheckoutConsumableOut`

## Damage / Loss Workflow

- Staff logs a record (default status: `Pending`)
- Manager/Admin can approve (status: `Approved`)
- Recoveries can be recorded (Cash / BillPosting / Replacement / StaffDeduction)

> Note: bill posting to guest ledger is planned as the next enhancement (will insert into booking other charges).

## Code Locations

- Repository interface: `HotelApp.Web/Repositories/IAssetManagementRepository.cs`
- Repository implementation: `HotelApp.Web/Repositories/AssetManagementRepository.cs`
- Controller: `HotelApp.Web/Controllers/AssetManagementController.cs`
- Views: `HotelApp.Web/Views/AssetManagement/*`
- Models/ViewModels:
  - `HotelApp.Web/Models/AssetManagement.cs`
  - `HotelApp.Web/ViewModels/AssetManagementViewModels.cs`
