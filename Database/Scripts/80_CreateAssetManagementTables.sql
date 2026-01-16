SET NOCOUNT ON;

/*
Asset Management - internal inventory (no procurement)
Covers:
- Item master + department eligibility + consumable standards
- Stock movements (IN/OUT/Transfer/Usage/Return)
- Current stock balances
- Allocations (dept/room/guest)
- Damage/Loss + Recovery
*/

IF OBJECT_ID('dbo.AssetDepartments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetDepartments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetDepartments PRIMARY KEY,
        BranchID INT NOT NULL,
        [Name] NVARCHAR(80) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AssetDepartments_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetDepartments_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_AssetDepartments_BranchID_Name UNIQUE (BranchID, [Name])
    );
END

IF OBJECT_ID('dbo.AssetUnits', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetUnits
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetUnits PRIMARY KEY,
        BranchID INT NOT NULL,
        [Name] NVARCHAR(30) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AssetUnits_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetUnits_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_AssetUnits_BranchID_Name UNIQUE (BranchID, [Name])
    );
END

IF OBJECT_ID('dbo.AssetItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetItems PRIMARY KEY,
        BranchID INT NOT NULL,
        Code NVARCHAR(30) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        Category INT NOT NULL, -- 1=Asset, 2=Reusable, 3=Consumable
        UnitId INT NOT NULL,
        IsRoomEligible BIT NOT NULL CONSTRAINT DF_AssetItems_IsRoomEligible DEFAULT(0),
        IsChargeable BIT NOT NULL CONSTRAINT DF_AssetItems_IsChargeable DEFAULT(0),
        ThresholdQty DECIMAL(18,2) NULL, -- for consumables
        RequiresCustodian BIT NOT NULL CONSTRAINT DF_AssetItems_RequiresCustodian DEFAULT(1),
        IsActive BIT NOT NULL CONSTRAINT DF_AssetItems_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetItems_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_AssetItems_Code_BranchID UNIQUE (Code, BranchID),
        CONSTRAINT FK_AssetItems_Unit FOREIGN KEY (UnitId) REFERENCES dbo.AssetUnits(Id)
    );

    CREATE INDEX IX_AssetItems_BranchID_IsActive
        ON dbo.AssetItems (BranchID, IsActive)
        INCLUDE (Code, [Name], Category, UnitId, IsRoomEligible, IsChargeable, ThresholdQty, RequiresCustodian);
END

IF OBJECT_ID('dbo.AssetItemDepartments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetItemDepartments
    (
        ItemId INT NOT NULL,
        DepartmentId INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetItemDepartments_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        CONSTRAINT PK_AssetItemDepartments PRIMARY KEY (ItemId, DepartmentId),
        CONSTRAINT FK_AssetItemDepartments_Item FOREIGN KEY (ItemId) REFERENCES dbo.AssetItems(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AssetItemDepartments_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.AssetDepartments(Id) ON DELETE CASCADE
    );
END

IF OBJECT_ID('dbo.AssetConsumableStandards', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetConsumableStandards
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetConsumableStandards PRIMARY KEY,
        BranchID INT NOT NULL,
        ItemId INT NOT NULL,
        PerRoomPerDayQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_AssetConsumableStandards_PerRoomPerDayQty DEFAULT(0),
        PerStayQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_AssetConsumableStandards_PerStayQty DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_AssetConsumableStandards_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetConsumableStandards_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        UpdatedDate DATETIME2 NULL,
        UpdatedBy INT NULL,
        CONSTRAINT UQ_AssetConsumableStandards_BranchID_Item UNIQUE (BranchID, ItemId),
        CONSTRAINT FK_AssetConsumableStandards_Item FOREIGN KEY (ItemId) REFERENCES dbo.AssetItems(Id) ON DELETE CASCADE
    );
END

IF OBJECT_ID('dbo.AssetStockBalances', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetStockBalances
    (
        BranchID INT NOT NULL,
        ItemId INT NOT NULL,
        OnHandQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_AssetStockBalances_OnHandQty DEFAULT(0),
        UpdatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetStockBalances_UpdatedDate DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_AssetStockBalances PRIMARY KEY (BranchID, ItemId),
        CONSTRAINT FK_AssetStockBalances_Item FOREIGN KEY (ItemId) REFERENCES dbo.AssetItems(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AssetStockBalances_BranchID_OnHand
        ON dbo.AssetStockBalances (BranchID, OnHandQty)
        INCLUDE (ItemId, UpdatedDate);
END

IF OBJECT_ID('dbo.AssetMovements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetMovements
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetMovements PRIMARY KEY,
        BranchID INT NOT NULL,
        MovementType INT NOT NULL,
        MovementDate DATETIME2 NOT NULL CONSTRAINT DF_AssetMovements_MovementDate DEFAULT(SYSUTCDATETIME()),

        BookingId INT NULL,
        BookingNumber NVARCHAR(50) NULL,
        RoomId INT NULL,
        FromDepartmentId INT NULL,
        ToDepartmentId INT NULL,
        GuestName NVARCHAR(120) NULL,
        CustodianName NVARCHAR(120) NULL,

        Notes NVARCHAR(500) NULL,
        AllowNegativeOverride BIT NOT NULL CONSTRAINT DF_AssetMovements_AllowNegativeOverride DEFAULT(0),

        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetMovements_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL
    );

    CREATE INDEX IX_AssetMovements_BranchID_MovementDate
        ON dbo.AssetMovements (BranchID, MovementDate DESC, Id DESC)
        INCLUDE (MovementType, BookingNumber, RoomId, FromDepartmentId, ToDepartmentId, GuestName);
END

IF OBJECT_ID('dbo.AssetMovementLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetMovementLines
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetMovementLines PRIMARY KEY,
        MovementId INT NOT NULL,
        ItemId INT NOT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        SerialNumber NVARCHAR(100) NULL,
        LineNote NVARCHAR(250) NULL,
        CONSTRAINT FK_AssetMovementLines_Movement FOREIGN KEY (MovementId) REFERENCES dbo.AssetMovements(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AssetMovementLines_Item FOREIGN KEY (ItemId) REFERENCES dbo.AssetItems(Id)
    );

    CREATE INDEX IX_AssetMovementLines_MovementId
        ON dbo.AssetMovementLines (MovementId);
END

IF OBJECT_ID('dbo.AssetAllocations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetAllocations
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetAllocations PRIMARY KEY,
        BranchID INT NOT NULL,
        AllocationType INT NOT NULL, -- 1=Department, 2=Room, 3=Guest
        ItemId INT NOT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        DepartmentId INT NULL,
        RoomId INT NULL,
        BookingId INT NULL,
        BookingNumber NVARCHAR(50) NULL,
        GuestName NVARCHAR(120) NULL,
        CustodianName NVARCHAR(120) NOT NULL,
        IsFixed BIT NOT NULL CONSTRAINT DF_AssetAllocations_IsFixed DEFAULT(0),
        IssuedOn DATETIME2 NOT NULL CONSTRAINT DF_AssetAllocations_IssuedOn DEFAULT(SYSUTCDATETIME()),
        ExpectedReturnDate DATE NULL,
        ReturnedOn DATETIME2 NULL,
        Status INT NOT NULL CONSTRAINT DF_AssetAllocations_Status DEFAULT(1), -- 1=Open, 2=Closed
        SourceMovementId INT NULL,
        ClosedMovementId INT NULL,
        ClosedBy INT NULL,
        CONSTRAINT FK_AssetAllocations_Item FOREIGN KEY (ItemId) REFERENCES dbo.AssetItems(Id),
        CONSTRAINT FK_AssetAllocations_SourceMovement FOREIGN KEY (SourceMovementId) REFERENCES dbo.AssetMovements(Id)
    );

    CREATE INDEX IX_AssetAllocations_BranchID_Status
        ON dbo.AssetAllocations (BranchID, Status)
        INCLUDE (AllocationType, ItemId, DepartmentId, RoomId, BookingNumber, GuestName, CustodianName, Qty, IsFixed, IssuedOn);
END

IF OBJECT_ID('dbo.AssetDamageLoss', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetDamageLoss
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetDamageLoss PRIMARY KEY,
        BranchID INT NOT NULL,
        ItemId INT NOT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        IssueType INT NOT NULL, -- 1=Damage, 2=Loss
        Reason NVARCHAR(300) NOT NULL,

        BookingId INT NULL,
        BookingNumber NVARCHAR(50) NULL,
        RoomId INT NULL,
        DepartmentId INT NULL,
        GuestName NVARCHAR(120) NULL,

        Status INT NOT NULL CONSTRAINT DF_AssetDamageLoss_Status DEFAULT(1), -- 1=Pending,2=Approved,3=Recovered,4=Closed
        ReportedOn DATETIME2 NOT NULL CONSTRAINT DF_AssetDamageLoss_ReportedOn DEFAULT(SYSUTCDATETIME()),
        ReportedBy INT NULL,
        ApprovedOn DATETIME2 NULL,
        ApprovedBy INT NULL,

        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetDamageLoss_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL
    );

    CREATE INDEX IX_AssetDamageLoss_BranchID_Status
        ON dbo.AssetDamageLoss (BranchID, Status, Id DESC)
        INCLUDE (ItemId, Qty, IssueType, BookingNumber, RoomId, DepartmentId, GuestName, ReportedOn);
END

IF OBJECT_ID('dbo.AssetDamageLossRecoveries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetDamageLossRecoveries
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetDamageLossRecoveries PRIMARY KEY,
        DamageLossId INT NOT NULL,
        RecoveryType INT NOT NULL, -- 1=Cash, 2=BillPosting, 3=Replacement, 4=StaffDeduction
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_AssetDamageLossRecoveries_Amount DEFAULT(0),
        Notes NVARCHAR(500) NULL,
        BookingOtherChargeId INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_AssetDamageLossRecoveries_CreatedDate DEFAULT(SYSUTCDATETIME()),
        CreatedBy INT NULL,
        CONSTRAINT FK_AssetDamageLossRecoveries_DamageLoss FOREIGN KEY (DamageLossId) REFERENCES dbo.AssetDamageLoss(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AssetDamageLossRecoveries_DamageLossId
        ON dbo.AssetDamageLossRecoveries (DamageLossId, Id DESC);
END
