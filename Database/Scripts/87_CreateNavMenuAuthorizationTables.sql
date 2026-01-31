-- =============================================
-- Nav Menu Authorization Tables
-- Created: 2026-01-31
-- Description: Stores navbar menu items (parent/child) and role-menu mapping
-- =============================================

-- Table: NavMenuItems (self-referencing parent/child)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NavMenuItems')
BEGIN
    CREATE TABLE NavMenuItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        Title NVARCHAR(150) NOT NULL,
        IconClass NVARCHAR(100) NULL,
        Controller NVARCHAR(100) NULL,
        Action NVARCHAR(100) NULL,
        ParentId INT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,

        CONSTRAINT UQ_NavMenuItems_Code UNIQUE (Code),
        CONSTRAINT FK_NavMenuItems_Parent FOREIGN KEY (ParentId) REFERENCES NavMenuItems(Id)
    );

    CREATE INDEX IX_NavMenuItems_ParentId ON NavMenuItems(ParentId);
    CREATE INDEX IX_NavMenuItems_SortOrder ON NavMenuItems(SortOrder);
    CREATE INDEX IX_NavMenuItems_IsActive ON NavMenuItems(IsActive);

    PRINT 'NavMenuItems table created successfully';
END
ELSE
BEGIN
    PRINT 'NavMenuItems table already exists';
END
GO

-- Table: RoleNavMenuItems (role to menu mapping)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleNavMenuItems')
BEGIN
    CREATE TABLE RoleNavMenuItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,
        NavMenuItemId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,

        CONSTRAINT FK_RoleNavMenuItems_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id),
        CONSTRAINT FK_RoleNavMenuItems_NavMenuItems FOREIGN KEY (NavMenuItemId) REFERENCES NavMenuItems(Id),
        CONSTRAINT UQ_RoleNavMenuItems_RoleId_MenuId UNIQUE (RoleId, NavMenuItemId)
    );

    CREATE INDEX IX_RoleNavMenuItems_RoleId ON RoleNavMenuItems(RoleId);
    CREATE INDEX IX_RoleNavMenuItems_NavMenuItemId ON RoleNavMenuItems(NavMenuItemId);
    CREATE INDEX IX_RoleNavMenuItems_IsActive ON RoleNavMenuItems(IsActive);

    PRINT 'RoleNavMenuItems table created successfully';
END
ELSE
BEGIN
    PRINT 'RoleNavMenuItems table already exists';
END
GO
