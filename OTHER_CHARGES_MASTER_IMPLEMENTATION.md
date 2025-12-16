# Other Charges Master (Utility)

Last updated: 15 Dec 2025

## What was added

- DB script: `Database/Scripts/40_CreateOtherChargesMaster.sql`
- MVC area (pattern mirrors Amenities Master)
  - Controller: `HotelApp.Web/Controllers/OtherChargesMasterController.cs`
  - Model + Enum: `HotelApp.Web/Models/OtherCharge.cs`
  - Repository: `HotelApp.Web/Repositories/IOtherChargeRepository.cs`, `HotelApp.Web/Repositories/OtherChargeRepository.cs`
  - Views: `HotelApp.Web/Views/OtherChargesMaster/{List,Create,Edit}.cshtml`

## DB schema

Table: `dbo.OtherCharges`

Columns:
- `Code` (unique per `BranchID`)
- `Name`
- `Type` (int; maps to enum `OtherChargeType`)
- `Rate` (decimal)
- `GSTPercent` (decimal)
- `CGSTPercent` (decimal; client-calculated as GST/2, server recalculates)
- `SGSTPercent` (decimal; client-calculated as GST/2, server recalculates)
- `IsActive` (status)
- audit columns + `BranchID`

## Validations / Rules

- Server-side uniqueness: `Code` must be unique within the current branch.
- Tax rule:
  - UI treats GST as a percentage.
  - CGST% and SGST% are auto-calculated as `GST% / 2` on the client.
  - Server recalculates `CGSTPercent` and `SGSTPercent` as a safety net.

## Navigation

- A new top menu section "Utility" contains "Other Charges Master".

## Booking integration (Bookings Dashboard)

- Booking List action dropdown includes "Other Charges".
- Uses a modal to add/edit multiple charges for a booking.
- Modal row removal uses an icon button (not a text "Remove" button).
- Removing an already-saved row shows a confirmation prompt; the delete applies on Save.
- Booking rows support `Qty` and the tax amounts are calculated on `Rate * Qty`.
- Booking rows support per-line `Note` (captured from modal and stored).
- New table stores booking-wise charges (rate, qty, note) and taxes computed from master %.
- DB scripts: `Database/Scripts/41_CreateBookingOtherCharges.sql` (updated), `Database/Scripts/42_AlterBookingOtherCharges_AddQty.sql` (upgrade), `Database/Scripts/43_AlterBookingOtherCharges_AddNote.sql` (upgrade)

## Booking details display

- Booking Details page shows an "Other Charges" section with line items and totals (Qty + Amount).
- Total column is formatted correctly using server-side `ToString("N2")`.

## Financial Summary (Booking Details)

- Booking Details "Financial Summary" now includes the total of Other Charges (inclusive of GST).
- The summary tiles for CGST/SGST/Total GST/Grand Total/Balance Due use adjusted values that include Booking Other Charges.
- This is currently a display-level adjustment on the details page (does not persist back into booking totals).

## Receipt (Booking)

- Booking Receipt now shows an "Other Charges" section below "Stay Summary".
- Receipt totals (CGST/SGST/Total Amount/Balance Payable) include Booking Other Charges (inclusive of GST).

## Activity Timeline

- Booking Other Charges add/update/remove are recorded in the Booking "Activity Timeline" (via `BookingAuditLog`) when saving from the modal.

## Quick sanity

- Apply DB script.
- Run web app and open: `/OtherChargesMaster/List`
