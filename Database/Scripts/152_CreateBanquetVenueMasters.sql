-- ============================================================
-- Script 152: Banquet & Event Management - Master Tables
-- Venue Master, Event Types, Packages (user-configurable GST),
-- Addon Services (user-configurable GST)
-- ============================================================
SET NOCOUNT ON;
GO

-- --------------------------------------------------------
-- 1. BanquetVenues
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetVenues', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetVenues
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetVenues PRIMARY KEY,
        VenueCode       NVARCHAR(30)    NOT NULL,
        VenueName       NVARCHAR(150)   NOT NULL,
        VenueType       NVARCHAR(30)    NOT NULL CONSTRAINT DF_BanquetVenues_VenueType DEFAULT ('Banquet'),
        -- Capacities (all user-configurable)
        CapacitySeated   INT            NOT NULL CONSTRAINT DF_BanquetVenues_CapacitySeated DEFAULT (0),
        CapacityBuffet   INT            NOT NULL CONSTRAINT DF_BanquetVenues_CapacityBuffet DEFAULT (0),
        CapacityTheater  INT            NOT NULL CONSTRAINT DF_BanquetVenues_CapacityTheater DEFAULT (0),
        CapacityCockTail INT            NOT NULL CONSTRAINT DF_BanquetVenues_CapacityCockTail DEFAULT (0),
        Area_SqFt        DECIMAL(10,2)  NULL,
        FloorId          INT            NULL,
        -- Pricing (user defines; GST separately)
        BaseRatePerDay      DECIMAL(18,2) NOT NULL CONSTRAINT DF_BanquetVenues_BaseRatePerDay DEFAULT (0),
        BaseRatePerHalfDay  DECIMAL(18,2) NOT NULL CONSTRAINT DF_BanquetVenues_BaseRatePerHalfDay DEFAULT (0),
        -- GST on venue hire (user-configurable; not hardcoded)
        GSTPercent      DECIMAL(6,2)   NOT NULL CONSTRAINT DF_BanquetVenues_GSTPercent DEFAULT (0),
        CGSTPercent     DECIMAL(6,2)   NOT NULL CONSTRAINT DF_BanquetVenues_CGSTPercent DEFAULT (0),
        SGSTPercent     DECIMAL(6,2)   NOT NULL CONSTRAINT DF_BanquetVenues_SGSTPercent DEFAULT (0),
        IGSTPercent     DECIMAL(6,2)   NOT NULL CONSTRAINT DF_BanquetVenues_IGSTPercent DEFAULT (0),
        -- SAC Code for invoice (user enters)
        SACCode         NVARCHAR(10)   NULL,
        -- Amenities flags
        IsAC            BIT NOT NULL CONSTRAINT DF_BanquetVenues_IsAC DEFAULT (0),
        HasStage        BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasStage DEFAULT (0),
        HasProjector    BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasProjector DEFAULT (0),
        HasSoundSystem  BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasSoundSystem DEFAULT (0),
        HasParking      BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasParking DEFAULT (0),
        HasCatering     BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasCatering DEFAULT (0),
        HasWifi         BIT NOT NULL CONSTRAINT DF_BanquetVenues_HasWifi DEFAULT (0),
        Description     NVARCHAR(1000) NULL,
        PhotoPath       NVARCHAR(500)  NULL,
        BranchID        INT            NOT NULL,
        IsActive        BIT NOT NULL   CONSTRAINT DF_BanquetVenues_IsActive DEFAULT (1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_BanquetVenues_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy       INT NULL,
        UpdatedDate     DATETIME2 NULL,
        UpdatedBy       INT NULL,
        CONSTRAINT UQ_BanquetVenues_Code_Branch UNIQUE (BranchID, VenueCode),
        CONSTRAINT FK_BanquetVenues_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID),
        CONSTRAINT FK_BanquetVenues_Floor FOREIGN KEY (FloorId) REFERENCES dbo.Floors(Id)
    );

    CREATE INDEX IX_BanquetVenues_Branch_Active ON dbo.BanquetVenues(BranchID, IsActive);
    PRINT 'Created dbo.BanquetVenues';
END
ELSE
    PRINT 'dbo.BanquetVenues already exists; skipping.';
GO

-- --------------------------------------------------------
-- 2. BanquetEventTypes
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetEventTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetEventTypes
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetEventTypes PRIMARY KEY,
        EventTypeCode   NVARCHAR(30)    NOT NULL,
        EventTypeName   NVARCHAR(100)   NOT NULL,
        Description     NVARCHAR(500)   NULL,
        IconClass       NVARCHAR(100)   NULL CONSTRAINT DF_BanquetEventTypes_IconClass DEFAULT ('fas fa-calendar-star'),
        BranchID        INT             NOT NULL,
        IsActive        BIT NOT NULL    CONSTRAINT DF_BanquetEventTypes_IsActive DEFAULT (1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_BanquetEventTypes_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy       INT NULL,
        UpdatedDate     DATETIME2 NULL,
        UpdatedBy       INT NULL,
        CONSTRAINT UQ_BanquetEventTypes_Code_Branch UNIQUE (BranchID, EventTypeCode),
        CONSTRAINT FK_BanquetEventTypes_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );
    CREATE INDEX IX_BanquetEventTypes_Branch_Active ON dbo.BanquetEventTypes(BranchID, IsActive);
    PRINT 'Created dbo.BanquetEventTypes';
END
ELSE
    PRINT 'dbo.BanquetEventTypes already exists; skipping.';
GO

-- --------------------------------------------------------
-- 3. BanquetPackages (catering/menu packages - GST user-configurable)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetPackages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetPackages
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetPackages PRIMARY KEY,
        PackageCode          NVARCHAR(30)    NOT NULL,
        PackageName          NVARCHAR(150)   NOT NULL,
        PackageType          NVARCHAR(30)    NOT NULL CONSTRAINT DF_BanquetPackages_PackageType DEFAULT ('VegMenu'),
        PricePerPax          DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BanquetPackages_PricePerPax DEFAULT (0),
        MinimumGuaranteePax  INT             NOT NULL CONSTRAINT DF_BanquetPackages_MinimumGuaranteePax DEFAULT (0),
        -- User-configurable GST (not hardcoded)
        GSTPercent           DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetPackages_GSTPercent DEFAULT (0),
        CGSTPercent          DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetPackages_CGSTPercent DEFAULT (0),
        SGSTPercent          DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetPackages_SGSTPercent DEFAULT (0),
        IGSTPercent          DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetPackages_IGSTPercent DEFAULT (0),
        SACCode              NVARCHAR(10)    NULL,
        -- Menu inclusions
        IncludesStarter      BIT NOT NULL CONSTRAINT DF_BanquetPackages_IncludesStarter DEFAULT (0),
        IncludesMainCourse   BIT NOT NULL CONSTRAINT DF_BanquetPackages_IncludesMainCourse DEFAULT (1),
        IncludesDessert      BIT NOT NULL CONSTRAINT DF_BanquetPackages_IncludesDessert DEFAULT (0),
        IncludesBeverages    BIT NOT NULL CONSTRAINT DF_BanquetPackages_IncludesBeverages DEFAULT (0),
        IncludesLive         BIT NOT NULL CONSTRAINT DF_BanquetPackages_IncludesLive DEFAULT (0),
        MenuDescription      NVARCHAR(MAX)   NULL,
        BranchID             INT             NOT NULL,
        IsActive             BIT NOT NULL    CONSTRAINT DF_BanquetPackages_IsActive DEFAULT (1),
        CreatedDate          DATETIME2 NOT NULL CONSTRAINT DF_BanquetPackages_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy            INT NULL,
        UpdatedDate          DATETIME2 NULL,
        UpdatedBy            INT NULL,
        CONSTRAINT UQ_BanquetPackages_Code_Branch UNIQUE (BranchID, PackageCode),
        CONSTRAINT FK_BanquetPackages_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );
    CREATE INDEX IX_BanquetPackages_Branch_Active ON dbo.BanquetPackages(BranchID, IsActive);
    PRINT 'Created dbo.BanquetPackages';
END
ELSE
    PRINT 'dbo.BanquetPackages already exists; skipping.';
GO

-- --------------------------------------------------------
-- 4. BanquetAddonServices (AV, Decoration, Photography, etc. - GST user-configurable)
-- --------------------------------------------------------
IF OBJECT_ID('dbo.BanquetAddonServices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BanquetAddonServices
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BanquetAddonServices PRIMARY KEY,
        ServiceCode     NVARCHAR(30)    NOT NULL,
        ServiceName     NVARCHAR(150)   NOT NULL,
        ServiceType     NVARCHAR(30)    NOT NULL CONSTRAINT DF_BanquetAddonServices_ServiceType DEFAULT ('Other'),
        Rate            DECIMAL(18,2)   NOT NULL CONSTRAINT DF_BanquetAddonServices_Rate DEFAULT (0),
        RateType        NVARCHAR(20)    NOT NULL CONSTRAINT DF_BanquetAddonServices_RateType DEFAULT ('PerEvent'),
        -- User-configurable GST
        GSTPercent      DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetAddonServices_GSTPercent DEFAULT (0),
        CGSTPercent     DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetAddonServices_CGSTPercent DEFAULT (0),
        SGSTPercent     DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetAddonServices_SGSTPercent DEFAULT (0),
        IGSTPercent     DECIMAL(6,2)    NOT NULL CONSTRAINT DF_BanquetAddonServices_IGSTPercent DEFAULT (0),
        SACCode         NVARCHAR(10)    NULL,
        Description     NVARCHAR(500)   NULL,
        BranchID        INT             NOT NULL,
        IsActive        BIT NOT NULL    CONSTRAINT DF_BanquetAddonServices_IsActive DEFAULT (1),
        CreatedDate     DATETIME2 NOT NULL CONSTRAINT DF_BanquetAddonServices_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CreatedBy       INT NULL,
        UpdatedDate     DATETIME2 NULL,
        UpdatedBy       INT NULL,
        CONSTRAINT UQ_BanquetAddonServices_Code_Branch UNIQUE (BranchID, ServiceCode),
        CONSTRAINT FK_BanquetAddonServices_Branch FOREIGN KEY (BranchID) REFERENCES dbo.BranchMaster(BranchID)
    );
    CREATE INDEX IX_BanquetAddonServices_Branch_Active ON dbo.BanquetAddonServices(BranchID, IsActive);
    PRINT 'Created dbo.BanquetAddonServices';
END
ELSE
    PRINT 'dbo.BanquetAddonServices already exists; skipping.';
GO

-- --------------------------------------------------------
-- 5. Seed default Event Types for all branches
-- --------------------------------------------------------
DECLARE @branchCursor CURSOR;
SET @branchCursor = CURSOR FOR SELECT BranchID FROM dbo.BranchMaster WHERE IsActive = 1;
OPEN @branchCursor;

DECLARE @bid INT;
FETCH NEXT FROM @branchCursor INTO @bid;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Only seed if no event types exist for this branch
    IF NOT EXISTS (SELECT 1 FROM dbo.BanquetEventTypes WHERE BranchID = @bid)
    BEGIN
        INSERT INTO dbo.BanquetEventTypes (EventTypeCode, EventTypeName, Description, IconClass, BranchID, IsActive)
        VALUES
            ('WEDDING',         'Wedding',              'Wedding & reception events',           'fas fa-rings-wedding',    @bid, 1),
            ('CORPORATE',       'Corporate Event',      'Corporate meets, AGMs, conferences',   'fas fa-briefcase',        @bid, 1),
            ('BIRTHDAY',        'Birthday Party',       'Birthday celebrations',                'fas fa-birthday-cake',    @bid, 1),
            ('CONFERENCE',      'Conference',           'Seminars, workshops, training',        'fas fa-chalkboard-user',  @bid, 1),
            ('SOCIAL',          'Social Gathering',     'Social get-togethers, reunions',       'fas fa-people-group',     @bid, 1),
            ('EXHIBITION',      'Exhibition',           'Product launches, exhibitions',        'fas fa-store',            @bid, 1),
            ('ANNIVERSARY',     'Anniversary',          'Anniversary celebrations',             'fas fa-heart',            @bid, 1),
            ('OTHER',           'Other',                'Any other event type',                 'fas fa-calendar-days',    @bid, 1);
    END

    FETCH NEXT FROM @branchCursor INTO @bid;
END

CLOSE @branchCursor;
DEALLOCATE @branchCursor;
GO

PRINT 'Script 152 completed successfully.';
GO
