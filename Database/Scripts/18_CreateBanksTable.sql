-- =============================================
-- Create Banks Table for Payment Methods
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Banks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Banks] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BankName] NVARCHAR(200) NOT NULL,
        [BankCode] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [LastModifiedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_Banks_IsActive ON [dbo].[Banks]([IsActive]);
    
    PRINT 'Banks table created successfully';
END
ELSE
BEGIN
    PRINT 'Banks table already exists';
END
GO

-- =============================================
-- Seed Bank Data
-- =============================================

IF NOT EXISTS (SELECT 1 FROM Banks)
BEGIN
    INSERT INTO Banks (BankName, BankCode, IsActive) VALUES
    ('State Bank of India', 'SBI', 1),
    ('HDFC Bank', 'HDFC', 1),
    ('ICICI Bank', 'ICICI', 1),
    ('Axis Bank', 'AXIS', 1),
    ('Punjab National Bank', 'PNB', 1),
    ('Bank of Baroda', 'BOB', 1),
    ('Canara Bank', 'CANARA', 1),
    ('Union Bank of India', 'UNION', 1),
    ('Bank of India', 'BOI', 1),
    ('Kotak Mahindra Bank', 'KOTAK', 1),
    ('IndusInd Bank', 'INDUSIND', 1),
    ('Yes Bank', 'YES', 1),
    ('IDBI Bank', 'IDBI', 1),
    ('Central Bank of India', 'CBI', 1),
    ('Indian Bank', 'INDIAN', 1);
    
    PRINT 'Bank data seeded successfully';
END
GO

-- =============================================
-- Alter BookingPayments Table for Enhanced Payment Details
-- =============================================

-- Add new columns if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') AND name = 'CardType')
BEGIN
    ALTER TABLE [dbo].[BookingPayments] ADD [CardType] NVARCHAR(50) NULL;
    PRINT 'Added CardType column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') AND name = 'CardLastFourDigits')
BEGIN
    ALTER TABLE [dbo].[BookingPayments] ADD [CardLastFourDigits] NVARCHAR(4) NULL;
    PRINT 'Added CardLastFourDigits column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') AND name = 'BankId')
BEGIN
    ALTER TABLE [dbo].[BookingPayments] ADD [BankId] INT NULL;
    PRINT 'Added BankId column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BookingPayments]') AND name = 'ChequeDate')
BEGIN
    ALTER TABLE [dbo].[BookingPayments] ADD [ChequeDate] DATE NULL;
    PRINT 'Added ChequeDate column';
END

-- Add foreign key constraint if not exists
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_BookingPayments_Banks]'))
BEGIN
    ALTER TABLE [dbo].[BookingPayments]
    ADD CONSTRAINT FK_BookingPayments_Banks FOREIGN KEY ([BankId]) 
    REFERENCES [dbo].[Banks]([Id]);
    
    PRINT 'Added foreign key constraint';
END
GO

PRINT 'Payment enhancement script completed successfully';
GO
