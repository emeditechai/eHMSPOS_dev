-- Create UserBranch mapping table for multi-branch assignment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserBranches')
BEGIN
    CREATE TABLE UserBranches (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        BranchID INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy INT NULL,
        ModifiedDate DATETIME NULL,
        CONSTRAINT FK_UserBranches_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_UserBranches_BranchMaster FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID),
        CONSTRAINT UQ_UserBranches_UserBranch UNIQUE (UserId, BranchID)
    );
    
    PRINT 'UserBranches table created successfully';
END
ELSE
BEGIN
    PRINT 'UserBranches table already exists';
END
GO

-- Create index for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserBranches_UserId')
BEGIN
    CREATE INDEX IX_UserBranches_UserId ON UserBranches(UserId);
    PRINT 'Index IX_UserBranches_UserId created';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserBranches_BranchID')
BEGIN
    CREATE INDEX IX_UserBranches_BranchID ON UserBranches(BranchID);
    PRINT 'Index IX_UserBranches_BranchID created';
END
GO

-- Migrate existing user branch data to UserBranches table
INSERT INTO UserBranches (UserId, BranchID, IsActive, CreatedDate)
SELECT Id, BranchID, 1, GETDATE()
FROM Users
WHERE BranchID IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM UserBranches 
    WHERE UserBranches.UserId = Users.Id 
    AND UserBranches.BranchID = Users.BranchID
);

PRINT 'Existing user branch data migrated to UserBranches table';
GO
