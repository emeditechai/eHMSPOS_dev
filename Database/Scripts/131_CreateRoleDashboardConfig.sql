-- =============================================
-- Script: 131_CreateRoleDashboardConfig.sql
-- Description: Creates RoleDashboardConfig table and seeds
--              role -> dashboard mappings. Front Office maps
--              to Rooms/Dashboard; all other roles map to
--              Dashboard/Index as a placeholder.
-- Created: 2026-04-17
-- =============================================

USE HMS_dev;
GO

-- Create table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'RoleDashboardConfig'
)
BEGIN
    CREATE TABLE [dbo].[RoleDashboardConfig] (
        [Id]                  INT            IDENTITY(1,1) NOT NULL,
        [RoleId]              INT            NOT NULL,
        [DashboardController] NVARCHAR(100)  NOT NULL,
        [DashboardAction]     NVARCHAR(100)  NOT NULL,
        [DisplayName]         NVARCHAR(200)  NULL,
        [IsActive]            BIT            NOT NULL CONSTRAINT DF_RoleDashboardConfig_IsActive DEFAULT 1,
        [CreatedDate]         DATETIME       NOT NULL CONSTRAINT DF_RoleDashboardConfig_CreatedDate DEFAULT GETDATE(),
        [LastModifiedDate]    DATETIME       NOT NULL CONSTRAINT DF_RoleDashboardConfig_LastModifiedDate DEFAULT GETDATE(),
        CONSTRAINT PK_RoleDashboardConfig PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_RoleDashboardConfig_Roles FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
        CONSTRAINT UQ_RoleDashboardConfig_RoleId UNIQUE ([RoleId])
    );
    PRINT 'Table RoleDashboardConfig created.';
END
ELSE
    PRINT 'Table RoleDashboardConfig already exists.';
GO

-- Seed: one row per role, using CASE to assign the right dashboard
INSERT INTO [dbo].[RoleDashboardConfig] ([RoleId], [DashboardController], [DashboardAction], [DisplayName], [IsActive], [CreatedDate], [LastModifiedDate])
SELECT
    r.[Id],
    CASE r.[Name]
        WHEN 'Front Office' THEN 'Rooms'
        ELSE 'Dashboard'
    END,
    CASE r.[Name]
        WHEN 'Front Office' THEN 'Dashboard'
        ELSE 'Index'
    END,
    CASE r.[Name]
        WHEN 'Administrator'   THEN 'Main Dashboard'
        WHEN 'Manager'         THEN 'Main Dashboard'
        WHEN 'Staff'           THEN 'Main Dashboard'
        WHEN 'Cashier'         THEN 'Main Dashboard'
        WHEN 'Supervisor'      THEN 'Main Dashboard'
        WHEN 'Store In-Charge' THEN 'Main Dashboard'
        WHEN 'Front Office'    THEN 'Room Status Dashboard'
        ELSE                        'Main Dashboard'
    END,
    1,
    GETDATE(),
    GETDATE()
FROM [dbo].[Roles] r
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[RoleDashboardConfig] dc WHERE dc.[RoleId] = r.[Id]
);
PRINT 'RoleDashboardConfig seeded.';
GO
