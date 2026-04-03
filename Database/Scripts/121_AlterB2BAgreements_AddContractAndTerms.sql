-- Expand B2B agreements into contract-style records and add terms/room-rate masters for existing databases

IF OBJECT_ID('dbo.B2BTermsConditions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.B2BTermsConditions
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TermsCode NVARCHAR(30) NOT NULL,
        TermsTitle NVARCHAR(150) NOT NULL,
        TermsType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BTermsConditions_TermsType DEFAULT ('General'),
        CancellationPolicyId INT NULL,
        PaymentTerms NVARCHAR(500) NULL,
        RefundPolicy NVARCHAR(1000) NULL,
        NoShowPolicy NVARCHAR(1000) NULL,
        AmendmentPolicy NVARCHAR(1000) NULL,
        CheckInCheckOutPolicy NVARCHAR(1000) NULL,
        ChildPolicy NVARCHAR(1000) NULL,
        ExtraBedPolicy NVARCHAR(1000) NULL,
        BillingInstructions NVARCHAR(1000) NULL,
        TaxNotes NVARCHAR(1000) NULL,
        LegalDisclaimer NVARCHAR(2000) NULL,
        SpecialConditions NVARCHAR(2000) NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_B2BTermsConditions_IsDefault DEFAULT (0),
        BranchID INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_B2BTermsConditions_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_B2BTermsConditions_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_B2BTermsConditions_Branch_TermsCode UNIQUE (BranchID, TermsCode),
        CONSTRAINT FK_B2BTermsConditions_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );
END
GO

IF COL_LENGTH('dbo.B2BAgreements', 'ContractReference') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD ContractReference NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'AgreementType') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD AgreementType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BAgreements_AgreementType DEFAULT ('Corporate');
GO
IF COL_LENGTH('dbo.B2BAgreements', 'BillingCycle') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD BillingCycle NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'PaymentTerms') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD PaymentTerms NVARCHAR(250) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'SecurityDepositAmount') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD SecurityDepositAmount DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'CreditLimit') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD CreditLimit DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'TermsConditionId') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD TermsConditionId INT NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'CancellationPolicyId') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD CancellationPolicyId INT NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'GstSlabId') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD GstSlabId INT NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'SeasonalRateNotes') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD SeasonalRateNotes NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'BlackoutDatesNotes') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD BlackoutDatesNotes NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IsAmendmentAllowed') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IsAmendmentAllowed BIT NOT NULL CONSTRAINT DF_B2BAgreements_IsAmendmentAllowed DEFAULT (1);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'AmendmentChargeAmount') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD AmendmentChargeAmount DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesBreakfast') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesBreakfast BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesBreakfast DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesLunch') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesLunch BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesLunch DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesDinner') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesDinner BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesDinner DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesLaundry') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesLaundry BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesLaundry DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesAirportTransfer') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesAirportTransfer BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesAirportTransfer DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesWifi') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesWifi BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesWifi DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'IncludesAccessToLounge') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD IncludesAccessToLounge BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesAccessToLounge DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'ServiceRemarks') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD ServiceRemarks NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'ApprovalStatus') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD ApprovalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BAgreements_ApprovalStatus DEFAULT ('Draft');
GO
IF COL_LENGTH('dbo.B2BAgreements', 'ApprovedByUserId') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD ApprovedByUserId INT NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'ApprovedDate') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD ApprovedDate DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'SignedByName') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD SignedByName NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'SignedDate') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD SignedDate DATE NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'SignedDocumentPath') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD SignedDocumentPath NVARCHAR(250) NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'AutoRenew') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD AutoRenew BIT NOT NULL CONSTRAINT DF_B2BAgreements_AutoRenew DEFAULT (0);
GO
IF COL_LENGTH('dbo.B2BAgreements', 'RenewalNoticeDays') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD RenewalNoticeDays INT NULL;
GO
IF COL_LENGTH('dbo.B2BAgreements', 'InternalRemarks') IS NULL
    ALTER TABLE dbo.B2BAgreements ADD InternalRemarks NVARCHAR(1000) NULL;
GO

UPDATE dbo.B2BAgreements
   SET AgreementType = ISNULL(NULLIF(AgreementType, ''), 'Corporate'),
       ApprovalStatus = ISNULL(NULLIF(ApprovalStatus, ''), 'Draft')
 WHERE AgreementType IS NULL OR AgreementType = '' OR ApprovalStatus IS NULL OR ApprovalStatus = '';
GO

IF OBJECT_ID('dbo.B2BAgreementRoomRates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.B2BAgreementRoomRates
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AgreementId INT NOT NULL,
        RoomTypeId INT NOT NULL,
        SeasonLabel NVARCHAR(50) NULL,
        ValidFrom DATE NOT NULL,
        ValidTo DATE NOT NULL,
        BaseRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_B2BAgreementRoomRates_BaseRate DEFAULT (0),
        ContractRate DECIMAL(18,2) NOT NULL,
        ExtraPaxRate DECIMAL(18,2) NOT NULL CONSTRAINT DF_B2BAgreementRoomRates_ExtraPaxRate DEFAULT (0),
        MealPlan NVARCHAR(20) NULL,
        GstSlabId INT NULL,
        Remarks NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_B2BAgreementRoomRates_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_B2BAgreementRoomRates_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BTermsConditions_CancellationPolicy')
    ALTER TABLE dbo.B2BTermsConditions ADD CONSTRAINT FK_B2BTermsConditions_CancellationPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreements_TermsCondition')
    ALTER TABLE dbo.B2BAgreements ADD CONSTRAINT FK_B2BAgreements_TermsCondition FOREIGN KEY (TermsConditionId) REFERENCES dbo.B2BTermsConditions(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreements_CancellationPolicy')
    ALTER TABLE dbo.B2BAgreements ADD CONSTRAINT FK_B2BAgreements_CancellationPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreements_GstSlab')
    ALTER TABLE dbo.B2BAgreements ADD CONSTRAINT FK_B2BAgreements_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreementRoomRates_Agreement')
    ALTER TABLE dbo.B2BAgreementRoomRates ADD CONSTRAINT FK_B2BAgreementRoomRates_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.B2BAgreements(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreementRoomRates_RoomType')
    ALTER TABLE dbo.B2BAgreementRoomRates ADD CONSTRAINT FK_B2BAgreementRoomRates_RoomType FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomTypes(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreementRoomRates_GstSlab')
    ALTER TABLE dbo.B2BAgreementRoomRates ADD CONSTRAINT FK_B2BAgreementRoomRates_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_B2BTermsConditions_Branch_Active' AND object_id = OBJECT_ID('dbo.B2BTermsConditions'))
    CREATE INDEX IX_B2BTermsConditions_Branch_Active ON dbo.B2BTermsConditions(BranchID, IsActive);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_B2BAgreementRoomRates_Agreement' AND object_id = OBJECT_ID('dbo.B2BAgreementRoomRates'))
    CREATE INDEX IX_B2BAgreementRoomRates_Agreement ON dbo.B2BAgreementRoomRates(AgreementId, IsActive);
GO