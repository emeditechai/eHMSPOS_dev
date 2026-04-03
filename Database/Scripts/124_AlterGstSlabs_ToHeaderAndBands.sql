-- Convert GST slab storage from one-row-per-band into GST header + child tariff bands.

IF OBJECT_ID('dbo.GstSlabs', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.GstSlabs', 'TariffFrom') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN TariffFrom DECIMAL(18,2) NULL;
    END

    IF COL_LENGTH('dbo.GstSlabs', 'TariffTo') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN TariffTo DECIMAL(18,2) NULL;
    END

    IF COL_LENGTH('dbo.GstSlabs', 'GstPercent') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN GstPercent DECIMAL(5,2) NULL;
    END

    IF COL_LENGTH('dbo.GstSlabs', 'CgstPercent') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN CgstPercent DECIMAL(5,2) NULL;
    END

    IF COL_LENGTH('dbo.GstSlabs', 'SgstPercent') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN SgstPercent DECIMAL(5,2) NULL;
    END

    IF COL_LENGTH('dbo.GstSlabs', 'IgstPercent') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.GstSlabs ALTER COLUMN IgstPercent DECIMAL(5,2) NULL;
    END
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
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GstSlabs_Active_Effective' AND object_id = OBJECT_ID('dbo.GstSlabs'))
BEGIN
    CREATE INDEX IX_GstSlabs_Active_Effective ON dbo.GstSlabs(IsActive, EffectiveFrom, EffectiveTo);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GstSlabBands_GstSlab_SortOrder' AND object_id = OBJECT_ID('dbo.GstSlabBands'))
BEGIN
    CREATE INDEX IX_GstSlabBands_GstSlab_SortOrder ON dbo.GstSlabBands(GstSlabId, SortOrder, TariffFrom);
END
GO

IF COL_LENGTH('dbo.GstSlabs', 'TariffFrom') IS NOT NULL
   AND COL_LENGTH('dbo.GstSlabs', 'GstPercent') IS NOT NULL
BEGIN
    INSERT INTO dbo.GstSlabBands
        (GstSlabId, TariffFrom, TariffTo, GstPercent, CgstPercent, SgstPercent, IgstPercent, SortOrder, IsActive, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
    SELECT gs.Id,
           ISNULL(gs.TariffFrom, 0),
           gs.TariffTo,
           ISNULL(gs.GstPercent, 0),
           ISNULL(gs.CgstPercent, ISNULL(gs.GstPercent, 0) / 2),
           ISNULL(gs.SgstPercent, ISNULL(gs.GstPercent, 0) / 2),
           ISNULL(gs.IgstPercent, ISNULL(gs.GstPercent, 0)),
           1,
           gs.IsActive,
           gs.CreatedDate,
           gs.CreatedBy,
           gs.UpdatedDate,
           gs.UpdatedBy
      FROM dbo.GstSlabs gs
     WHERE NOT EXISTS (SELECT 1 FROM dbo.GstSlabBands band WHERE band.GstSlabId = gs.Id)
       AND (gs.TariffFrom IS NOT NULL OR gs.GstPercent IS NOT NULL);
END
GO