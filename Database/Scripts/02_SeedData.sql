-- =============================================
-- Hotel Management System - Seed Data
-- Created: 2025-11-19
-- Description: Initial roles and admin user
-- =============================================

USE HotelApp;
GO

-- =============================================
-- Seed Roles
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Roles] ON;
    
    INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [IsSystemRole], [CreatedDate], [LastModifiedDate])
    VALUES 
        (1, 'Administrator', 'Full system access', 1, '2025-09-03 20:56:42.370', '2025-09-03 20:56:42.370'),
        (2, 'Manager', 'Hotel management access', 1, '2025-09-03 20:56:42.370', '2025-09-03 20:56:42.370'),
        (3, 'Staff', 'Front desk and operations', 1, '2025-09-03 20:56:42.370', '2025-09-03 20:56:42.370');
    
    SET IDENTITY_INSERT [dbo].[Roles] OFF;
    
    PRINT 'Roles seeded successfully';
END
ELSE
BEGIN
    PRINT 'Roles already exist, skipping seed';
END
GO

-- =============================================
-- Seed Default Admin User
-- Username: admin
-- Password: admin@123
-- BCrypt Hash: $2a$12$jfLvB6D7RyZxZYYmE9stKOYQALsjD/91rp5yM8JU4j3PQqB2ylOgW
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    SET IDENTITY_INSERT [dbo].[Users] ON;
    
    INSERT INTO [dbo].[Users] 
    (
        [Id],
        [Username],
        [Email],
        [PasswordHash],
        [Salt],
        [FirstName],
        [LastName],
        [PhoneNumber],
        [Phone],
        [FullName],
        [Role],
        [IsActive],
        [IsLockedOut],
        [FailedLoginAttempts],
        [LastLoginDate],
        [CreatedDate],
        [LastModifiedDate],
        [MustChangePassword],
        [PasswordLastChanged],
        [RequiresMFA]
    )
    VALUES 
    (
        1,
        'admin',
        'admin@restaurant.com',
        '$2a$12$jfLvB6D7RyZxZYYmE9stKOYQALsjD/91rp5yM8JU4j3PQqB2ylOgW',
        '$2a$12$9Yf2le/PNxMYI9sNuGdWqO',
        'Super',
        'Admin',
        '8617280732',
        '8617280732',
        'Super Admin',
        3,
        1,
        0,
        0,
        NULL,
        '2025-09-03 20:56:45.183',
        '2025-09-03 20:56:45.183',
        0,
        NULL,
        0
    );
    
    SET IDENTITY_INSERT [dbo].[Users] OFF;
    
    PRINT 'Admin user created successfully';
END
ELSE
BEGIN
    PRINT 'Admin user already exists, skipping seed';
END
GO

-- =============================================
-- Assign Administrator Role to Admin User
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = 1 AND [RoleId] = 1)
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [AssignedDate], [AssignedBy])
    VALUES (1, 1, '2025-11-18 19:26:00.773', NULL);
    
    PRINT 'Administrator role assigned to admin user';
END
ELSE
BEGIN
    PRINT 'Admin user role already assigned, skipping';
END
GO

PRINT 'Seed data completed successfully';
GO
