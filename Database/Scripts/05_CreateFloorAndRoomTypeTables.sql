-- Floor Master Table
CREATE TABLE Floors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FloorName NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy INT NULL,
    LastModifiedBy INT NULL
);

-- Create index on FloorName for faster lookups
CREATE INDEX IX_Floors_FloorName ON Floors(FloorName);

-- Insert seed data for Floors
INSERT INTO Floors (FloorName, IsActive, CreatedDate)
VALUES 
    ('Ground Floor', 1, GETDATE()),
    ('First Floor', 1, GETDATE()),
    ('Second Floor', 1, GETDATE()),
    ('Third Floor', 1, GETDATE()),
    ('Fourth Floor', 1, GETDATE());

PRINT 'Floor Master table created and seeded successfully';
