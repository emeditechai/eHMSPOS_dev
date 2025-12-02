-- Create UserRoles junction table for many-to-many relationship
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE UserRoles (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        AssignedDate DATETIME NOT NULL DEFAULT GETDATE(),
        AssignedBy INT NULL,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id),
        CONSTRAINT UQ_UserRoles_UserId_RoleId UNIQUE(UserId, RoleId)
    );
    
    CREATE INDEX IX_UserRoles_UserId ON UserRoles(UserId);
    CREATE INDEX IX_UserRoles_RoleId ON UserRoles(RoleId);
    
    PRINT 'UserRoles table created successfully';
END
ELSE
BEGIN
    PRINT 'UserRoles table already exists';
END
GO

-- Migrate existing Role data from Users table to UserRoles table
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Role')
BEGIN
    -- First check if UserRoles table has the correct columns
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserRoles') AND name = 'IsActive')
    BEGIN
        -- Insert existing user roles into UserRoles table (only where Role is not null)
        INSERT INTO UserRoles (UserId, RoleId, IsActive, AssignedDate, CreatedDate, ModifiedDate)
        SELECT 
            Id as UserId,
            Role as RoleId,
            IsActive,
            GETDATE() as AssignedDate,
            CreatedDate,
            LastModifiedDate as ModifiedDate
        FROM Users
        WHERE Role IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM UserRoles ur 
            WHERE ur.UserId = Users.Id AND ur.RoleId = Users.Role
        );
        
        PRINT 'Existing user role data migrated to UserRoles table';
    END
    ELSE
    BEGIN
        PRINT 'UserRoles table structure is incompatible - please recreate the table';
    END
END
GO
