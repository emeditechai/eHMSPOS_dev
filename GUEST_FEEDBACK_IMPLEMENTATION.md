# Guest Feedback (HotelApp)

## Goal
Add a professional, hotel-oriented Guest Feedback feature end-to-end (DB → repository → controller → UI) under the **Bookings** menu.

## Navigation
- Menu: **Bookings → Guest Feedback**
- Routes:
  - `GET /GuestFeedback/List` (list/filter)
  - `GET /GuestFeedback/Create?bookingNumber=BK-...` (new feedback; booking prefill optional)
  - `POST /GuestFeedback/Create` (save)
  - `GET /GuestFeedback/Details/{id}` (view)


### Public (Guest) Access

The feedback form supports anonymous access (no login) for sending the link via email after checkout.

- Public link format: `GET /GuestFeedback/Create?t={token}`
- The token is a signed, expiring value (ASP.NET Core DataProtection).
- Without a valid token, anonymous users will receive an error response.
- Staff users (logged-in) can still open the Create page normally.
## Database
Migration script:
- `Database/Scripts/79_CreateGuestFeedbackTables.sql`
Table: `dbo.GuestFeedback`
- Booking context (optional): `BookingId`, `BookingNumber`, `RoomNumber`, `VisitDate`
- Guest context (optional): `GuestName`, `Email`, `Phone`, `Birthday`, `Anniversary`, `IsFirstVisit`
- Ratings (1–5):
  - `OverallRating` (required)
  - `RoomCleanlinessRating`, `StaffBehaviorRating`, `ServiceRating`, `RoomComfortRating`, `AmenitiesRating`, `FoodRating`, `ValueForMoneyRating`, `CheckInExperienceRating`
- `QuickTags` (comma-separated)
- `Comments`
- Audit: `CreatedBy`, `CreatedDate`

## Backend
- Model: `HotelApp.Web/Models/GuestFeedback.cs`
- ViewModels: `HotelApp.Web/ViewModels/GuestFeedbackViewModels.cs`
- Repository:
  - Interface: `HotelApp.Web/Repositories/IGuestFeedbackRepository.cs`
  - Implementation: `HotelApp.Web/Repositories/GuestFeedbackRepository.cs`
- Controller:
  - `HotelApp.Web/Controllers/GuestFeedbackController.cs`

Hotel header info is loaded from `IHotelSettingsRepository` (per branch).

## UI
- Views:
  - `HotelApp.Web/Views/GuestFeedback/Create.cshtml`
  - `HotelApp.Web/Views/GuestFeedback/List.cshtml`
  - `HotelApp.Web/Views/GuestFeedback/Details.cshtml`
- Styles:
  - `HotelApp.Web/wwwroot/css/guest-feedback.css`

UI features:
- Star ratings (1–5)
- Quick tags selection (stored as comma-separated string)
- Optional guest details and dates

## Notes
- This is an internal (authenticated) page by default (`[Authorize]`).
- For a public guest-facing feedback link, we can add an anonymous endpoint + tokenized URL mapped to a booking.
