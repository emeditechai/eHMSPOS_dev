-- Create B2B Client, Agreement, Terms, and GST Slab master tables

IF OBJECT_ID('dbo.B2BClients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.B2BClients
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ClientCode NVARCHAR(30) NOT NULL,
        ClientName NVARCHAR(150) NOT NULL,
        DisplayName NVARCHAR(100) NOT NULL,
        CompanyType NVARCHAR(30) NOT NULL,
        AgreementId INT NULL,
        Pan NVARCHAR(20) NOT NULL,
        ContactPerson NVARCHAR(120) NOT NULL,
        ContactNo NVARCHAR(20) NOT NULL,
        CorporateEmail NVARCHAR(150) NOT NULL,
        AlternateContact NVARCHAR(120) NULL,
        Address NVARCHAR(250) NOT NULL,
        AddressLine2 NVARCHAR(250) NULL,
        City NVARCHAR(100) NOT NULL,
        CountryId INT NOT NULL,
        Pincode NVARCHAR(20) NOT NULL,
        StateId INT NOT NULL,
        IsCreditAllowed BIT NOT NULL CONSTRAINT DF_B2BClients_IsCreditAllowed DEFAULT (0),
        CreditAmount DECIMAL(18,2) NULL,
        CreditDays INT NOT NULL CONSTRAINT DF_B2BClients_CreditDays DEFAULT (0),
        BillingCycle NVARCHAR(20) NOT NULL CONSTRAINT DF_B2BClients_BillingCycle DEFAULT ('Monthly'),
        BillingType NVARCHAR(20) NOT NULL CONSTRAINT DF_B2BClients_BillingType DEFAULT ('Prepaid'),
        OutstandingAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_B2BClients_OutstandingAmount DEFAULT (0),
        AllowExceedLimit BIT NOT NULL CONSTRAINT DF_B2BClients_AllowExceedLimit DEFAULT (0),
        GstNo NVARCHAR(30) NOT NULL,
        Cin NVARCHAR(21) NULL,
        GstRegistrationType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BClients_GstRegistrationType DEFAULT ('Regular'),
        PlaceOfSupplyStateId INT NOT NULL,
        ReverseCharge BIT NOT NULL CONSTRAINT DF_B2BClients_ReverseCharge DEFAULT (0),
        EInvoiceApplicable BIT NOT NULL CONSTRAINT DF_B2BClients_EInvoiceApplicable DEFAULT (0),
        TdsApplicable BIT NOT NULL CONSTRAINT DF_B2BClients_TdsApplicable DEFAULT (0),
        TdsPercentage DECIMAL(5,2) NULL,
        Blacklisted BIT NOT NULL CONSTRAINT DF_B2BClients_Blacklisted DEFAULT (0),
        Remarks NVARCHAR(1000) NULL,
        BranchID INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_B2BClients_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_B2BClients_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_B2BClients_Branch_ClientCode UNIQUE (BranchID, ClientCode),
        CONSTRAINT FK_B2BClients_Country FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id),
        CONSTRAINT FK_B2BClients_State FOREIGN KEY (StateId) REFERENCES dbo.States(Id),
        CONSTRAINT FK_B2BClients_PlaceOfSupplyState FOREIGN KEY (PlaceOfSupplyStateId) REFERENCES dbo.States(Id),
        CONSTRAINT FK_B2BClients_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );

    CREATE INDEX IX_B2BClients_Branch_Active ON dbo.B2BClients(BranchID, IsActive);
END
GO

IF OBJECT_ID('dbo.GstSlabs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GstSlabs
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SlabCode NVARCHAR(30) NOT NULL,
        SlabName NVARCHAR(120) NOT NULL,
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_GstSlabs_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_GstSlabs_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_GstSlabs_SlabCode UNIQUE (SlabCode)
    );

    CREATE INDEX IX_GstSlabs_Active_Effective ON dbo.GstSlabs(IsActive, EffectiveFrom, EffectiveTo);
END
GO

IF OBJECT_ID('dbo.GstSlabBands', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GstSlabBands
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        GstSlabId INT NOT NULL,
        TariffFrom DECIMAL(18,2) NOT NULL,
        TariffTo DECIMAL(18,2) NULL,
        GstPercent DECIMAL(5,2) NOT NULL,
        CgstPercent DECIMAL(5,2) NOT NULL,
        SgstPercent DECIMAL(5,2) NOT NULL,
        IgstPercent DECIMAL(5,2) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_GstSlabBands_SortOrder DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_GstSlabBands_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_GstSlabBands_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT FK_GstSlabBands_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id)
    );

    CREATE INDEX IX_GstSlabBands_GstSlab_SortOrder ON dbo.GstSlabBands(GstSlabId, SortOrder, TariffFrom);
END
GO

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
        CONSTRAINT FK_B2BTermsConditions_CancellationPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id),
        CONSTRAINT FK_B2BTermsConditions_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );

    CREATE INDEX IX_B2BTermsConditions_Branch_Active ON dbo.B2BTermsConditions(BranchID, IsActive);
END
GO

IF OBJECT_ID('dbo.B2BAgreements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.B2BAgreements
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AgreementCode NVARCHAR(30) NOT NULL,
        AgreementName NVARCHAR(150) NOT NULL,
        ContractReference NVARCHAR(50) NULL,
        AgreementType NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BAgreements_AgreementType DEFAULT ('Corporate'),
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NOT NULL,
        BillingType NVARCHAR(30) NOT NULL,
        CreditDays INT NOT NULL CONSTRAINT DF_B2BAgreements_CreditDays DEFAULT (0),
        BillingCycle NVARCHAR(20) NULL,
        PaymentTerms NVARCHAR(250) NULL,
        SecurityDepositAmount DECIMAL(18,2) NULL,
        CreditLimit DECIMAL(18,2) NULL,
        RatePlanType NVARCHAR(30) NOT NULL,
        DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_B2BAgreements_DiscountPercent DEFAULT (0),
        MealPlan NVARCHAR(20) NULL,
        TermsConditionId INT NULL,
        CancellationPolicyId INT NULL,
        GstSlabId INT NULL,
        SeasonalRateNotes NVARCHAR(500) NULL,
        BlackoutDatesNotes NVARCHAR(500) NULL,
        IsAmendmentAllowed BIT NOT NULL CONSTRAINT DF_B2BAgreements_IsAmendmentAllowed DEFAULT (1),
        AmendmentChargeAmount DECIMAL(18,2) NULL,
        IncludesBreakfast BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesBreakfast DEFAULT (0),
        IncludesLunch BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesLunch DEFAULT (0),
        IncludesDinner BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesDinner DEFAULT (0),
        IncludesLaundry BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesLaundry DEFAULT (0),
        IncludesAirportTransfer BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesAirportTransfer DEFAULT (0),
        IncludesWifi BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesWifi DEFAULT (0),
        IncludesAccessToLounge BIT NOT NULL CONSTRAINT DF_B2BAgreements_IncludesAccessToLounge DEFAULT (0),
        ServiceRemarks NVARCHAR(500) NULL,
        ApprovalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_B2BAgreements_ApprovalStatus DEFAULT ('Draft'),
        ApprovedByUserId INT NULL,
        ApprovedDate DATETIME2 NULL,
        SignedByName NVARCHAR(120) NULL,
        SignedDate DATE NULL,
        SignedDocumentPath NVARCHAR(250) NULL,
        AutoRenew BIT NOT NULL CONSTRAINT DF_B2BAgreements_AutoRenew DEFAULT (0),
        RenewalNoticeDays INT NULL,
        Remarks NVARCHAR(500) NULL,
        InternalRemarks NVARCHAR(1000) NULL,
        BranchID INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_B2BAgreements_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_B2BAgreements_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_B2BAgreements_Branch_AgreementCode UNIQUE (BranchID, AgreementCode),
        CONSTRAINT FK_B2BAgreements_TermsCondition FOREIGN KEY (TermsConditionId) REFERENCES dbo.B2BTermsConditions(Id),
        CONSTRAINT FK_B2BAgreements_CancellationPolicy FOREIGN KEY (CancellationPolicyId) REFERENCES dbo.CancellationPolicies(Id),
        CONSTRAINT FK_B2BAgreements_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id),
        CONSTRAINT FK_B2BAgreements_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );

    CREATE INDEX IX_B2BAgreements_Branch_Active ON dbo.B2BAgreements(BranchID, IsActive);
END
GO

IF OBJECT_ID('dbo.B2BAgreements', 'U') IS NOT NULL
    AND COL_LENGTH('dbo.B2BClients', 'AgreementId') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BClients_Agreement')
BEGIN
    ALTER TABLE dbo.B2BClients ADD CONSTRAINT FK_B2BClients_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.B2BAgreements(Id);
END
GO

IF OBJECT_ID('dbo.B2BClients', 'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_B2BClients_AgreementId' AND object_id = OBJECT_ID('dbo.B2BClients'))
BEGIN
    CREATE INDEX IX_B2BClients_AgreementId ON dbo.B2BClients(AgreementId);
END
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
        UpdatedBy INT NULL,
        CONSTRAINT FK_B2BAgreementRoomRates_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.B2BAgreements(Id),
        CONSTRAINT FK_B2BAgreementRoomRates_RoomType FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomTypes(Id),
        CONSTRAINT FK_B2BAgreementRoomRates_GstSlab FOREIGN KEY (GstSlabId) REFERENCES dbo.GstSlabs(Id)
    );

    CREATE INDEX IX_B2BAgreementRoomRates_Agreement ON dbo.B2BAgreementRoomRates(AgreementId, IsActive);
END
GO