CREATE PROCEDURE sp_UpsertHotelSettings
    @BranchID INT,
    @HotelName NVARCHAR(200),
    @LglNm NVARCHAR(200) = NULL,
    @Address NVARCHAR(500),
    @ContactNumber1 NVARCHAR(20),
    @ContactNumber2 NVARCHAR(20) = NULL,
    @EmailAddress NVARCHAR(200),
    @Website NVARCHAR(300) = NULL,
    @GSTCode NVARCHAR(50) = NULL,
    @LogoPath NVARCHAR(500) = NULL,
    @CheckInTime TIME,
    @CheckOutTime TIME,
    @ByPassActualDayRate BIT = 0,
    @DiscountApprovalRequired BIT = 0,
    @MinimumBookingAmountRequired BIT = 0,
    @MinimumBookingAmount DECIMAL(18,2) = NULL,
    @NoShowGraceHours INT = 6,
    @CancellationRefundApprovalThreshold DECIMAL(18,2) = NULL,
    @EnableCancellationPolicy BIT = 1,
    @PoliceStation NVARCHAR(200) = NULL,
    @EInvoiceMode NVARCHAR(10) = 'MANUAL',
    @EInvoiceJsonStoragePath NVARCHAR(500) = NULL,
    @EInvoiceApiBaseUrl NVARCHAR(500) = NULL,
    @EInvoiceAuthUrl NVARCHAR(500) = NULL,
    @EInvoiceIrnEndpoint NVARCHAR(500) = NULL,
    @EInvoiceClientId NVARCHAR(200) = NULL,
    @EInvoiceClientSecret NVARCHAR(1000) = NULL,
    @EInvoiceUsername NVARCHAR(200) = NULL,
    @EInvoicePassword NVARCHAR(1000) = NULL,
    @ModifiedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM HotelSettings WHERE BranchID = @BranchID)
    BEGIN
        UPDATE HotelSettings SET
            HotelName = @HotelName,
            LglNm = @LglNm,
            Address = @Address,
            ContactNumber1 = @ContactNumber1,
            ContactNumber2 = @ContactNumber2,
            EmailAddress = @EmailAddress,
            Website = @Website,
            GSTCode = @GSTCode,
            LogoPath = COALESCE(@LogoPath, LogoPath),
            CheckInTime = @CheckInTime,
            CheckOutTime = @CheckOutTime,
            ByPassActualDayRate = @ByPassActualDayRate,
            DiscountApprovalRequired = @DiscountApprovalRequired,
            MinimumBookingAmountRequired = @MinimumBookingAmountRequired,
            MinimumBookingAmount = @MinimumBookingAmount,
            NoShowGraceHours = @NoShowGraceHours,
            CancellationRefundApprovalThreshold = @CancellationRefundApprovalThreshold,
            EnableCancellationPolicy = @EnableCancellationPolicy,
            PoliceStation = @PoliceStation,
            EInvoiceMode = @EInvoiceMode,
            EInvoiceJsonStoragePath = @EInvoiceJsonStoragePath,
            EInvoiceApiBaseUrl = @EInvoiceApiBaseUrl,
            EInvoiceAuthUrl = @EInvoiceAuthUrl,
            EInvoiceIrnEndpoint = @EInvoiceIrnEndpoint,
            EInvoiceClientId = @EInvoiceClientId,
            EInvoiceClientSecret = COALESCE(@EInvoiceClientSecret, EInvoiceClientSecret),
            EInvoiceUsername = @EInvoiceUsername,
            EInvoicePassword = COALESCE(@EInvoicePassword, EInvoicePassword),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @ModifiedBy
        WHERE BranchID = @BranchID;
    END
    ELSE
    BEGIN
        INSERT INTO HotelSettings (
            BranchID, HotelName, LglNm, Address, ContactNumber1, ContactNumber2,
            EmailAddress, Website, GSTCode, LogoPath, CheckInTime, CheckOutTime,
            ByPassActualDayRate, DiscountApprovalRequired,
            MinimumBookingAmountRequired, MinimumBookingAmount,
            NoShowGraceHours, CancellationRefundApprovalThreshold,
            EnableCancellationPolicy, PoliceStation,
            EInvoiceMode, EInvoiceJsonStoragePath,
            EInvoiceApiBaseUrl, EInvoiceAuthUrl, EInvoiceIrnEndpoint,
            EInvoiceClientId, EInvoiceClientSecret, EInvoiceUsername, EInvoicePassword,
            IsActive, CreatedDate, CreatedBy, LastModifiedDate, LastModifiedBy
        )
        VALUES (
            @BranchID, @HotelName, @LglNm, @Address, @ContactNumber1, @ContactNumber2,
            @EmailAddress, @Website, @GSTCode, @LogoPath, @CheckInTime, @CheckOutTime,
            @ByPassActualDayRate, @DiscountApprovalRequired,
            @MinimumBookingAmountRequired, @MinimumBookingAmount,
            @NoShowGraceHours, @CancellationRefundApprovalThreshold,
            @EnableCancellationPolicy, @PoliceStation,
            @EInvoiceMode, @EInvoiceJsonStoragePath,
            @EInvoiceApiBaseUrl, @EInvoiceAuthUrl, @EInvoiceIrnEndpoint,
            @EInvoiceClientId, @EInvoiceClientSecret, @EInvoiceUsername, @EInvoicePassword,
            1, GETDATE(), @ModifiedBy, GETDATE(), @ModifiedBy
        );
    END
END
GO
PRINT 'sp_UpsertHotelSettings recreated with EInvoiceJsonStoragePath.';
