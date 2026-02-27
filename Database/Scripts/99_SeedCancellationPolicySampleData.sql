-- =============================================
-- Seed sample cancellation policy for demo/UAT
-- Created: 2026-02-08
-- =============================================

DECLARE @BranchID INT = 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.CancellationPolicies
    WHERE BranchID = @BranchID
      AND PolicyName = 'Standard Web Booking Policy'
      AND BookingSource = 'Website'
      AND CustomerType = 'B2C'
      AND RateType = 'Standard'
)
BEGIN
    INSERT INTO dbo.CancellationPolicies (
        BranchID, PolicyName, BookingSource, CustomerType, RateType,
        ValidFrom, ValidTo,
        NoShowRefundAllowed, ApprovalRequired, GatewayFeeDeductionPercent,
        IsActive, CreatedDate, LastModifiedDate
    )
    VALUES (
        @BranchID,
        'Standard Web Booking Policy',
        'Website',
        'B2C',
        'Standard',
        NULL,
        NULL,
        0,
        0,
        2.00,
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );

    DECLARE @PolicyId INT = SCOPE_IDENTITY();

    -- >72 hours => refund 100% minus gateway fee (2%)
    INSERT INTO dbo.CancellationPolicyRules (PolicyId, MinHoursBeforeCheckIn, MaxHoursBeforeCheckIn, RefundPercent, FlatDeduction, GatewayFeeDeductionPercent, IsActive, SortOrder)
    VALUES (@PolicyId, 72, 999999, 100.00, 0.00, NULL, 1, 1);

    -- 24-72 hours => 50% refund
    INSERT INTO dbo.CancellationPolicyRules (PolicyId, MinHoursBeforeCheckIn, MaxHoursBeforeCheckIn, RefundPercent, FlatDeduction, GatewayFeeDeductionPercent, IsActive, SortOrder)
    VALUES (@PolicyId, 24, 71, 50.00, 0.00, NULL, 1, 2);

    -- <24 hours => 0% refund
    INSERT INTO dbo.CancellationPolicyRules (PolicyId, MinHoursBeforeCheckIn, MaxHoursBeforeCheckIn, RefundPercent, FlatDeduction, GatewayFeeDeductionPercent, IsActive, SortOrder)
    VALUES (@PolicyId, 0, 23, 0.00, 0.00, NULL, 1, 3);

    PRINT 'Seeded: Standard Web Booking Policy (BranchID=1)';
END
ELSE
BEGIN
    PRINT 'Seed policy already exists; skipping';
END
GO
