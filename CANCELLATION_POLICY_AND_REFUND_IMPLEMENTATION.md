# Cancellation Policy & Refund — Implementation Reference

This document describes the current implementation of **Cancellation Policy and Refund** for the HotelApp (ASP.NET Core MVC + Dapper + SQL scripts).

## Scope

Implemented in this iteration:
- Booking cancellation endpoint is now **fully transactional** (DB update + audit + room release).
- Refund preview calculation based on configured cancellation policy rules.
- Cancellation/refund register entry written to `BookingCancellations`.
- Hotel Settings code updated to match the new stored procedure parameters added in DB script 97.
- Admin CRUD UI for cancellation policies + rules.
- Booking Create flow now captures `RateType` and stores a **policy snapshot** at booking time.
- Cancellation Register report added under Reports.

Pending (next steps):
- Dedicated cancellation preview/confirm UI (modal) and override approval workflow UI.
- No-show automation job and related reports.

## Database objects

Created/updated via scripts under `Database/Scripts/`:

- `94_CreateCancellationPolicyTables.sql`
  - `CancellationPolicies`
  - `CancellationPolicyRules`

- `95_AlterBookings_AddRateTypeAndCancellationPolicySnapshot.sql`
  - Adds booking snapshot columns:
    - `RateType`
    - `CancellationPolicyId`
    - `CancellationPolicySnapshot`
    - `CancellationPolicyAccepted`
    - `CancellationPolicyAcceptedAt`

- `96_CreateBookingCancellationsTable.sql`
  - Creates `BookingCancellations` refund/cancellation register table.

- `97_AlterHotelSettings_AddCancellationAndNoShowSettings.sql`
  - Adds:
    - `NoShowGraceHours`
    - `CancellationRefundApprovalThreshold`
  - Updates stored procedures:
    - `sp_GetHotelSettingsByBranch`
    - `sp_UpsertHotelSettings`

- `98_CreateCancellationRefundFunctions.sql`
  - Adds `dbo.fn_CalculateRefundAmount(...)` for audit/report-side calculation.

- `99_SeedCancellationPolicySampleData.sql`
  - Seeds sample policies and rules.

- `100_SeedCancellationMenus.sql`
  - Adds menu items:
    - Settings → Cancellation Policies
    - Reports → Cancellation Register

- `101_CreateCancellationRegisterReportStoredProcedure.sql`
  - Adds `sp_GetCancellationRegister`.

## Application code wiring

### Booking cancellation transaction

- Repository implementation: `HotelApp.Web/Repositories/BookingRepository.cs`
  - `GetCancellationPreviewAsync(...)`
  - `CancelBookingAsync(...)`

Cancellation does the following in a single DB transaction:
1. Validates booking status (cannot cancel if already cancelled or checked-out).
2. Computes refund preview based on:
   - amount paid (sum of `BookingPayments.Amount`)
   - hotel check-in time (from `HotelSettings.CheckInTime`)
   - hours before check-in
   - matching `CancellationPolicies` + `CancellationPolicyRules`
3. Ensures a policy snapshot is persisted to the booking if missing.
4. Inserts a row in `BookingCancellations`.
5. Releases rooms:
   - marks active `BookingRooms` inactive
   - sets assigned `Rooms.Status = 'Available'`
6. Updates `Bookings.Status = 'Cancelled'`.
7. Writes `BookingAuditLog` entry.

### Controller endpoints

- `POST /Booking/Cancel`
  - Now calls repository transaction to cancel and returns JSON:
    - `success`
    - `message`
    - `refundAmount` (when available)

- `GET /Booking/CancellationPreview?bookingNumber=...`
  - Returns JSON preview:
    - `amountPaid`, `refundAmount`, `hoursBeforeCheckIn`, `approvalStatus`

### UI (Booking list)

- `HotelApp.Web/Views/Booking/List.cshtml`
  - Adds hidden antiforgery token form for the `fetch('/Booking/Cancel')` call.
  - Prompts for an optional cancellation reason.

### Hotel Settings compatibility

Because `97_AlterHotelSettings_AddCancellationAndNoShowSettings.sql` updates the stored procedure signatures, the app code was updated:
- `HotelApp.Web/Models/HotelSettings.cs`
  - Adds `NoShowGraceHours` and `CancellationRefundApprovalThreshold`
- `HotelApp.Web/Repositories/HotelSettingsRepository.cs`
  - Passes new parameters to `sp_UpsertHotelSettings`
- `HotelApp.Web/Views/HotelSettings/Edit.cshtml` and `.../Index.cshtml`
  - Adds fields for the new settings

## Policy admin (CRUD)

- Controller: `HotelApp.Web/Controllers/CancellationPolicyController.cs`
  - `Index`, `Create`, `Edit`
  - `GET /CancellationPolicy/Applicable` returns the applicable policy snapshot JSON for a booking

- Repository: `HotelApp.Web/Repositories/CancellationPolicyRepository.cs`
  - CRUD for `CancellationPolicies` and `CancellationPolicyRules`
  - Snapshot generator to persist to `Bookings.CancellationPolicySnapshot`

- Views:
  - `HotelApp.Web/Views/CancellationPolicy/Index.cshtml`
  - `HotelApp.Web/Views/CancellationPolicy/Create.cshtml`
  - `HotelApp.Web/Views/CancellationPolicy/Edit.cshtml`

## Booking-time policy snapshot + acceptance

- `HotelApp.Web/ViewModels/BookingCreateViewModel.cs`
  - Added `RateType`, `CancellationPolicyAccepted`, and hidden snapshot fields.

- `HotelApp.Web/Controllers/BookingController.cs`
  - Captures `RateType`
  - Fetches applicable policy snapshot and saves it to the booking record
  - Enforces acceptance for website/direct-web bookings

- `HotelApp.Web/Views/Booking/Create.cshtml`
  - Shows a policy panel (loaded via AJAX) + acceptance checkbox.

## Reports

- `GET /Reports/CancellationRegister`
  - Repository call to `sp_GetCancellationRegister`
  - View: `HotelApp.Web/Views/Reports/CancellationRegister.cshtml`

## Refund calculation rules (current)

Refund logic uses the matching policy rule slab based on `HoursBeforeCheckIn`:
- `refundRaw = amountPaid * refundPercent/100`
- `deduction = flatDeduction + amountPaid*gatewayFeePct/100`
- `refundFinal = clamp(refundRaw - deduction, 0, amountPaid)`

Special cases:
- No-show => refund is `0`.
- `RateType` of `NonRefundable` => refund is `0`.

## How to validate

1. Apply DB scripts 94–99 to your database.
2. Run the app.
3. Create a booking with some payments.
4. Use Bookings Dashboard → Cancel.
5. Confirm:
   - booking status becomes Cancelled
   - rooms are released
   - an audit log entry is created
   - `BookingCancellations` row is inserted

---

Owner note: next iteration should add approval workflow screens (approve/reject) and no-show automation.
