USE HMS_dev;
GO

IF COL_LENGTH('dbo.Bookings', 'B2BClientId') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD B2BClientId INT NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'B2BClientCode') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD B2BClientCode NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'B2BClientName') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD B2BClientName NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'B2BAgreementId') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD B2BAgreementId INT NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'AgreementCode') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD AgreementCode NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'AgreementName') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD AgreementName NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'GstSlabId') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD GstSlabId INT NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'GstSlabCode') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD GstSlabCode NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'GstSlabName') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD GstSlabName NVARCHAR(120) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CompanyContactPerson') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CompanyContactPerson NVARCHAR(120) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CompanyContactNo') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CompanyContactNo NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CompanyEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CompanyEmail NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CompanyGstNo') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CompanyGstNo NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'BillingAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD BillingAddress NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'BillingStateName') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD BillingStateName NVARCHAR(120) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'BillingPincode') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD BillingPincode NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'BillingType') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD BillingType NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'BillingTo') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD BillingTo NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CreditDays') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CreditDays INT NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'MealPlan') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD MealPlan NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CorporateDiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CorporateDiscountPercent DECIMAL(5,2) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'CompanyCreditLimit') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD CompanyCreditLimit DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.Bookings', 'IsCreditAllowed') IS NULL
BEGIN
    ALTER TABLE dbo.Bookings ADD IsCreditAllowed BIT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Bookings_B2BClient'
)
AND COL_LENGTH('dbo.Bookings', 'B2BClientId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Bookings WITH CHECK
        ADD CONSTRAINT FK_Bookings_B2BClient FOREIGN KEY (B2BClientId) REFERENCES dbo.B2BClients(Id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Bookings_B2BAgreement'
)
AND COL_LENGTH('dbo.Bookings', 'B2BAgreementId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Bookings WITH CHECK
        ADD CONSTRAINT FK_Bookings_B2BAgreement FOREIGN KEY (B2BAgreementId) REFERENCES dbo.B2BAgreements(Id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Bookings_GstSlab'
)
AND COL_LENGTH('dbo.Bookings', 'GstSlabId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Bookings WITH CHECK
        ADD CONSTRAINT FK_Bookings_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_B2BClientId' AND object_id = OBJECT_ID('dbo.Bookings')
)
BEGIN
    CREATE INDEX IX_Bookings_B2BClientId ON dbo.Bookings(B2BClientId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_B2BAgreementId' AND object_id = OBJECT_ID('dbo.Bookings')
)
BEGIN
    CREATE INDEX IX_Bookings_B2BAgreementId ON dbo.Bookings(B2BAgreementId);
END
GO

PRINT 'B2B booking flow columns ensured on Bookings table.';
GO