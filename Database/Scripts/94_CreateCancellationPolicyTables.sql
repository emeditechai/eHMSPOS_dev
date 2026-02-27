-- =============================================
-- Cancellation Policy Master + Rules
-- Created: 2026-02-08
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CancellationPolicies')
BEGIN
    CREATE TABLE dbo.CancellationPolicies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchID INT NOT NULL,
        PolicyName NVARCHAR(150) NOT NULL,
        BookingSource NVARCHAR(50) NOT NULL,   -- Website / WalkIn / Phone / OTA / Reference
        CustomerType NVARCHAR(10) NOT NULL,    -- B2C / B2B
        RateType NVARCHAR(20) NOT NULL,        -- Standard / Discounted / NonRefundable
        ValidFrom DATE NULL,
        ValidTo DATE NULL,
        NoShowRefundAllowed BIT NOT NULL CONSTRAINT DF_CancellationPolicies_NoShowRefundAllowed DEFAULT (0),
        ApprovalRequired BIT NOT NULL CONSTRAINT DF_CancellationPolicies_ApprovalRequired DEFAULT (0),
        GatewayFeeDeductionPercent DECIMAL(5,2) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CancellationPolicies_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_CancellationPolicies_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_CancellationPolicies_LastModifiedDate DEFAULT (SYSUTCDATETIME()),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_CancellationPolicies_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );

    CREATE INDEX IX_CancellationPolicies_BranchActive ON dbo.CancellationPolicies(BranchID, IsActive);
    CREATE INDEX IX_CancellationPolicies_Dimensions ON dbo.CancellationPolicies(BranchID, BookingSource, CustomerType, RateType, IsActive);

    PRINT 'Table CancellationPolicies created successfully';
END
ELSE
BEGIN
    PRINT 'Table CancellationPolicies already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CancellationPolicyRules')
BEGIN
    CREATE TABLE dbo.CancellationPolicyRules (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PolicyId INT NOT NULL,
        MinHoursBeforeCheckIn INT NOT NULL,
        MaxHoursBeforeCheckIn INT NOT NULL,
        RefundPercent DECIMAL(5,2) NOT NULL,
        FlatDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_CancellationPolicyRules_FlatDeduction DEFAULT (0),
        GatewayFeeDeductionPercent DECIMAL(5,2) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CancellationPolicyRules_IsActive DEFAULT (1),
        SortOrder INT NOT NULL CONSTRAINT DF_CancellationPolicyRules_SortOrder DEFAULT (0),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_CancellationPolicyRules_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME2 NOT NULL CONSTRAINT DF_CancellationPolicyRules_LastModifiedDate DEFAULT (SYSUTCDATETIME()),
        LastModifiedBy INT NULL,
        CONSTRAINT FK_CancellationPolicyRules_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.CancellationPolicies(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_CancellationPolicyRules_Policy ON dbo.CancellationPolicyRules(PolicyId, IsActive, SortOrder);

    PRINT 'Table CancellationPolicyRules created successfully';
END
ELSE
BEGIN
    PRINT 'Table CancellationPolicyRules already exists';
END
GO
