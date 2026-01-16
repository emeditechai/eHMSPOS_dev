SET NOCOUNT ON;

/* Seed common Asset Management masters per branch.
   Idempotent and safe to re-run.
*/

DECLARE @Branches TABLE (BranchID INT);
INSERT INTO @Branches(BranchID)
SELECT DISTINCT BranchID FROM dbo.BranchMaster WHERE IsActive = 1;

-- Fallback when BranchMaster differs / not present
IF NOT EXISTS (SELECT 1 FROM @Branches)
BEGIN
    INSERT INTO @Branches(BranchID) VALUES (1);
END

DECLARE @BranchID INT;
DECLARE branch_cursor CURSOR FOR SELECT BranchID FROM @Branches;
OPEN branch_cursor;
FETCH NEXT FROM branch_cursor INTO @BranchID;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Departments
    IF OBJECT_ID('dbo.AssetDepartments', 'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetDepartments WHERE BranchID = @BranchID AND [Name] = 'Housekeeping')
            INSERT INTO dbo.AssetDepartments(BranchID, [Name], IsActive) VALUES (@BranchID, 'Housekeeping', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetDepartments WHERE BranchID = @BranchID AND [Name] = 'Kitchen')
            INSERT INTO dbo.AssetDepartments(BranchID, [Name], IsActive) VALUES (@BranchID, 'Kitchen', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetDepartments WHERE BranchID = @BranchID AND [Name] = 'Room Service')
            INSERT INTO dbo.AssetDepartments(BranchID, [Name], IsActive) VALUES (@BranchID, 'Room Service', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetDepartments WHERE BranchID = @BranchID AND [Name] = 'Front Office')
            INSERT INTO dbo.AssetDepartments(BranchID, [Name], IsActive) VALUES (@BranchID, 'Front Office', 1);
    END

    -- Units
    IF OBJECT_ID('dbo.AssetUnits', 'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetUnits WHERE BranchID = @BranchID AND [Name] = 'Nos')
            INSERT INTO dbo.AssetUnits(BranchID, [Name], IsActive) VALUES (@BranchID, 'Nos', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetUnits WHERE BranchID = @BranchID AND [Name] = 'Pkt')
            INSERT INTO dbo.AssetUnits(BranchID, [Name], IsActive) VALUES (@BranchID, 'Pkt', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetUnits WHERE BranchID = @BranchID AND [Name] = 'Bottle')
            INSERT INTO dbo.AssetUnits(BranchID, [Name], IsActive) VALUES (@BranchID, 'Bottle', 1);
        IF NOT EXISTS (SELECT 1 FROM dbo.AssetUnits WHERE BranchID = @BranchID AND [Name] = 'Set')
            INSERT INTO dbo.AssetUnits(BranchID, [Name], IsActive) VALUES (@BranchID, 'Set', 1);
    END

    FETCH NEXT FROM branch_cursor INTO @BranchID;
END

CLOSE branch_cursor;
DEALLOCATE branch_cursor;
