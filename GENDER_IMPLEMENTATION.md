# Gender Field Implementation

## Overview
Added Gender field to capture guest information across all booking-related forms and database tables.

## Database Changes
- **Migration Script**: `Database/Scripts/26_AddGenderColumn.sql`
- **Tables Updated**:
  - `Guests` table: Added `Gender NVARCHAR(20) NULL`
  - `BookingGuests` table: Added `Gender NVARCHAR(20) NULL`

## Model Changes
- **Guest.cs**: Added `public string? Gender { get; set; }`
- **BookingGuest** (in Booking.cs): Added `public string? Gender { get; set; }`

## Controller Changes
- **BookingController.cs**:
  - `AddGuestRequest`: Added `Gender` property
  - `UpdateGuestRequest`: Added `Gender` property
  - `AddGuest` action: Handles Gender field
  - `UpdateGuest` action: Handles Gender field

## Repository Changes
- **BookingRepository.cs**:
  - `AddGuestToBookingAsync`: INSERT/UPDATE queries include Gender
  - `UpdateGuestAsync`: INSERT/UPDATE queries include Gender
  - All SQL queries updated to handle Gender field

## View Changes
### Create Booking (Views/Booking/Create.cshtml)
- Added Gender dropdown after Last Name field
- Options: Male, Female, Other

### Booking Details (Views/Booking/Details.cshtml)
- **Add Guest Modal**: Added Gender dropdown
- **Edit Guest Modal**: Added Gender dropdown
- **View Guest Modal**: Added Gender display in Personal Information section
- **JavaScript Functions**:
  - `showViewGuestModal`: Updated to accept and display gender parameter
  - `showEditGuestModal`: Updated to accept and populate gender field

## Gender Options
- Male
- Female
- Other

## Implementation Notes
- Gender field is optional across all forms
- Stored in database as NVARCHAR(20)
- Dropdown select in UI for consistency
- Syncs to both BookingGuests and Guests tables
- Displayed in View Guest modal

## Testing Checklist
- [ ] Run migration script: `26_AddGenderColumn.sql`
- [ ] Create new booking with gender
- [ ] Add additional guest with gender in booking details
- [ ] Edit guest and update gender
- [ ] View guest details and verify gender displays correctly
- [ ] Verify gender saves to both BookingGuests and Guests tables
