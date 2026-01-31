-- =============================================
-- Seed Additional Roles
-- Created: 2026-01-31
-- Description: Adds Cashier, Supervisor, Store In-Charge roles
-- Notes:
--   - Safe to run multiple times
--   - Does NOT use IDENTITY_INSERT; lets SQL assign IDs
-- =============================================

USE HotelApp;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Cashier')
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsSystemRole], [CreatedDate], [LastModifiedDate])
    VALUES ('Cashier', 'Cashier role', 1, GETDATE(), GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Supervisor')
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsSystemRole], [CreatedDate], [LastModifiedDate])
    VALUES ('Supervisor', 'Supervisor role', 1, GETDATE(), GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Store In-Charge')
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsSystemRole], [CreatedDate], [LastModifiedDate])
    VALUES ('Store In-Charge', 'Store In-Charge role', 1, GETDATE(), GETDATE());
END
GO
