-- Add BranchID to all existing tables
-- This enables multi-branch support across the entire application

-- Add BranchID to Users table
ALTER TABLE Users ADD BranchID INT NULL;
GO

ALTER TABLE Users ADD CONSTRAINT FK_Users_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Users_BranchID ON Users(BranchID);
GO

-- Update existing users to default HO branch
UPDATE Users SET BranchID = 1 WHERE BranchID IS NULL;
GO

-- Make BranchID required after updating existing data
ALTER TABLE Users ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to Guests table
ALTER TABLE Guests ADD BranchID INT NULL;
GO

ALTER TABLE Guests ADD CONSTRAINT FK_Guests_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Guests_BranchID ON Guests(BranchID);
GO

UPDATE Guests SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE Guests ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to Floors table
ALTER TABLE Floors ADD BranchID INT NULL;
GO

ALTER TABLE Floors ADD CONSTRAINT FK_Floors_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Floors_BranchID ON Floors(BranchID);
GO

UPDATE Floors SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE Floors ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to RoomTypes table
ALTER TABLE RoomTypes ADD BranchID INT NULL;
GO

ALTER TABLE RoomTypes ADD CONSTRAINT FK_RoomTypes_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_RoomTypes_BranchID ON RoomTypes(BranchID);
GO

UPDATE RoomTypes SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE RoomTypes ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to Rooms table
ALTER TABLE Rooms ADD BranchID INT NULL;
GO

ALTER TABLE Rooms ADD CONSTRAINT FK_Rooms_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Rooms_BranchID ON Rooms(BranchID);
GO

UPDATE Rooms SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE Rooms ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to RateMaster table
ALTER TABLE RateMaster ADD BranchID INT NULL;
GO

ALTER TABLE RateMaster ADD CONSTRAINT FK_RateMaster_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_RateMaster_BranchID ON RateMaster(BranchID);
GO

UPDATE RateMaster SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE RateMaster ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to Bookings table
ALTER TABLE Bookings ADD BranchID INT NULL;
GO

ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Bookings_BranchID ON Bookings(BranchID);
GO

UPDATE Bookings SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE Bookings ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to BookingAuditLog table
ALTER TABLE BookingAuditLog ADD BranchID INT NULL;
GO

ALTER TABLE BookingAuditLog ADD CONSTRAINT FK_BookingAuditLog_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_BookingAuditLog_BranchID ON BookingAuditLog(BranchID);
GO

UPDATE BookingAuditLog SET BranchID = 1 WHERE BranchID IS NULL;
GO

ALTER TABLE BookingAuditLog ALTER COLUMN BranchID INT NOT NULL;
GO

-- Add BranchID to Roles table
ALTER TABLE Roles ADD BranchID INT NULL;
GO

ALTER TABLE Roles ADD CONSTRAINT FK_Roles_BranchMaster 
    FOREIGN KEY (BranchID) REFERENCES BranchMaster(BranchID);
GO

CREATE INDEX IX_Roles_BranchID ON Roles(BranchID);
GO

UPDATE Roles SET BranchID = 1 WHERE BranchID IS NULL;
GO

-- For Roles, BranchID can be NULL to allow global roles
-- If you want branch-specific roles only, uncomment the line below
-- ALTER TABLE Roles ALTER COLUMN BranchID INT NOT NULL;
GO
