SET NOCOUNT ON;

IF OBJECT_ID('dbo.Amenities', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Amenities
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Amenities PRIMARY KEY,
		AmenityName NVARCHAR(100) NOT NULL,
		BranchID INT NOT NULL,
		IsActive BIT NOT NULL CONSTRAINT DF_Amenities_IsActive DEFAULT(1),
		CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Amenities_CreatedDate DEFAULT(SYSUTCDATETIME()),
		CreatedBy INT NULL,
		UpdatedDate DATETIME2 NULL,
		UpdatedBy INT NULL
	);

	ALTER TABLE dbo.Amenities
		ADD CONSTRAINT UQ_Amenities_AmenityName_BranchID UNIQUE (AmenityName, BranchID);

	CREATE INDEX IX_Amenities_BranchID_IsActive
		ON dbo.Amenities (BranchID, IsActive) INCLUDE (AmenityName);
END
ELSE
BEGIN
	PRINT 'dbo.Amenities already exists; skipping create.';

	IF NOT EXISTS (
		SELECT 1
		FROM sys.indexes
		WHERE name = 'UQ_Amenities_AmenityName_BranchID'
		  AND object_id = OBJECT_ID('dbo.Amenities')
	)
	BEGIN
		BEGIN TRY
			ALTER TABLE dbo.Amenities
				ADD CONSTRAINT UQ_Amenities_AmenityName_BranchID UNIQUE (AmenityName, BranchID);
		END TRY
		BEGIN CATCH
			PRINT 'Could not add unique constraint UQ_Amenities_AmenityName_BranchID (may already exist or duplicates present).';
		END CATCH
	END

	IF NOT EXISTS (
		SELECT 1
		FROM sys.indexes
		WHERE name = 'IX_Amenities_BranchID_IsActive'
		  AND object_id = OBJECT_ID('dbo.Amenities')
	)
	BEGIN
		CREATE INDEX IX_Amenities_BranchID_IsActive
			ON dbo.Amenities (BranchID, IsActive) INCLUDE (AmenityName);
	END
END

