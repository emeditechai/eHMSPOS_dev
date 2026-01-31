-- =============================================
-- Authorization Matrix Tables (Page + UI Element)
-- Created: 2026-01-31
-- =============================================

-- Table: AuthorizationResources
-- ResourceType: 'Group' | 'Page' | 'Ui'
-- ResourceKey examples:
--   Group: GROUP:ROOMS
--   Page:  PAGE:Booking.List
--   Ui:    UI:Booking.Create.Save
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuthorizationResources')
BEGIN
    CREATE TABLE AuthorizationResources (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ResourceType NVARCHAR(20) NOT NULL,
        ResourceKey NVARCHAR(200) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Controller NVARCHAR(100) NULL,
        Action NVARCHAR(100) NULL,
        ParentResourceId INT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,

        CONSTRAINT UQ_AuthorizationResources_ResourceKey UNIQUE (ResourceKey),
        CONSTRAINT FK_AuthorizationResources_Parent FOREIGN KEY (ParentResourceId) REFERENCES AuthorizationResources(Id)
    );

    CREATE INDEX IX_AuthorizationResources_Type ON AuthorizationResources(ResourceType);
    CREATE INDEX IX_AuthorizationResources_ControllerAction ON AuthorizationResources(Controller, Action);
    CREATE INDEX IX_AuthorizationResources_Parent ON AuthorizationResources(ParentResourceId);
    CREATE INDEX IX_AuthorizationResources_IsActive ON AuthorizationResources(IsActive);

    PRINT 'AuthorizationResources table created successfully';
END
ELSE
BEGIN
    PRINT 'AuthorizationResources table already exists';
END
GO

-- Role scoped permissions (optional BranchID)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuthorizationRolePermissions')
BEGIN
    CREATE TABLE AuthorizationRolePermissions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleId INT NOT NULL,
        ResourceId INT NOT NULL,
        BranchID INT NULL,
        BranchIdNormalized AS (ISNULL(BranchID, 0)) PERSISTED,
        IsAllowed BIT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,

        CONSTRAINT FK_AuthorizationRolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id),
        CONSTRAINT FK_AuthorizationRolePermissions_Resources FOREIGN KEY (ResourceId) REFERENCES AuthorizationResources(Id),
        CONSTRAINT UQ_AuthorizationRolePermissions UNIQUE (RoleId, ResourceId, BranchIdNormalized)
    );

    CREATE INDEX IX_AuthorizationRolePermissions_RoleId ON AuthorizationRolePermissions(RoleId);
    CREATE INDEX IX_AuthorizationRolePermissions_ResourceId ON AuthorizationRolePermissions(ResourceId);
    CREATE INDEX IX_AuthorizationRolePermissions_BranchId ON AuthorizationRolePermissions(BranchID);
    CREATE INDEX IX_AuthorizationRolePermissions_IsActive ON AuthorizationRolePermissions(IsActive);

    PRINT 'AuthorizationRolePermissions table created successfully';
END
ELSE
BEGIN
    PRINT 'AuthorizationRolePermissions table already exists';
END
GO

-- User scoped permissions (optional BranchID) - overrides role permissions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuthorizationUserPermissions')
BEGIN
    CREATE TABLE AuthorizationUserPermissions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        ResourceId INT NOT NULL,
        BranchID INT NULL,
        BranchIdNormalized AS (ISNULL(BranchID, 0)) PERSISTED,
        IsAllowed BIT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,

        CONSTRAINT FK_AuthorizationUserPermissions_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_AuthorizationUserPermissions_Resources FOREIGN KEY (ResourceId) REFERENCES AuthorizationResources(Id),
        CONSTRAINT UQ_AuthorizationUserPermissions UNIQUE (UserId, ResourceId, BranchIdNormalized)
    );

    CREATE INDEX IX_AuthorizationUserPermissions_UserId ON AuthorizationUserPermissions(UserId);
    CREATE INDEX IX_AuthorizationUserPermissions_ResourceId ON AuthorizationUserPermissions(ResourceId);
    CREATE INDEX IX_AuthorizationUserPermissions_BranchId ON AuthorizationUserPermissions(BranchID);
    CREATE INDEX IX_AuthorizationUserPermissions_IsActive ON AuthorizationUserPermissions(IsActive);

    PRINT 'AuthorizationUserPermissions table created successfully';
END
ELSE
BEGIN
    PRINT 'AuthorizationUserPermissions table already exists';
END
GO
