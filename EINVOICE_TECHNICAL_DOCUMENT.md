# GST B2B E-Invoice — Technical Documentation

**Product:** eHMS POS (Hotel Management System)  
**Module:** B2B E-Invoice Generation & IRP Integration  
**Version:** 2.1.5  
**Date:** May 2026  
**Applicable Migrations:** 141 → 145  

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Operating Modes](#3-operating-modes)
4. [Database Schema](#4-database-schema)
5. [Configuration — Hotel Settings](#5-configuration--hotel-settings)
6. [API Flow: AUTO Mode (IRP Integration)](#6-api-flow-auto-mode-irp-integration)
7. [MANUAL Mode (Option B — File Export)](#7-manual-mode-option-b--file-export)
8. [Invoice JSON Structure](#8-invoice-json-structure)
9. [Code Architecture](#9-code-architecture)
10. [E-Invoice Dashboard](#10-e-invoice-dashboard)
11. [Retry / Manual Push](#11-retry--manual-push)
12. [Security](#12-security)
13. [Error Handling & Audit Trail](#13-error-handling--audit-trail)
14. [Database Migrations Reference](#14-database-migrations-reference)
15. [Switching Environments (Sandbox → Production)](#15-switching-environments-sandbox--production)
16. [Troubleshooting](#16-troubleshooting)

---

## 1. Overview

The E-Invoice module generates GST-compliant e-invoice JSON (IRN payload) automatically for every **B2B guest checkout**. It supports two operating modes:

| Mode | Behaviour |
|------|-----------|
| **AUTO** | JSON is generated at checkout → submitted directly to the NIC Invoice Registration Portal (IRP) via REST API → IRN + QR code received and stored |
| **MANUAL** | JSON is generated at checkout → saved to the database → optionally exported as a JSON file to a configured folder |

Both modes use the **same** `B2BEInvoiceJsonLogs` table and **same** JSON generation logic. The mode is configured per-branch in `HotelSettings`.

---

## 2. Architecture

```
B2B Guest Checkout
        │
        ▼
 RoomsController
 (CheckOut / ForceCheckOut)
        │
        ▼
 EInvoiceJsonService.GenerateAndSaveAsync()
        │
        ├── [MANUAL mode] ──────────────────────────────────┐
        │   • Build JSON payload                             │
        │   • Save to B2BEInvoiceJsonLogs                   │
        │   • Export .json file to EInvoiceJsonStoragePath  │
        │                                                    ▼
        │                                          [Done — status: null]
        │
        └── [AUTO mode] ──────────────────────────────────────┐
            • Build JSON payload                               │
            • Save to B2BEInvoiceJsonLogs (PushStatus=PENDING) │
            • IrpApiService.GetValidTokenAsync()               │
            │   ├─ Cache hit → return stored token             │
            │   └─ Cache miss → POST /auth → store token       │
            • IrpApiService.GenerateIrnAsync()                 │
            │   └─ POST /Invoice with Bearer token             │
            • Update log: PUSHED (Irn, AckNo, QR) or FAILED   │
            ▼                                                  │
      [Done]◄─────────────────────────────────────────────────┘
```

---

## 3. Operating Modes

### 3.1 AUTO Mode

- Activated when `HotelSettings.EInvoiceMode = "AUTO"`
- On every B2B checkout: JSON is built → IRP API called → IRN stored
- If IRP call fails: log is saved with `PushStatus = FAILED`; staff can **Retry** from the dashboard
- Token is cached in `EInvoiceIrpTokens` (default TTL: 55 minutes); re-authentication happens automatically on expiry

### 3.2 MANUAL Mode

- Activated when `HotelSettings.EInvoiceMode = "MANUAL"`
- On every B2B checkout: JSON is built and saved to DB
- If `EInvoiceJsonStoragePath` is configured: JSON file is also written to disk as  
  `EInvoice_{BookingNo}_{InvoiceNo}.json`
- No IRP API call is made; `PushStatus` remains `NULL`
- Staff can view/copy the JSON from the **E-Invoice Dashboard**

---

## 4. Database Schema

### 4.1 `dbo.B2BEInvoiceJsonLogs`

Primary log table. One row per e-invoice event (per booking per checkout).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | INT IDENTITY | NO | Primary key |
| `BookingId` | INT | NO | FK → Bookings.Id |
| `BookingNo` | NVARCHAR(50) | NO | Human-readable booking number |
| `InvoiceNumber` | NVARCHAR(50) | NO | Invoice number (e.g. INV/2026-27/00019) |
| `Version` | NVARCHAR(20) | NO | Unique version ("1.1", "1.2", …) from sequence |
| `GenerationType` | NVARCHAR(10) | NO | `MANUAL` or `AUTO` (default `MANUAL`) |
| `JsonPayload` | NVARCHAR(MAX) | NO | Full e-invoice JSON sent/generated |
| `BranchID` | INT | NO | Branch that generated the invoice |
| `CreatedDate` | DATETIME2 | NO | UTC timestamp of generation |
| `CreatedBy` | INT | YES | UserId of staff who triggered checkout |
| `PushStatus` | NVARCHAR(20) | YES | `NULL` / `PENDING` / `PUSHED` / `FAILED` |
| `PushedAt` | DATETIME2 | YES | UTC timestamp of successful IRP push |
| `PushResponse` | NVARCHAR(MAX) | YES | Raw IRP response or error message |
| `Irn` | NVARCHAR(100) | YES | Invoice Reference Number from IRP |
| `AckNo` | NVARCHAR(50) | YES | Acknowledgement number from IRP |
| `AckDt` | NVARCHAR(50) | YES | Acknowledgement date string from IRP |
| `SignedQRCode` | NVARCHAR(MAX) | YES | Base64 encoded signed QR code from IRP |
| `IrnRequestJson` | NVARCHAR(MAX) | YES | Exact JSON sent to IRP (audit) |
| `IrnResponseJson` | NVARCHAR(MAX) | YES | Exact JSON received from IRP (audit) |

### 4.2 `dbo.EInvoiceIrpTokens`

Caches IRP authentication tokens to avoid re-authenticating on every invoice.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | INT IDENTITY | NO | Primary key |
| `BranchID` | INT | NO | Branch the token belongs to |
| `SessionUserId` | INT | YES | User who triggered authentication |
| `AccessToken` | NVARCHAR(MAX) | NO | Bearer token from IRP |
| `ExpiresAt` | DATETIME2 | NO | Token expiry (UTC). Query filters `ExpiresAt > SYSUTCDATETIME()` |
| `CreatedAt` | DATETIME2 | NO | Default: `SYSUTCDATETIME()` |
| `CreatedBy` | INT | YES | UserId |

**Token cache logic:** `GetValidTokenAsync` queries for the most recent non-expired token for `BranchID`. If found, uses it. If not, calls `/auth` and inserts new row. A 60-second safety margin is applied to `ExpiresAt` to prevent using a token that expires mid-request.

### 4.3 `dbo.EInvoiceVersionSequence`

Monotonically incrementing counter to generate unique version numbers.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | INT | Always 1 row |
| `LastSequence` | INT (default 0) | Incremented atomically with `HOLDLOCK` |

Version format: `"1.{LastSequence}"` (e.g. `1.1`, `1.42`, `1.100`)

### 4.4 `dbo.HotelSettings` — E-Invoice Columns

| Column | Type | Description |
|--------|------|-------------|
| `EInvoiceMode` | NVARCHAR(10) | `MANUAL` or `AUTO` |
| `EInvoiceApiBaseUrl` | NVARCHAR(500) | Base URL (informational) |
| `EInvoiceAuthUrl` | NVARCHAR(500) | Full URL for POST /auth |
| `EInvoiceIrnEndpoint` | NVARCHAR(500) | Full URL for POST /Invoice |
| `EInvoiceClientId` | NVARCHAR(200) | IRP client_id header value |
| `EInvoiceClientSecret` | NVARCHAR(1000) | **Encrypted** using ASP.NET Data Protection |
| `EInvoiceUsername` | NVARCHAR(200) | IRP portal username |
| `EInvoicePassword` | NVARCHAR(1000) | **Encrypted** using ASP.NET Data Protection |
| `EInvoiceJsonStoragePath` | NVARCHAR(500) | Filesystem path for MANUAL JSON export |

---

## 5. Configuration — Hotel Settings

Navigate to: **Settings → Hotel Settings → Edit**

### AUTO Mode Setup

| Field | Sandbox Value | Production Value |
|-------|--------------|-----------------|
| E-Invoice Mode | `AUTO` | `AUTO` |
| API Base URL | `https://sandbox.einvoiceapi.nic.in` | `https://einvoice1.gst.gov.in` |
| Authorization URL | `https://sandbox.einvoiceapi.nic.in/eivital/v1.04/auth` | `https://einvoice1.gst.gov.in/eivital/v1.04/auth` |
| Generate IRN Endpoint | `https://sandbox.einvoiceapi.nic.in/eicore/v1.03/Invoice` | `https://einvoice1.gst.gov.in/eicore/v1.03/Invoice` |
| Client ID | *(from NIC sandbox registration)* | *(from NIC production)* |
| Client Secret | *(from NIC)* | *(from NIC)* |
| Username | *(from NIC)* | *(from NIC)* |
| Password | *(from NIC)* | *(from NIC)* |

> **Important:** Client Secret and Password are encrypted before storage using ASP.NET Data Protection (`"HotelApp.Web.EInvoice.Secrets.v1"` purpose key). They are never stored in plain text.

> **Partial update:** Leaving the Secret/Password field blank during an edit will **keep the existing encrypted value** (COALESCE in SP). The field must only be filled when you want to change the value.

### MANUAL Mode Setup

| Field | Value |
|-------|-------|
| E-Invoice Mode | `MANUAL` |
| JSON Export Path | Optional. e.g. `C:\EInvoiceExports\` or `/var/einvoice/` |

When Mode is saved as MANUAL, all API credential fields are automatically cleared in the database.

---

## 6. API Flow: AUTO Mode (IRP Integration)

### Step 1 — Authentication

**Endpoint:** `POST {EInvoiceAuthUrl}`

**Request Headers:**
```
Content-Type:  application/json
client_id:     {EInvoiceClientId}
client_secret: {decrypted EInvoiceClientSecret}
gstin:         {HotelSettings.GSTCode}
```

**Request Body:**
```json
{
  "username": "{EInvoiceUsername}",
  "password": "{decrypted EInvoicePassword}"
}
```

**Expected Response:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "expires_in": 3600
}
```

**Token Storage:**  
Token is inserted into `EInvoiceIrpTokens` with `ExpiresAt = NOW + expires_in - 60s`. On subsequent requests, the cached token is used until it expires.

---

### Step 2 — Invoice JSON Preparation

The JSON is built by `EInvoiceJsonService.BuildItemListAsync()`:

- For **multi-room-type** B2B bookings: one `ItemList` entry per `B2BBookingRoomLine` (active lines only)
- For **single room-type** bookings: one entry using booking header totals
- GST rate is resolved from `GstSlabRepository.ResolveBandAsync()` by matching `RatePerNight` → GST band → `GstPercent`
- `TotAmt = BaseAmount + round(BaseAmount × GstRate / 100, 2)` (post-tax total)

---

### Step 3 — IRN Generation

**Endpoint:** `POST {EInvoiceIrnEndpoint}`

**Request Headers:**
```
Content-Type:   application/json
Authorization:  Bearer {access_token}
gstin:          {HotelSettings.GSTCode}
```

**Request Body:** Full e-invoice JSON (see Section 8)

**Expected Response (Success):**
```json
{
  "Status": "Success",
  "Irn":          "8f5c8a7d9e3b2c1a...",
  "AckNo":        "123456789012345",
  "AckDt":        "2026-05-04T12:30:00",
  "SignedQRCode": "base64encodedstring..."
}
```

> The service handles both flat response and `result`-wrapped response (`{ "result": { "Irn": ... } }`) as used by some NIC sandbox endpoints.

**On Success:** `B2BEInvoiceJsonLogs` updated:
- `PushStatus = 'PUSHED'`
- `PushedAt = SYSUTCDATETIME()`
- `Irn`, `AckNo`, `AckDt`, `SignedQRCode` populated
- `IrnRequestJson` = request body sent
- `IrnResponseJson` = raw response received

**On Failure:** `B2BEInvoiceJsonLogs` updated:
- `PushStatus = 'FAILED'`
- `PushResponse` = HTTP status + response body or exception message
- `IrnResponseJson` = raw error response

---

## 7. MANUAL Mode (Option B — File Export)

At checkout, the system:

1. Builds the same invoice JSON as AUTO mode
2. Saves to `B2BEInvoiceJsonLogs` with `GenerationType = 'MANUAL'`, `PushStatus = NULL`
3. If `EInvoiceJsonStoragePath` is configured in HotelSettings:
   - Creates directory if it doesn't exist
   - Writes file: `{path}/EInvoice_{BookingNo}_{InvoiceNo}.json`
   - File write failure is non-fatal (logged as warning, checkout proceeds)

Staff can also view and copy the JSON from the **E-Invoice Dashboard** at any time.

---

## 8. Invoice JSON Structure

```json
{
  "Version": "1.42",
  "TranDtls": {
    "TaxSch": "GST",
    "SupTyp": "B2B"
  },
  "DocDtls": {
    "Typ": "INV",
    "No":  "INV/2026-27/00019",
    "Dt":  "04/05/2026"
  },
  "SellerDtls": {
    "Gstin": "19AAPFU0939F1ZV",
    "LglNm": "Hotel AA Residency"
  },
  "BuyerDtls": {
    "Gstin": "19GT654322",
    "LglNm": "Emeditech Development"
  },
  "ItemList": [
    {
      "SlNo":      "1",
      "PrdDesc":   "Room Charges - Deluxe",
      "Qty":       1,
      "UnitPrice": 1200.00,
      "GstRt":     10.00,
      "TotAmt":    1320.00
    }
  ]
}
```

### Field Mapping

| JSON Field | Source |
|-----------|--------|
| `Version` | Auto-incremented from `EInvoiceVersionSequence` → format `"1.{n}"` |
| `DocDtls.No` | `Booking.InvoiceNumber` ?? `Booking.BookingNumber` |
| `DocDtls.Dt` | `ActualCheckOutDate` ?? `CheckOutDate` formatted as `dd/MM/yyyy` |
| `SellerDtls.Gstin` | `HotelSettings.GSTCode` |
| `SellerDtls.LglNm` | `HotelSettings.LglNm` ?? `HotelSettings.HotelName` |
| `BuyerDtls.Gstin` | `Booking.CompanyGstNo` |
| `BuyerDtls.LglNm` | `Booking.B2BClientName` |
| `ItemList[i].PrdDesc` | `"Room Charges - {RoomTypeName}"` |
| `ItemList[i].Qty` | `B2BBookingRoomLine.RequiredRooms` |
| `ItemList[i].UnitPrice` | `LineBaseAmount / Qty` |
| `ItemList[i].GstRt` | Resolved from `GstSlabs` master via `ResolveBandAsync()` |
| `ItemList[i].TotAmt` | `BaseAmount + round(BaseAmount × GstRt / 100, 2)` |

---

## 9. Code Architecture

### Services

| Class | Interface | Responsibility |
|-------|-----------|---------------|
| `EInvoiceJsonService` | `IEInvoiceJsonService` | Orchestrates JSON build + save + IRP call (for AUTO) or file export (for MANUAL) |
| `IrpApiService` | `IIrpApiService` | Handles IRP HTTP calls: auth token management + IRN generation |
| `EInvoiceProtector` | `IEInvoiceProtector` | ASP.NET Data Protection wrapper for encrypting/decrypting IRP secrets |

### Repositories

| Class | Interface | Responsibility |
|-------|-----------|---------------|
| `B2BEInvoiceLogRepository` | `IB2BEInvoiceLogRepository` | CRUD for `B2BEInvoiceJsonLogs`; dashboard SP call |
| `HotelSettingsRepository` | `IHotelSettingsRepository` | Reads/writes HotelSettings including all EInvoice columns |

### Key Methods

**`EInvoiceJsonService.GenerateAndSaveAsync(booking, hotelSettings, userId)`**
- Entry point called on every B2B checkout
- Guards: only B2B bookings, only MANUAL/AUTO modes, idempotent (skips if log exists for booking)
- Calls `IrpApiService` for AUTO, `TryExportToFileAsync` for MANUAL

**`IrpApiService.GetValidTokenAsync(settings, branchId, userId)`**
- Opens its own `SqlConnection` (independent of scoped `IDbConnection`)
- Queries `EInvoiceIrpTokens` for non-expired token for `branchId`
- Falls back to `AuthenticateAndStoreAsync` on cache miss

**`IrpApiService.GenerateIrnAsync(settings, accessToken, invoiceJson)`**
- Creates `HttpRequestMessage` with per-request headers (safe for pooled `HttpClient`)
- Handles both flat and `result`-wrapped IRP response envelope

**`IB2BEInvoiceLogRepository.UpdateIrnResponseAsync(logId, irn, ackNo, ackDt, qr, pushStatus, reqJson, respJson)`**
- Called after every IRP attempt (success or failure) to update the log row

### DI Registration (`Program.cs`)

```csharp
builder.Services.AddHttpClient();             // IHttpClientFactory
builder.Services.AddScoped<IEInvoiceProtector, EInvoiceProtector>();
builder.Services.AddScoped<IIrpApiService, IrpApiService>();
builder.Services.AddScoped<IEInvoiceJsonService, EInvoiceJsonService>();
builder.Services.AddScoped<IB2BEInvoiceLogRepository, B2BEInvoiceLogRepository>();
```

### Controller Integration

**`RoomsController`** — checkout triggers:
```csharp
var hotelSettings = await _hotelSettingsRepository.GetByBranchAsync(booking.BranchID);
if (hotelSettings != null)
    await _eInvoiceJsonService.GenerateAndSaveAsync(booking, hotelSettings, currentUserId);
```
Called in both `CheckOut` and `ForceCheckOut` actions. Wrapped in `try/catch` — failure is non-fatal; checkout completes regardless.

**`BookingController.RetryIrn(int id)`** — manual retry from dashboard:
- Validates log is AUTO mode and settings have credentials
- Calls `GetValidTokenAsync` → `GenerateIrnAsync`
- Returns JSON `{ success, irn, ackNo, ackDt }` for AJAX response

---

## 10. E-Invoice Dashboard

**URL:** `/Booking/EInvoiceDashboard`  
**Nav:** Bookings → E-Invoice Logs  
**Access:** Authenticated users with Bookings menu access

### Filters

| Filter | Description |
|--------|-------------|
| Check-Out From / To | Filters on `Bookings.ActualCheckOutDate` |
| Generation Type | `MANUAL` / `AUTO` / All |
| Portal Status | `Not Pushed` / `Pushed` / `Failed` / All |
| Booking No | Partial match search |

### Summary Cards (8)

| Card | Metric |
|------|--------|
| Total E-Invoices | All rows in result |
| Not Pushed | `PushStatus IS NULL` |
| Pushed | `PushStatus = 'PUSHED'` |
| Failed | `PushStatus = 'FAILED'` |
| Manual | `GenerationType = 'MANUAL'` |
| Auto | `GenerationType = 'AUTO'` |
| Total Revenue | Sum of `GrandTotal` |
| Total GST | Sum of `TaxAmount` |

Not Pushed / Pushed / Failed cards are **clickable** — clicking filters the table by that portal status.

### Table Columns

Booking No, Invoice No, Version, Gen. Type (badge), Guest / Company, Company GST, Check-Out, Base Amt, GST, Grand Total, Portal Status (colour badge), Generated On, Actions

### Actions per Row

| Button | Condition | Behaviour |
|--------|-----------|-----------|
| **View** | Always | Opens modal with pretty-printed JSON + Copy button |
| **Push** | AUTO, not yet pushed | AJAX call to `POST /Booking/RetryIrn` |
| **Retry** | AUTO, failed | AJAX call to `POST /Booking/RetryIrn` |
| **Pushed** (disabled) | Already pushed | No action |
| **Push** (coming soon) | MANUAL, not pushed | Opens placeholder modal |

### Stored Procedure

`dbo.usp_GetB2BEInvoiceDashboard`

```sql
EXEC usp_GetB2BEInvoiceDashboard
    @BranchID       = 1,
    @FromDate       = '2026-04-01',
    @ToDate         = '2026-05-04',
    @GenerationType = NULL,    -- NULL = all
    @BookingNoSearch = NULL,   -- NULL = all
    @PushStatus     = NULL     -- NULL = all
```

---

## 11. Retry / Manual Push

When AUTO mode records fail (e.g. credentials not configured at checkout time, network issue):

1. Go to `Bookings → E-Invoice Logs`
2. Locate the `Failed` row
3. Click **Retry**

The system will:
1. Check `EInvoiceIrpTokens` for a cached valid token
2. If expired/missing, call `/auth` and cache a new token
3. Submit the stored `JsonPayload` to the IRN endpoint
4. Update the row live (no page reload):
   - Success → badge changes to **Pushed**, button disabled
   - Failure → error alert shown, row remains **Failed**

**`POST /Booking/RetryIrn`** endpoint:
- CSRF protected (AntiForgery token required)
- Returns `{ success: bool, message: string, irn?: string, ackNo?: string, ackDt?: string }`

---

## 12. Security

| Concern | Implementation |
|---------|---------------|
| **Secret storage** | `EInvoiceClientSecret` and `EInvoicePassword` encrypted using `IDataProtector` with purpose `"HotelApp.Web.EInvoice.Secrets.v1"` before storing to DB |
| **Partial update** | Blank secret/password on save → COALESCE in SP keeps existing encrypted value |
| **CSRF** | `RetryIrn` is `[HttpPost]` with AntiForgery token validated; token injected via `@Html.AntiForgeryToken()` in dashboard view |
| **Per-request headers** | `HttpRequestMessage` used (not `DefaultRequestHeaders`) — prevents header leakage between concurrent requests on pooled `HttpClient` |
| **Token expiry margin** | `ExpiresAt = issued_at + expires_in - 60s` to avoid using a token that expires mid-flight |
| **Non-fatal checkout** | IRP failures are caught and logged — checkout is never blocked by e-invoice failure |
| **Input scoping** | All SP queries filter by `BranchID` — no cross-branch data leakage |

---

## 13. Error Handling & Audit Trail

### Error States

| Scenario | `PushStatus` | `PushResponse` / `IrnResponseJson` |
|----------|-------------|----------------------------------|
| Settings not configured | `FAILED` | `"Authentication failed: could not obtain token."` |
| HTTP 401 from IRP | `FAILED` | `"HTTP 401: {response body}"` |
| Network timeout | `FAILED` | Exception message |
| `access_token` missing in auth response | `FAILED` | `"Authentication failed: ..."` |
| `EInvoiceIrnEndpoint` not set | `FAILED` | `"EInvoiceIrnEndpoint is not configured."` |

### Logging

All IRP interactions are logged via `ILogger<IrpApiService>`:

```
IRP: Authenticating at https://sandbox.einvoiceapi.nic.in/eivital/v1.04/auth
IRP Auth response (200): {"access_token":"eyJ...","expires_in":3600}
IRP: Token obtained and cached for branch 1, expires 2026-05-04T14:29:00Z
IRP: Using cached token for branch 1
IRP: Calling IRN endpoint https://sandbox.einvoiceapi.nic.in/eicore/v1.03/Invoice
IRP IRN response (200): {"Status":"Success","Irn":"...","AckNo":"..."}
IRP: IRN generated for booking B2B-20260504075259-338: 8f5c8a7d...
```

### Full Audit Trail

Every IRP submission stores:
- `IrnRequestJson` — exact JSON submitted to IRP
- `IrnResponseJson` — exact response received from IRP
- `PushedAt` — UTC timestamp
- `SessionUserId` in `EInvoiceIrpTokens` — who triggered the auth

---

## 14. Database Migrations Reference

| Migration | File | What It Does |
|-----------|------|-------------|
| 141 | `141_AddEInvoiceConfigToHotelSettings.sql` | Adds EInvoice columns to HotelSettings; recreates `sp_UpsertHotelSettings` |
| 141b | `141b_RecreateUpsertSP_EInvoiceJsonPath.sql` | Adds `EInvoiceJsonStoragePath` column and SP param |
| 142 | `142_CreateB2BEInvoiceJsonLogs.sql` | Creates `B2BEInvoiceJsonLogs` and `EInvoiceVersionSequence` tables |
| 143 | `143_CreateB2BEInvoiceDashboardSP.sql` | Creates `usp_GetB2BEInvoiceDashboard`; inserts nav menu item |
| 144 | `144_AddPushStatusToB2BEInvoiceLogs.sql` | Adds `PushStatus`, `PushedAt`, `PushResponse` columns; updates SP |
| 145 | `145_AddIrnFieldsAndTokenTable.sql` | Creates `EInvoiceIrpTokens`; adds `Irn`, `AckNo`, `AckDt`, `SignedQRCode`, `IrnRequestJson`, `IrnResponseJson` to logs; updates SP |

**Apply all pending migrations:**
```bash
for f in 141 141b 142 143 144 145; do
  sqlcmd -S {SERVER} -U {USER} -P {PASS} -d HMS_Dev -C \
    -i Database/Scripts/${f}_*.sql
done
```

---

## 15. Switching Environments (Sandbox → Production)

No code changes required. Update only `HotelSettings` in the admin panel:

| Setting | Sandbox | Production |
|---------|---------|------------|
| Auth URL | `https://sandbox.einvoiceapi.nic.in/eivital/v1.04/auth` | `https://einvoice1.gst.gov.in/eivital/v1.04/auth` |
| IRN Endpoint | `https://sandbox.einvoiceapi.nic.in/eicore/v1.03/Invoice` | `https://einvoice1.gst.gov.in/eicore/v1.03/Invoice` |
| Client ID | NIC sandbox client_id | NIC production client_id |
| Client Secret | NIC sandbox secret | NIC production secret |
| Username | NIC sandbox username | NIC portal username |
| Password | NIC sandbox password | NIC portal password |

After updating, all cached tokens in `EInvoiceIrpTokens` for that branch will expire naturally. New invoices will authenticate against the production portal.

---

## 16. Troubleshooting

### "Authentication failed: could not obtain token"

**Cause 1:** API credentials not configured in HotelSettings  
**Fix:** Settings → Hotel Settings → Edit → fill all AUTO mode fields → Save

**Cause 2:** Mode was previously MANUAL (which clears all API fields)  
**Fix:** Same as above — re-enter all credentials after switching to AUTO

**Cause 3:** NIC portal returned 401/403 (wrong credentials)  
**Fix:** Verify `client_id`, `client_secret`, `username`, `password` with NIC. Check logs for `IRP Auth response (401)` to confirm.

**Cause 4:** Network cannot reach sandbox/production IRP URL  
**Fix:** Ensure the server has outbound HTTPS access to `sandbox.einvoiceapi.nic.in` or `einvoice1.gst.gov.in`

---

### "EInvoiceIrnEndpoint is not configured"

**Fix:** Set the Generate IRN Endpoint field in Hotel Settings

---

### Invoice generated with wrong GST rate

The GST rate is resolved from the `GstSlabs` master table using `ResolveBandAsync(ratePerNight, stayDate, gstSlabId)`. Check:
1. GST Slab master has the correct band for the room rate
2. The booking's `GstSlabId` is set correctly
3. If no band is found, fallback uses `TaxAmount / BaseAmount × 100` from booking header

---

### JSON shows `&quot;` in dashboard viewer

This was a double-encoding bug (fixed in commit `f32fd96`). If seen on older deployments: ensure `@System.Web.HttpUtility.HtmlAttributeEncode()` is NOT wrapping `row.JsonPayload` in the view — use plain `@(row.JsonPayload)`.

---

### Duplicate invoice for same booking

`ExistsForBookingAsync` is checked before generating. If a log exists for `BookingId`, generation is skipped. If duplicate logs appear, check for concurrent checkout requests (race condition). The `GetNextVersionAsync` uses `WITH (HOLDLOCK)` to ensure sequence safety.

---

*Document generated: May 2026 | eHMS POS v2.1.5 | Module: B2B E-Invoice*
