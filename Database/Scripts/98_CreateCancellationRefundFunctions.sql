-- =============================================
-- SQL refund calculation helper (policy-driven)
-- Created: 2026-02-08
-- Note: Business logic is also implemented in C#; this is for reporting/audit.
-- =============================================

IF OBJECT_ID(N'dbo.fn_CalculateRefundAmount', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_CalculateRefundAmount;
GO

CREATE FUNCTION dbo.fn_CalculateRefundAmount
(
    @PolicyId INT,
    @AmountPaid DECIMAL(18,2),
    @HoursBeforeCheckIn INT,
    @RateType NVARCHAR(20),
    @IsNoShow BIT
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @refundPercent DECIMAL(5,2) = 0;
    DECLARE @flatDeduction DECIMAL(18,2) = 0;
    DECLARE @gatewayPct DECIMAL(5,2) = 0;
    DECLARE @deduction DECIMAL(18,2) = 0;
    DECLARE @refundRaw DECIMAL(18,2) = 0;
    DECLARE @refundFinal DECIMAL(18,2) = 0;

    IF (@IsNoShow = 1)
        RETURN 0;

    IF (UPPER(LTRIM(RTRIM(ISNULL(@RateType, '')))) IN ('NONREFUNDABLE', 'NON-REFUNDABLE', 'NON_REFUNDABLE'))
        RETURN 0;

    SELECT TOP 1
        @refundPercent = r.RefundPercent,
        @flatDeduction = r.FlatDeduction,
        @gatewayPct = COALESCE(r.GatewayFeeDeductionPercent, p.GatewayFeeDeductionPercent, 0)
    FROM dbo.CancellationPolicyRules r
    INNER JOIN dbo.CancellationPolicies p ON p.Id = r.PolicyId
    WHERE r.PolicyId = @PolicyId
      AND r.IsActive = 1
      AND p.IsActive = 1
      AND @HoursBeforeCheckIn BETWEEN r.MinHoursBeforeCheckIn AND r.MaxHoursBeforeCheckIn
    ORDER BY r.SortOrder ASC, r.MinHoursBeforeCheckIn DESC;

    SET @refundRaw = (@AmountPaid * ISNULL(@refundPercent, 0) / 100.0);
    SET @deduction = ISNULL(@flatDeduction, 0) + (@AmountPaid * ISNULL(@gatewayPct, 0) / 100.0);

    SET @refundFinal = @refundRaw - @deduction;

    IF (@refundFinal < 0) SET @refundFinal = 0;
    IF (@refundFinal > @AmountPaid) SET @refundFinal = @AmountPaid;

    RETURN @refundFinal;
END
GO

PRINT 'Function fn_CalculateRefundAmount created successfully';
GO
