# HotelApp Web (MVC)

Initial scaffold for Hotel Management System.

## Tech Stack
- .NET 9 MVC
- SQL Server (Dapper ORM)
- Cookie Authentication

## Connection String
Edit `appsettings.json` with your SQL Server credentials. Example:
```
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HotelApp;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
}
```

## Sample Users Table
```sql
CREATE TABLE Users (
  Id INT IDENTITY PRIMARY KEY,
  Username NVARCHAR(100) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(128) NOT NULL,
  DisplayName NVARCHAR(150) NULL
);
-- Insert a test user (password: admin123)
INSERT INTO Users (Username, PasswordHash, DisplayName)
VALUES (
  'admin',
  HASHBYTES('SHA2_256', 'admin123') -- store as hex string manually if needed
  , 'Administrator'
);
```
Note: In C# we use `Convert.ToHexString(SHA256(...))`. To generate a matching hash in SQL Server:
```sql
SELECT CONVERT(VARCHAR(64), HASHBYTES('SHA2_256','admin123'),2);
```
Use that returned hex string as `PasswordHash`.

## Run
```bash
dotnet run --project HotelApp.Web
```
Visit https://localhost:5001 (or shown URL).

## Next Steps
- Implement full BRD entities (Reservations, Rooms, Rates).
- Add role-based authorization.
- Add dashboard KPIs and charts.
- Centralize password hashing & migration to stronger algorithm (e.g., PBKDF2 / Argon2).
