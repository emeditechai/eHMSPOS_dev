# Business Reports (Analytics) Implementation

This document describes the **new business analytics reports** added under the **Reports** navigation.

## What’s Added

### 1) Business Analytics Dashboard
- Menu: **Reports → Business Analytics**
- Page: `ReportsController.BusinessAnalyticsDashboard`
- Filter: Date range (`fromDate`, `toDate`)
- UI:
  - KPI cards: Total Bookings, Room Revenue, ADR, RevPAR, Total Collected, Occupancy %, Outstanding Balance
  - Daily trend table (day-by-day)
  - Payment-method summary table
- Export: CSV / Excel / PDF (client-side export from rendered tables)

**Notes:**
- Room Revenue is based on `BookingRoomNights.RateAmount` within the date range.
- ADR = Room Revenue / Sold room-nights.
- RevPAR = Room Revenue / (Active rooms × total days).

### 2) Room Type Performance
- Menu: **Reports → Room Type Performance**
- Page: `ReportsController.RoomTypePerformance`
- Filter: Date range
- UI:
  - KPI cards: Sold Nights, Revenue
  - Table: Room type wise Sold Nights / Revenue / Avg per night
- Export: CSV / Excel / PDF (client-side)

### 3) Outstanding Balances
- Menu: **Reports → Outstanding Balances**
- Page: `ReportsController.OutstandingBalances`
- Filter: Date range (filters by booking `CheckInDate`)
- UI:
  - KPI cards: Total Bookings, Total Balance
  - Table: Booking-wise outstanding balances
- Export: CSV / Excel / PDF (client-side)

### 4) Channel & Source Performance
- Menu: **Reports → Channel/Source Performance**
- Page: `ReportsController.ChannelSourcePerformance`
- Filter: Date range
- UI:
  - KPI cards: Total Bookings, Sold Nights, Room Revenue, ADR, RevPAR, Occupancy %
  - Table: Channel + Source mix with revenue share
- Export: CSV / Excel / PDF (client-side)

### 5) Guest Details
- Menu: **Reports → Guest Details**
- Page: `ReportsController.GuestDetails`
- Filter: Date range (filters by booking `CheckInDate`)
- UI:
  - KPI cards: Total Guests, Total Bookings, Total Nights, Revenue, Outstanding
  - Table: Guest-wise bookings/nights/revenue with last stay date
- Export: CSV / Excel / PDF (client-side)

## Database (Stored Procedures)

Run the SQL scripts:
- `Database/Scripts/82_CreateBusinessAnalyticsReports.sql`
- `Database/Scripts/83_EnhanceBusinessAnalyticsDashboard_AdrRevpar.sql`
- `Database/Scripts/84_CreateChannelSourcePerformanceReport.sql`
- `Database/Scripts/85_CreateGuestDetailsReport.sql`

It creates these stored procedures:

1. `sp_GetBusinessAnalyticsDashboard`
   - Params: `@BranchID`, `@FromDate`, `@ToDate`
   - Result sets:
     - Summary KPIs
     - Daily Trend
     - Payment Method Summary

4. `sp_GetChannelSourcePerformanceReport`
   - Params: `@BranchID`, `@FromDate`, `@ToDate`
   - Result sets:
     - Summary
     - Channel+Source rows

5. `sp_GetGuestDetailsReport`
   - Params: `@BranchID`, `@FromDate`, `@ToDate`
   - Result sets:
     - Summary
     - Guest rows

2. `sp_GetRoomTypePerformanceReport`
   - Params: `@BranchID`, `@FromDate`, `@ToDate`
   - Result sets:
     - Summary
     - Room-type rows

3. `sp_GetOutstandingBalanceReport`
   - Params: `@BranchID`, `@FromDate`, `@ToDate`
   - Result sets:
     - Summary
     - Booking-wise details

## Code Touchpoints

- Repository:
  - `HotelApp.Web/Repositories/ReportsRepository.cs`

- Controller:
  - `HotelApp.Web/Controllers/ReportsController.cs`

- Views:
  - `HotelApp.Web/Views/Reports/BusinessAnalyticsDashboard.cshtml`
  - `HotelApp.Web/Views/Reports/RoomTypePerformance.cshtml`
  - `HotelApp.Web/Views/Reports/OutstandingBalances.cshtml`
  - `HotelApp.Web/Views/Reports/ChannelSourcePerformance.cshtml`
  - `HotelApp.Web/Views/Reports/GuestDetails.cshtml`

- Navigation:
  - `HotelApp.Web/Views/Shared/_Layout.cshtml`

## Notes / Assumptions
- Branch scoping is applied using `Bookings.BranchID`.
- Revenue used in dashboard is based on **successful payments** inside the payment date range.
- Occupancy is calculated using sold room-nights vs (active rooms × total days).
- Excel export is generated as `.xls` HTML (same pattern as existing reports).
