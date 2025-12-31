-- Creates per-branch Email/SMTP configuration for sending system emails

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MailConfigurations')
BEGIN
    CREATE TABLE dbo.MailConfigurations
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BranchID INT NOT NULL,
        SmtpHost NVARCHAR(200) NULL,
        SmtpPort INT NULL,
        UserName NVARCHAR(200) NULL,
        PasswordProtected NVARCHAR(MAX) NULL,
        EnableSslTls BIT NOT NULL CONSTRAINT DF_MailConfigurations_EnableSslTls DEFAULT (1),
        FromEmail NVARCHAR(200) NULL,
        FromName NVARCHAR(200) NULL,
        AdminNotificationEmail NVARCHAR(200) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MailConfigurations_IsActive DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_MailConfigurations_CreatedDate DEFAULT (GETDATE()),
        CreatedBy INT NULL,
        LastModifiedDate DATETIME NOT NULL CONSTRAINT DF_MailConfigurations_LastModifiedDate DEFAULT (GETDATE()),
        LastModifiedBy INT NULL
    );

    -- One row per branch
    CREATE UNIQUE INDEX UX_MailConfigurations_BranchID ON dbo.MailConfigurations(BranchID);
END
