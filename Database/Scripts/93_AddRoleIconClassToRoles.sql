-- =============================================
-- Add IconClass column to Roles (for UI role icons)
-- Created: 2026-02-01
-- =============================================

-- Add the column if it doesn't exist
IF COL_LENGTH('dbo.Roles', 'IconClass') IS NULL
BEGIN
    ALTER TABLE dbo.Roles
    ADD IconClass NVARCHAR(100) NULL;
END
GO

-- Seed a few sensible defaults (only where empty)
UPDATE dbo.Roles
SET IconClass = CASE
    WHEN Name IN ('Admin', 'Super Admin', 'Administrator') THEN 'fas fa-user-shield'
    WHEN Name = 'Supervisor' THEN 'fas fa-user-tie'
    WHEN Name = 'Cashier' THEN 'fas fa-cash-register'
    WHEN Name IN ('Store In-Charge', 'Store In-Charge ', 'Store In-Charge') THEN 'fas fa-warehouse'
    WHEN Name LIKE '%Store%' THEN 'fas fa-warehouse'
    WHEN Name LIKE '%Manager%' THEN 'fas fa-user-cog'
    ELSE 'fas fa-id-badge'
END
WHERE (IconClass IS NULL OR LTRIM(RTRIM(IconClass)) = '');
GO
