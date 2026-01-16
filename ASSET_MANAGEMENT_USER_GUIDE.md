# Standard Operating Procedure (SOP) — Asset Management

**Document ID:** SOP-AM-001  \
**Module:** Asset Management (Internal Inventory)  \
**Effective Date:** 16 Jan 2026  \
**Version:** 1.0  \
**Owner:** Hotel Operations / Store Keeper  \
**Applies To:** All branches using HotelApp

---

## 1. Purpose

To define a standard, step-by-step process to:

- Maintain item masters (Units, Departments, Items)
- Record stock IN/OUT using Stock Movement
- Monitor balances using Stock Report and Movement Audit
- Record and recover Damage/Loss cases

---

## 2. Scope

**In-scope**

- Unit Master, Department Master, Item Master
- Consumable Standards
- Stock Movement (IN/OUT)
- Movement Audit
- Stock Report
- Damage/Loss and Recovery

**Out-of-scope**

- Purchase orders / procurement
- Vendor management

---

## 3. Roles & Responsibilities

- **Admin**
  - Can enable **Allow Negative (Admin Override)** for consumables
  - Oversees configuration and access
- **Manager / Supervisor**
  - Reviews movements when required
  - Approves Damage/Loss records
- **Store Keeper / Staff**
  - Creates masters (if allowed)
  - Enters Stock Movements
  - Logs Damage/Loss records

---

## 4. Definitions

- **Branch:** Operational unit for which stock is maintained. Stock is always maintained per branch.
- **Asset:** Reusable/durable item (e.g., equipment, tools).
- **Consumable:** Item consumed during operations (e.g., soap, water bottle).
- **Movement:** A transaction that increases (IN) or decreases (OUT) stock.
- **Custodian:** Person responsible for an issued item.
- **Requires Custodian:** Item setting that forces capturing Custodian Name during movement.

---

## 5. Preconditions / Controls

Before starting daily operations:

1. Confirm you are working in the correct **Branch**.
2. Ensure Units, Departments, and Items are created.
3. Confirm item codes are standardized and **unique**.
4. Do not issue stock without recording a Stock Movement.

---

## 6. Procedure

### 6.1 Access the Module

1. Login to HotelApp.
2. From the top navbar, open **Asset Management**.
3. Available menus:
   - Item Master
   - Departments
   - Units
   - Consumable Standards
   - Stock Movement
   - Movement Audit
   - Stock Report
   - Damage/Loss

---

### 6.2 Master Setup (One-time per Branch)

#### 6.2.1 Create Units

1. Open **Asset Management → Units**.
2. Click **Create Unit**.
3. Enter unit name (examples: `Nos`, `Pcs`, `Kg`, `Ltr`).
4. Save.

#### 6.2.2 Create Departments

1. Open **Asset Management → Departments**.
2. Click **Create Department**.
3. Enter department name (examples: `Housekeeping`, `Maintenance`, `Front Office`).
4. Save.

#### 6.2.3 Create Items (Item Master)

1. Open **Asset Management → Item Master**.
2. Click **Create Item**.
3. Fill the form:
   - **Code**: must be **unique** (system blocks duplicates).
   - **Name**: item name.
   - **Unit**: select from Unit Master.
   - **Category**:
     - **Asset**: durable item
     - **Consumable**: consumable item
   - **Room Eligible**: set **Yes** if it can be allocated/used for rooms.
   - **Chargeable**: set **Yes** if recovery from guest/staff may happen.
   - **Requires Custodian**:
     - If **Yes**, **Stock Movement** requires **Custodian Name** whenever this item is used.
     - Use for items requiring accountability (costly tools/equipment).
   - **Threshold Qty**: optional, for low stock awareness.
   - **Status**: Active/Inactive.
   - **Eligible Departments**: select which departments can use this item.
4. Save.

---

### 6.3 Configure Consumable Standards (Recommended)

Use this when you want standardized/expected usage.

1. Open **Asset Management → Consumable Standards**.
2. Click **Add / Update Standard**.
3. Select a **Consumable** item.
4. Enter:
   - **Per Room/Day** quantity
   - **Per Stay** quantity
5. Set **Active** and Save.

---

### 6.4 Stock Movement (Daily Operations)

Use Stock Movement to add stock (IN) or issue/consume stock (OUT). This is the primary daily workflow.

#### 6.4.1 Create a Stock Movement

1. Open **Asset Management → Stock Movement**.
2. Select **Movement Type**.
3. Enter additional fields (shown/hidden based on movement type):
   - **Custodian Name** (mandatory if any selected item has **Requires Custodian = Yes**)
   - **To Department** (for Department Issue)
   - **Room** (for Room Allocation)
   - **Booking Number / Guest Name** (for Guest Issue)
   - **Notes** (recommended)
4. Add one or more line items:
   - Select **Item**
   - Enter **Qty** (must be > 0)
   - Optional: Serial / Note
5. Click **Save Movement**.

#### 6.4.2 Movement Types — When to Use

**IN movements**

- **Opening Stock (IN):** initial stock entry when starting the module
- **Return (IN):** stock returned back into store
- **Damage Recovery (IN):** recovered stock after repair/replacement

**OUT movements**

- **Department Issue (OUT):** issuing items to a department
- **Room Allocation (OUT):** allocating items to a room
- **Guest Issue (OUT):** issuing items directly to a guest/booking
- **Consumable Usage (OUT):** recording consumables used
- **Auto Checkout Consumables (OUT):** checkout-related consumable adjustments (if used)

#### 6.4.3 Stock Rules / Compliance

- Stock will **not** go below zero.
- Exception: **Consumables** can go negative only when **Admin** enables **Allow Negative (Admin Override)**.

---

### 6.5 Movement Audit (Verification)

1. Open **Asset Management → Movement Audit**.
2. Verify movements are captured correctly (date, type, items, qty, notes).
3. Use this for investigations and accountability.

---

### 6.6 Stock Report (Current Balance)

1. Open **Asset Management → Stock Report**.
2. Review current balance per item.
3. If stock looks incorrect, cross-check **Movement Audit**.

---

### 6.7 Damage / Loss Workflow

Use Damage/Loss to record broken/missing items and record recoveries.

#### 6.7.1 Create a Damage/Loss Record

1. Open **Asset Management → Damage/Loss**.
2. Click **Create Damage/Loss**.
3. Enter item, qty, notes (and related details).
4. Save.

#### 6.7.2 Approve Damage/Loss

1. Open the record.
2. Click **Approve** (usually Manager/Admin).

#### 6.7.3 Add Recovery

1. Open the approved Damage/Loss record.
2. Click **Add Recovery**.
3. Select recovery type (Cash / Replacement / Staff Deduction / Bill Posting).
4. Save.

> Note: If you select **Bill Posting**, posting the recovery into guest billing/receipt may require additional integration depending on your setup.

---

## 7. Records to Maintain

- Stock Movements (via **Movement Audit**)
- Current stock balances (via **Stock Report**)
- Damage/Loss records and Recovery entries

---

## 8. Troubleshooting

**Problem:** “Custodian Name is required” error

- Check the selected items in the movement lines.
- If any item has **Requires Custodian = Yes**, you must enter **Custodian Name**.

**Problem:** “Code already exists” when creating/editing an item

- Item **Code** must be unique.
- Choose a standardized format (example: `HK-SOAP-100`, `RM-TOWEL-BATH`).

**Problem:** “Insufficient stock / cannot go below zero”

- Verify balance in **Stock Report**.
- Confirm you selected the correct movement type.
- For consumables only, Admin may enable **Allow Negative (Admin Override)** if your process allows it.

---

## 9. Change History

- v1.0 (16 Jan 2026): Initial SOP created.
