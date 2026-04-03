IF COL_LENGTH('dbo.B2BClients', 'DisplayName') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD DisplayName NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'DisplayName') IS NOT NULL
BEGIN
    UPDATE dbo.B2BClients SET DisplayName = LEFT(ClientName, 100) WHERE DisplayName IS NULL;
    ALTER TABLE dbo.B2BClients ALTER COLUMN DisplayName NVARCHAR(100) NOT NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'CompanyType') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD CompanyType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BClients_CompanyType DEFAULT ('Corporate');
END
GO

IF COL_LENGTH('dbo.B2BClients', 'Pan') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD Pan NVARCHAR(20) NOT NULL CONSTRAINT DF_B2BClients_Pan DEFAULT ('');
END
GO

IF COL_LENGTH('dbo.B2BClients', 'AlternateContact') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD AlternateContact NVARCHAR(120) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'AddressLine2') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD AddressLine2 NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'City') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD City NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'City') IS NOT NULL
BEGIN
    UPDATE dbo.B2BClients SET City = 'Unknown' WHERE City IS NULL;
    ALTER TABLE dbo.B2BClients ALTER COLUMN City NVARCHAR(100) NOT NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'CountryId') IS NULL
BEGIN
    DECLARE @DefaultCountryId INT;
    SELECT TOP (1) @DefaultCountryId = Id FROM dbo.Countries WHERE Code = 'IN' ORDER BY Id;
    IF @DefaultCountryId IS NULL
        SELECT TOP (1) @DefaultCountryId = Id FROM dbo.Countries ORDER BY Id;

    ALTER TABLE dbo.B2BClients ADD CountryId INT NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'CountryId') IS NOT NULL
BEGIN
    DECLARE @DefaultCountryId INT;
    SELECT TOP (1) @DefaultCountryId = Id FROM dbo.Countries WHERE Code = 'IN' ORDER BY Id;
    IF @DefaultCountryId IS NULL
        SELECT TOP (1) @DefaultCountryId = Id FROM dbo.Countries ORDER BY Id;

    UPDATE dbo.B2BClients SET CountryId = @DefaultCountryId WHERE CountryId IS NULL;
    ALTER TABLE dbo.B2BClients ALTER COLUMN CountryId INT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BClients_Country')
    BEGIN
        ALTER TABLE dbo.B2BClients WITH CHECK ADD CONSTRAINT FK_B2BClients_Country FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id);
    END
END
GO

IF COL_LENGTH('dbo.B2BClients', 'CreditDays') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD CreditDays INT NOT NULL CONSTRAINT DF_B2BClients_CreditDays DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'BillingCycle') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD BillingCycle NVARCHAR(20) NOT NULL CONSTRAINT DF_B2BClients_BillingCycle DEFAULT ('Monthly');
END
GO

IF COL_LENGTH('dbo.B2BClients', 'BillingType') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD BillingType NVARCHAR(20) NOT NULL CONSTRAINT DF_B2BClients_BillingType DEFAULT ('Prepaid');
END
GO

IF COL_LENGTH('dbo.B2BClients', 'BillingType') IS NOT NULL
BEGIN
    UPDATE dbo.B2BClients SET BillingType = CASE WHEN ISNULL(IsCreditAllowed, 0) = 1 THEN 'Credit' ELSE 'Prepaid' END WHERE BillingType IS NULL OR LTRIM(RTRIM(BillingType)) = '';
END
GO

IF COL_LENGTH('dbo.B2BClients', 'OutstandingAmount') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD OutstandingAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_B2BClients_OutstandingAmount DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'AllowExceedLimit') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD AllowExceedLimit BIT NOT NULL CONSTRAINT DF_B2BClients_AllowExceedLimit DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'Cin') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD Cin NVARCHAR(21) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'GstRegistrationType') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD GstRegistrationType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BClients_GstRegistrationType DEFAULT ('Regular');
END
GO

IF COL_LENGTH('dbo.B2BClients', 'PlaceOfSupplyStateId') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD PlaceOfSupplyStateId INT NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'PlaceOfSupplyStateId') IS NOT NULL
BEGIN
    UPDATE dbo.B2BClients SET PlaceOfSupplyStateId = StateId WHERE PlaceOfSupplyStateId IS NULL;
    ALTER TABLE dbo.B2BClients ALTER COLUMN PlaceOfSupplyStateId INT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BClients_PlaceOfSupplyState')
    BEGIN
        ALTER TABLE dbo.B2BClients WITH CHECK ADD CONSTRAINT FK_B2BClients_PlaceOfSupplyState FOREIGN KEY (PlaceOfSupplyStateId) REFERENCES dbo.States(Id);
    END
END
GO

IF COL_LENGTH('dbo.B2BClients', 'ReverseCharge') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD ReverseCharge BIT NOT NULL CONSTRAINT DF_B2BClients_ReverseCharge DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'EInvoiceApplicable') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD EInvoiceApplicable BIT NOT NULL CONSTRAINT DF_B2BClients_EInvoiceApplicable DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'TdsApplicable') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD TdsApplicable BIT NOT NULL CONSTRAINT DF_B2BClients_TdsApplicable DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'TdsPercentage') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD TdsPercentage DECIMAL(5,2) NULL;
END
GO

IF COL_LENGTH('dbo.B2BClients', 'Blacklisted') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD Blacklisted BIT NOT NULL CONSTRAINT DF_B2BClients_Blacklisted DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.B2BClients', 'Remarks') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD Remarks NVARCHAR(1000) NULL;
END
GO