-- =============================================
-- Room Price Details Report Stored Procedure
-- Description: Returns room + room type + current applicable rate (if any)
-- =============================================

USE HMS_dev;
GO

IF OBJECT_ID('sp_GetRoomPriceDetailsReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetRoomPriceDetailsReport;
GO

CREATE PROCEDURE sp_GetRoomPriceDetailsReport
    @BranchID INT
    ,@AsOfDate DATE = NULL
    ,@RoomTypeId INT = NULL
    ,@RoomStatus NVARCHAR(50) = NULL
    ,@FloorId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF (@AsOfDate IS NULL)
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    ;WITH CurrentRate AS (
        SELECT
            rm.BranchID,
            rm.RoomTypeId,
            rm.CustomerType,
            rm.Source,
            rm.BaseRate,
            rm.ExtraPaxRate,
            rm.TaxPercentage,
            rm.CGSTPercentage,
            rm.SGSTPercentage,
            rm.StartDate,
            rm.EndDate,
            rm.IsWeekdayRate,
            rm.ApplyDiscount,
            rm.IsDynamicRate,
            rm.CreatedDate,
            ROW_NUMBER() OVER (
                PARTITION BY rm.BranchID, rm.RoomTypeId
                ORDER BY rm.CreatedDate DESC
            ) AS rn
        FROM RateMaster rm
        WHERE rm.IsActive = 1
          AND rm.BranchID = @BranchID
                    AND @AsOfDate BETWEEN CAST(rm.StartDate AS DATE) AND CAST(rm.EndDate AS DATE)
    )

    SELECT
        r.Id AS RoomId,
        r.RoomNumber,
        r.Status AS RoomStatus,
        r.Floor,
        f.FloorName,
        r.Notes,

        rt.Id AS RoomTypeId,
        rt.TypeName AS RoomType,
        rt.Description AS RoomTypeDescription,
        rt.MaxOccupancy,
        rt.Amenities,
        rt.BaseRate AS DefaultBaseRate,
        rt.Max_RoomAvailability AS RoomTypeCapacity,

        cr.CustomerType AS CurrentCustomerType,
        cr.Source AS CurrentSource,
        cr.BaseRate AS CurrentBaseRate,
        cr.ExtraPaxRate AS CurrentExtraPaxRate,
        cr.TaxPercentage AS CurrentTaxPercentage,
        cr.CGSTPercentage AS CurrentCGSTPercentage,
        cr.SGSTPercentage AS CurrentSGSTPercentage,
        cr.StartDate AS CurrentRateStartDate,
        cr.EndDate AS CurrentRateEndDate,
        cr.IsWeekdayRate,
        cr.ApplyDiscount,
        cr.IsDynamicRate
    FROM Rooms r
    INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.Id
    LEFT JOIN Floors f ON r.Floor = f.Id
    LEFT JOIN CurrentRate cr ON cr.BranchID = r.BranchID AND cr.RoomTypeId = r.RoomTypeId AND cr.rn = 1
    WHERE r.IsActive = 1
      AND r.BranchID = @BranchID
            AND (@RoomTypeId IS NULL OR r.RoomTypeId = @RoomTypeId)
            AND (@RoomStatus IS NULL OR LTRIM(RTRIM(@RoomStatus)) = '' OR r.Status = @RoomStatus)
            AND (@FloorId IS NULL OR r.Floor = @FloorId)
    ORDER BY r.RoomNumber;
END;
GO

PRINT 'Stored Procedure sp_GetRoomPriceDetailsReport created successfully';
GO
