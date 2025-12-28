-- Create RoomServices table (cached snapshot of pending room service settlement lines)

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    WHERE t.name = 'RoomServices' AND t.schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[RoomServices]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BookingID] INT NOT NULL,
        [RoomID] INT NOT NULL,
        [BranchID] INT NOT NULL,

        [OrderID] INT NOT NULL,
        [OrderDate] DATETIME NOT NULL,
        [OrderNo] NVARCHAR(50) NULL,

        [MenuItem] NVARCHAR(200) NULL,
        [Price] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_Price] DEFAULT (0),
        [Qty] INT NOT NULL CONSTRAINT [DF_RoomServices_Qty] DEFAULT (1),
        [NetAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_NetAmount] DEFAULT (0),
        [DiscountAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_DiscountAmount] DEFAULT (0),
        [ActualBillAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_ActualBillAmount] DEFAULT (0),
        [IsSettled] BIT NOT NULL CONSTRAINT [DF_RoomServices_IsSettled] DEFAULT (0),
        [SettleAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_SettleAmount] DEFAULT (0),

        -- Note: GST/CGST/SGST are order-level amounts and may be repeated across rows.
        [CGSTAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_CGSTAmount] DEFAULT (0),
        [SGSTAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_SGSTAmount] DEFAULT (0),
        [GSTAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_RoomServices_GSTAmount] DEFAULT (0),

        [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_RoomServices_CreatedAt] DEFAULT (GETDATE())
    );

    CREATE INDEX [IX_RoomServices_Booking_Room_Branch]
        ON [dbo].[RoomServices] ([BookingID], [RoomID], [BranchID]);

    PRINT 'RoomServices table created.';
END
ELSE
BEGIN
    PRINT 'RoomServices table already exists.';
END
