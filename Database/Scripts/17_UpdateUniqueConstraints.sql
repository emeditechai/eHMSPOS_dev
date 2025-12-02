-- Update unique constraints to enforce branch-level uniqueness for all master tables

-- Drop old unique constraint on RoomTypes.TypeName (global uniqueness)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ__RoomType__D4E7DFA8377CD358' AND object_id = OBJECT_ID('RoomTypes'))
BEGIN
    ALTER TABLE RoomTypes DROP CONSTRAINT UQ__RoomType__D4E7DFA8377CD358;
    PRINT 'Dropped old TypeName unique constraint from RoomTypes';
END
ELSE IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('RoomTypes') AND is_unique = 1)
BEGIN
    DECLARE @ConstraintName NVARCHAR(200);
    SELECT @ConstraintName = name FROM sys.indexes WHERE object_id = OBJECT_ID('RoomTypes') AND is_unique = 1 AND name LIKE 'UQ%';
    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE RoomTypes DROP CONSTRAINT ' + @ConstraintName);
        PRINT 'Dropped unique constraint: ' + @ConstraintName;
    END
END
GO

-- Create new unique constraint on RoomTypes (TypeName + BranchID)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_RoomTypes_TypeName_BranchID' AND object_id = OBJECT_ID('RoomTypes'))
BEGIN
    ALTER TABLE RoomTypes ADD CONSTRAINT UQ_RoomTypes_TypeName_BranchID UNIQUE (TypeName, BranchID);
    PRINT 'Created new unique constraint UQ_RoomTypes_TypeName_BranchID';
END
GO

-- Drop old unique constraint on Floors.FloorName if exists
DECLARE @FloorConstraint NVARCHAR(200);
SELECT @FloorConstraint = name FROM sys.indexes WHERE object_id = OBJECT_ID('Floors') AND is_unique = 1 AND name LIKE 'UQ%';
IF @FloorConstraint IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Floors DROP CONSTRAINT ' + @FloorConstraint);
    PRINT 'Dropped old FloorName unique constraint: ' + @FloorConstraint;
END
GO

-- Create new unique constraint on Floors (FloorName + BranchID)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Floors_FloorName_BranchID' AND object_id = OBJECT_ID('Floors'))
BEGIN
    ALTER TABLE Floors ADD CONSTRAINT UQ_Floors_FloorName_BranchID UNIQUE (FloorName, BranchID);
    PRINT 'Created new unique constraint UQ_Floors_FloorName_BranchID';
END
GO

-- Drop old unique constraint on Rooms.RoomNumber if exists
DECLARE @RoomConstraint NVARCHAR(200);
SELECT @RoomConstraint = name FROM sys.indexes WHERE object_id = OBJECT_ID('Rooms') AND is_unique = 1 AND name LIKE 'UQ%';
IF @RoomConstraint IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Rooms DROP CONSTRAINT ' + @RoomConstraint);
    PRINT 'Dropped old RoomNumber unique constraint: ' + @RoomConstraint;
END
GO

-- Create new unique constraint on Rooms (RoomNumber + BranchID)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Rooms_RoomNumber_BranchID' AND object_id = OBJECT_ID('Rooms'))
BEGIN
    ALTER TABLE Rooms ADD CONSTRAINT UQ_Rooms_RoomNumber_BranchID UNIQUE (RoomNumber, BranchID);
    PRINT 'Created new unique constraint UQ_Rooms_RoomNumber_BranchID';
END
GO

PRINT 'All unique constraints updated to enforce branch-level uniqueness';
