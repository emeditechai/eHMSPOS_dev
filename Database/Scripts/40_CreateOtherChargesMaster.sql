SET NOCOUNT ON;

IF OBJECT_ID('dbo.OtherCharges', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.OtherCharges
	(
		Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OtherCharges PRIMARY KEY,
		Code NVARCHAR(30) NOT NULL,
		[Name] NVARCHAR(150) NOT NULL,
		[Type] INT NOT NULL,
		Rate DECIMAL(18,2) NOT NULL CONSTRAINT DF_OtherCharges_Rate DEFAULT(0),
		GSTPercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_OtherCharges_GSTPercent DEFAULT(0),
		CGSTPercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_OtherCharges_CGSTPercent DEFAULT(0),
		SGSTPercent DECIMAL(6,2) NOT NULL CONSTRAINT DF_OtherCharges_SGSTPercent DEFAULT(0),
		BranchID INT NOT NULL,
		IsActive BIT NOT NULL CONSTRAINT DF_OtherCharges_IsActive DEFAULT(1),
		CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_OtherCharges_CreatedDate DEFAULT(SYSUTCDATETIME()),
		CreatedBy INT NULL,
		UpdatedDate DATETIME2 NULL,
		UpdatedBy INT NULL
	);

	ALTER TABLE dbo.OtherCharges
		ADD CONSTRAINT UQ_OtherCharges_Code_BranchID UNIQUE (Code, BranchID);

	CREATE INDEX IX_OtherCharges_BranchID_IsActive
		ON dbo.OtherCharges (BranchID, IsActive)
		INCLUDE (Code, [Name], [Type], Rate, GSTPercent, CGSTPercent, SGSTPercent);
END
ELSE
BEGIN
	PRINT 'dbo.OtherCharges already exists; skipping create.';

	IF NOT EXISTS (
		SELECT 1
		FROM sys.indexes
		WHERE name = 'UQ_OtherCharges_Code_BranchID'
		  AND object_id = OBJECT_ID('dbo.OtherCharges')
	)
	BEGIN
		BEGIN TRY
			ALTER TABLE dbo.OtherCharges
				ADD CONSTRAINT UQ_OtherCharges_Code_BranchID UNIQUE (Code, BranchID);
		END TRY
		BEGIN CATCH
			PRINT 'Could not add unique constraint UQ_OtherCharges_Code_BranchID (may already exist or duplicates present).';
		END CATCH
	END

	IF NOT EXISTS (
		SELECT 1
		FROM sys.indexes
		WHERE name = 'IX_OtherCharges_BranchID_IsActive'
		  AND object_id = OBJECT_ID('dbo.OtherCharges')
	)
	BEGIN
		CREATE INDEX IX_OtherCharges_BranchID_IsActive
			ON dbo.OtherCharges (BranchID, IsActive)
			INCLUDE (Code, [Name], [Type], Rate, GSTPercent, CGSTPercent, SGSTPercent);
	END
END
