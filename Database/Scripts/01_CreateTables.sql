-- =============================================
-- Hotel Management System - Database Schema
-- Created: 2025-11-19
-- Description: Core authentication tables
-- =============================================

USE HotelApp;
GO

-- =============================================
-- Table: Roles
-- Description: System and custom roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL UNIQUE,
        [Description] NVARCHAR(500) NULL,
        [IsSystemRole] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_Roles_Name ON [dbo].[Roles]([Name]);
END
GO

-- =============================================
-- Table: Users
-- Description: System users with authentication details
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(100) NOT NULL UNIQUE,
        [Email] NVARCHAR(255) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [Salt] NVARCHAR(255) NULL,
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [Phone] NVARCHAR(20) NULL,
        [FullName] NVARCHAR(200) NULL,
        [Role] INT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsLockedOut] BIT NOT NULL DEFAULT 0,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LastLoginDate] DATETIME2 NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [MustChangePassword] BIT NOT NULL DEFAULT 0,
        [PasswordLastChanged] DATETIME2 NULL,
        [RequiresMFA] BIT NOT NULL DEFAULT 0
    );
    
    CREATE INDEX IX_Users_Username ON [dbo].[Users]([Username]);
    CREATE INDEX IX_Users_Email ON [dbo].[Users]([Email]);
    CREATE INDEX IX_Users_IsActive ON [dbo].[Users]([IsActive]);
END
GO

-- =============================================
-- Table: UserRoles
-- Description: Many-to-many relationship between users and roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [AssignedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [AssignedBy] INT NULL,
        
        CONSTRAINT PK_UserRoles PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_AssignedBy FOREIGN KEY ([AssignedBy]) REFERENCES [dbo].[Users]([Id])
    );
    
    CREATE INDEX IX_UserRoles_UserId ON [dbo].[UserRoles]([UserId]);
    CREATE INDEX IX_UserRoles_RoleId ON [dbo].[UserRoles]([RoleId]);
END
GO

PRINT 'Tables created successfully';
GO
