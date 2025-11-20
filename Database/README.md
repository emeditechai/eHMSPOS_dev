# Database Setup Guide

## Overview
This folder contains SQL scripts for setting up the Hotel Management System database.

## Prerequisites
- SQL Server 2016 or later
- Database: `HotelApp`

## Script Execution Order

1. **01_CreateTables.sql** - Creates core authentication tables
   - Roles
   - Users
   - UserRoles

2. **02_SeedData.sql** - Inserts initial data
   - Default roles (Administrator, Manager, Staff)
   - Default admin user (admin/admin@123)

## Default Admin Credentials

**Username:** `admin`  
**Password:** `admin@123`  
**Email:** admin@restaurant.com

## Running Scripts

### Option 1: SQL Server Management Studio (SSMS)
```sql
-- Create database first
CREATE DATABASE HotelApp;
GO

-- Then execute scripts in order
USE HotelApp;
GO
-- Run 01_CreateTables.sql
-- Run 02_SeedData.sql
```

### Option 2: Command Line (sqlcmd)
```bash
sqlcmd -S localhost -d HotelApp -i 01_CreateTables.sql
sqlcmd -S localhost -d HotelApp -i 02_SeedData.sql
```

### Option 3: Azure Data Studio
1. Connect to your SQL Server instance
2. Create the `HotelApp` database
3. Open and execute `01_CreateTables.sql`
4. Open and execute `02_SeedData.sql`

## Table Structures

### Users Table
Stores user authentication and profile information including BCrypt password hashes.

### Roles Table
Defines system and custom roles for authorization.

### UserRoles Table
Many-to-many relationship linking users to their assigned roles.

## Security Notes

- Passwords are hashed using BCrypt algorithm
- Default admin password should be changed immediately in production
- Account lockout after failed login attempts
- Support for MFA (Multi-Factor Authentication)

## Connection String

Update `appsettings.json` in the web project:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HotelApp;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```
