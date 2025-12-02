-- Create Branch Master Table
-- This table stores information about different branches of the hotel
-- Supports multi-branch operations with HO (Head Office) identification

CREATE TABLE BranchMaster (
    BranchID INT PRIMARY KEY IDENTITY(1,1),
    BranchName NVARCHAR(200) NOT NULL,
    BranchCode NVARCHAR(50) NOT NULL UNIQUE,
    Country NVARCHAR(100) NOT NULL,
    State NVARCHAR(100) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    Pincode NVARCHAR(20) NOT NULL,
    IsHOBranch BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy INT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedBy INT NULL,
    ModifiedDate DATETIME NULL,
    CONSTRAINT CHK_BranchMaster_BranchCode CHECK (LEN(BranchCode) >= 2),
    CONSTRAINT CHK_BranchMaster_Pincode CHECK (LEN(Pincode) >= 5)
);

-- Create index for faster lookups
CREATE INDEX IX_BranchMaster_BranchCode ON BranchMaster(BranchCode);
CREATE INDEX IX_BranchMaster_IsActive ON BranchMaster(IsActive);
CREATE INDEX IX_BranchMaster_IsHOBranch ON BranchMaster(IsHOBranch);

-- Insert default HO Branch
INSERT INTO BranchMaster (BranchName, BranchCode, Country, State, City, Address, Pincode, IsHOBranch, IsActive)
VALUES ('Head Office', 'HO', 'India', 'Maharashtra', 'Mumbai', 'Head Office Address', '400001', 1, 1);

GO
