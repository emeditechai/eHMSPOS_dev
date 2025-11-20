# Hotel Management System - LuxStay

A modern hotel management web application built with ASP.NET Core MVC, SQL Server, and Dapper ORM.

## 🏨 Features

### Authentication & Authorization
- ✅ BCrypt password hashing for secure authentication
- ✅ Role-based access control (Administrator, Manager, Staff)
- ✅ Account lockout after failed login attempts
- ✅ Session management with cookie authentication
- ✅ Support for Multi-Factor Authentication (MFA) ready

### Dashboard
- ✅ Real-time KPI cards (Guests, Occupancy, Revenue, Check-ins)
- ✅ Interactive charts (Revenue overview, Room types distribution)
- ✅ Recent bookings table with status tracking
- ✅ Responsive sidebar navigation
- ✅ User profile management

### UI/UX
- ✅ Modern LuxStay branded design
- ✅ Fully responsive for mobile, tablet, and desktop
- ✅ Chart.js integration for data visualization
- ✅ Font Awesome icons

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server 2016 or later
- Azure Data Studio or SQL Server Management Studio (SSMS)

### Installation

1. **Clone the repository**
   ```bash
   cd /Users/abhikporel/dev/Hotelapp
   ```

2. **Restore NuGet packages**
   ```bash
   cd HotelApp.Web
   dotnet restore
   ```

3. **Setup Database**
   
   First, create the database:
   ```sql
   CREATE DATABASE HotelApp;
   GO
   ```
   
   Then execute the SQL scripts in order:
   - `Database/Scripts/01_CreateTables.sql` - Creates tables
   - `Database/Scripts/02_SeedData.sql` - Seeds initial data

   See `Database/README.md` for detailed instructions.

4. **Update Connection String**
   
   Edit `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=HotelApp;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
   }
   ```

5. **Run the Application**
   ```bash
   dotnet run --urls "https://localhost:7200;http://localhost:5200"
   ```

6. **Access the Application**
   - HTTPS: https://localhost:7200
   - HTTP: http://localhost:5200

## 🔐 Default Credentials

**Username:** `admin`  
**Password:** `admin@123`  
**Email:** admin@restaurant.com

> ⚠️ **Important:** Change the default password immediately in production!

## 📁 Project Structure

```
HotelApp/
├── Database/
│   ├── Scripts/
│   │   ├── 01_CreateTables.sql
│   │   └── 02_SeedData.sql
│   └── README.md
├── HotelApp.Web/
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   └── DashboardController.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── UserRole.cs
│   │   └── LoginViewModel.cs
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   └── UserRepository.cs
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   └── AuthService.cs
│   ├── Views/
│   │   ├── Account/
│   │   │   └── Login.cshtml
│   │   ├── Dashboard/
│   │   │   └── Index.cshtml
│   │   └── Shared/
│   │       └── _Layout.cshtml
│   └── wwwroot/
│       └── css/
│           ├── login.css
│           └── dashboard.css
└── README.md
```

## 🛠️ Technology Stack

- **Framework:** ASP.NET Core 9.0 MVC
- **ORM:** Dapper (micro-ORM)
- **Database:** Microsoft SQL Server
- **Authentication:** Cookie-based with BCrypt password hashing
- **Frontend:** HTML5, CSS3, JavaScript
- **Charts:** Chart.js
- **Icons:** Font Awesome 6.4.0

## 📊 Database Schema

### Users Table
Stores user accounts with secure BCrypt password hashing.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Username | NVARCHAR(100) | Unique username |
| Email | NVARCHAR(255) | User email |
| PasswordHash | NVARCHAR(255) | BCrypt hashed password |
| Salt | NVARCHAR(255) | Password salt |
| FullName | NVARCHAR(200) | Display name |
| IsActive | BIT | Account status |
| IsLockedOut | BIT | Lockout flag |
| FailedLoginAttempts | INT | Failed login counter |

### Roles Table
Defines system and custom roles.

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR(100) | Role name |
| Description | NVARCHAR(500) | Role description |
| IsSystemRole | BIT | System role flag |

### UserRoles Table
Many-to-many relationship between users and roles.

## 🔒 Security Features

1. **BCrypt Password Hashing** - Industry-standard password security
2. **Account Lockout** - Automatic lockout after 5 failed attempts
3. **Secure Cookies** - HttpOnly and Secure flags enabled
4. **SQL Injection Protection** - Parameterized queries via Dapper
5. **CSRF Protection** - Anti-forgery tokens on forms
6. **Session Management** - Secure cookie-based sessions

## 📝 Development Roadmap

### Phase 1 - Authentication ✅ (Completed)
- [x] Login/Logout functionality
- [x] BCrypt password hashing
- [x] Role-based authorization
- [x] Account lockout mechanism
- [x] Dashboard with KPIs

### Phase 2 - Core Modules (Planned)
- [ ] Room Management (CRUD)
- [ ] Guest Management
- [ ] Booking/Reservation System
- [ ] Check-in/Check-out workflows
- [ ] Payment processing

### Phase 3 - Advanced Features (Planned)
- [ ] Reporting & Analytics
- [ ] Email notifications
- [ ] SMS integration
- [ ] Multi-property support
- [ ] Mobile app API

## 🧪 Testing

Run tests:
```bash
dotnet test
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is proprietary software. All rights reserved.

## 👥 Authors

- Development Team - Initial work

## 📞 Support

For support and questions, contact the development team.

---

**Version:** 1.0.0  
**Last Updated:** November 19, 2025
