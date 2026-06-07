CREATE OR ALTER PROCEDURE [dbo].[GetRoomAvailabilityCalendar]
    @StartDate DATE,
    @EndDate DATE,
    @RoomTypeId INT = NULL,
    @Source NVARCHAR(50) = 'WalkIn',
    @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Generate Date Series
    WITH DateSeries AS (
        SELECT @StartDate AS StayDate
        UNION ALL
        SELECT DATEADD(day, 1, StayDate)
        FROM DateSeries
        WHERE StayDate < @EndDate
    ),
    -- 2. Base Room Types and Total Capacity
    FilteredRoomTypes AS (
        SELECT 
            rt.Id AS RoomTypeId,
            rt.TypeName AS RoomTypeName,
            ISNULL(rt.MaxOccupancy, 0) AS MaxOccupancy,
            ISNULL(rt.Max_RoomAvailability, 0) AS TotalCapacity
        FROM RoomTypes rt
        WHERE rt.BranchID = @BranchId
          AND rt.IsActive = 1
          AND (@RoomTypeId IS NULL OR rt.Id = @RoomTypeId)
    ),
    -- 3. Required Rooms from standard Bookings (no B2B lines)
    BookingsWithoutLines AS (
        SELECT 
            b.RoomTypeId,
            d.StayDate,
            SUM(ISNULL(b.RequiredRooms, 0)) AS RequiredRooms
        FROM Bookings b
        INNER JOIN DateSeries d 
            ON CAST(b.CheckInDate AS DATE) <= d.StayDate
           AND CAST(ISNULL(b.ActualCheckOutDate, b.CheckOutDate) AS DATE) > d.StayDate
        WHERE b.BranchID = @BranchId
          AND b.Status IN ('Confirmed', 'CheckedIn', 'PartialCancelled')
          AND NOT EXISTS (SELECT 1 FROM B2BBookingRoomLines brl WHERE brl.BookingId = b.Id AND ISNULL(brl.IsCancelled, 0) = 0)
        GROUP BY b.RoomTypeId, d.StayDate
    ),
    -- 4. Required Rooms from B2B Lines
    BookingsWithLines AS (
        SELECT 
            brl.RoomTypeId,
            d.StayDate,
            SUM(ISNULL(brl.RequiredRooms, 0)) AS RequiredRooms
        FROM B2BBookingRoomLines brl
        INNER JOIN Bookings b ON brl.BookingId = b.Id
        INNER JOIN DateSeries d 
            ON CAST(ISNULL(brl.CheckInDate, b.CheckInDate) AS DATE) <= d.StayDate
           AND CAST(ISNULL(brl.CheckOutDate, b.CheckOutDate) AS DATE) > d.StayDate
        WHERE b.BranchID = @BranchId
          AND b.Status IN ('Confirmed', 'CheckedIn', 'PartialCancelled')
          AND b.ActualCheckOutDate IS NULL
          AND ISNULL(brl.IsCancelled, 0) = 0
        GROUP BY brl.RoomTypeId, d.StayDate
    ),
    -- 5. Calculate Total Occupied Per Day
    OccupiedPerDay AS (
        SELECT 
            RoomTypeId, 
            StayDate, 
            SUM(RequiredRooms) AS TotalOccupied
        FROM (
            SELECT RoomTypeId, StayDate, RequiredRooms FROM BookingsWithoutLines
            UNION ALL
            SELECT RoomTypeId, StayDate, RequiredRooms FROM BookingsWithLines
        ) t
        GROUP BY RoomTypeId, StayDate
    ),
    -- 6. Rate calculation CTE
    RatesAll AS (
        SELECT 
            d.StayDate,
            rt.RoomTypeId,
            rm.CustomerType,
            ISNULL(sdr.BaseRate, ISNULL(wr.BaseRate, rm.BaseRate)) AS EffectiveBaseRate,
            ISNULL(sdr.ExtraPaxRate, ISNULL(wr.ExtraPaxRate, rm.ExtraPaxRate)) AS EffectiveExtraPaxRate,
            CASE 
                WHEN sdr.Id IS NOT NULL THEN 'Special Day'
                WHEN wr.Id IS NOT NULL THEN 'Weekend'
                ELSE 'Standard'
            END AS RateType,
            sdr.EventName,
            CASE 
                WHEN rm.TaxPercentage > 0 THEN rm.TaxPercentage
                WHEN (rm.CGSTPercentage + rm.SGSTPercentage) > 0 THEN (rm.CGSTPercentage + rm.SGSTPercentage)
                ELSE 0 
            END AS TaxPercentage,
            rm.ApplyDiscount AS ApplyDiscountStr,
            ROW_NUMBER() OVER (
                PARTITION BY d.StayDate, rt.RoomTypeId, rm.CustomerType
                ORDER BY 
                    CASE WHEN sdr.Id IS NOT NULL THEN 1
                         WHEN wr.Id IS NOT NULL THEN 2
                         WHEN d.StayDate BETWEEN CAST(rm.StartDate AS DATE) AND CAST(rm.EndDate AS DATE) THEN 3
                         ELSE 4 END ASC,
                    rm.CreatedDate DESC
            ) AS rn
        FROM DateSeries d
        CROSS JOIN FilteredRoomTypes rt
        INNER JOIN RateMaster rm ON rm.RoomTypeId = rt.RoomTypeId 
            AND rm.IsActive = 1 
            AND rm.BranchID = @BranchId
            AND (ISNULL(rm.Source, '') = '' OR rm.Source = @Source)
        LEFT JOIN SpecialDayRates sdr ON sdr.RateMasterId = rm.Id AND sdr.IsActive = 1 
            AND d.StayDate BETWEEN CAST(sdr.FromDate AS DATE) AND CAST(sdr.ToDate AS DATE)
        LEFT JOIN WeekendRates wr ON wr.RateMasterId = rm.Id AND wr.IsActive = 1
            AND wr.DayOfWeek = DATENAME(weekday, d.StayDate)
    )
    
    -- 7. Final Projection
    SELECT 
        d.StayDate,
        rt.RoomTypeId,
        rt.RoomTypeName,
        rt.TotalCapacity AS TotalRooms,
        ISNULL(occ.TotalOccupied, 0) AS OccupiedRooms,
        CASE WHEN rt.TotalCapacity - ISNULL(occ.TotalOccupied, 0) < 0 THEN 0 ELSE rt.TotalCapacity - ISNULL(occ.TotalOccupied, 0) END AS AvailableRooms,
        rt.MaxOccupancy,
        
        -- B2C Rates
        ISNULL(b2c.EffectiveBaseRate, 0) AS B2C_OriginalRate,
        CAST(ISNULL(b2c.EffectiveBaseRate, 0) * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2)) AS B2C_BaseRate,
        ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0) AS B2C_DiscountPercent,
        ISNULL(b2c.EffectiveBaseRate, 0) - CAST(ISNULL(b2c.EffectiveBaseRate, 0) * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2)) AS B2C_DiscountAmount,
        ISNULL(b2c.TaxPercentage, 0) AS B2C_TaxPercentage,
        CAST(CAST(ISNULL(b2c.EffectiveBaseRate, 0) * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2)) * (1.0 + ISNULL(b2c.TaxPercentage, 0)/100.0) AS DECIMAL(18,2)) AS B2C_ActualRoomRate,
        ISNULL(b2c.EffectiveExtraPaxRate, 0) AS B2C_ExtraPaxRate,
        b2c.RateType AS B2C_RateType,
        b2c.EventName AS B2C_EventName,
        
        -- B2B Rates
        b2b.EffectiveBaseRate AS B2B_OriginalRate,
        CASE WHEN b2b.EffectiveBaseRate IS NOT NULL THEN 
             CAST(b2b.EffectiveBaseRate * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2))
        ELSE NULL END AS B2B_BaseRate,
        CASE WHEN b2b.EffectiveBaseRate IS NOT NULL THEN ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0) ELSE NULL END AS B2B_DiscountPercent,
        CASE WHEN b2b.EffectiveBaseRate IS NOT NULL THEN 
             b2b.EffectiveBaseRate - CAST(b2b.EffectiveBaseRate * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2))
        ELSE NULL END AS B2B_DiscountAmount,
        b2b.TaxPercentage AS B2B_TaxPercentage,
        CASE WHEN b2b.EffectiveBaseRate IS NOT NULL THEN 
             CAST(CAST(b2b.EffectiveBaseRate * (1.0 - ISNULL(TRY_CAST(b2c.ApplyDiscountStr AS DECIMAL(18,2)), 0)/100.0) AS DECIMAL(18,2)) * (1.0 + ISNULL(b2b.TaxPercentage, 0)/100.0) AS DECIMAL(18,2))
        ELSE NULL END AS B2B_ActualRoomRate,
        b2b.EffectiveExtraPaxRate AS B2B_ExtraPaxRate,
        b2b.RateType AS B2B_RateType,
        b2b.EventName AS B2B_EventName

    FROM DateSeries d
    CROSS JOIN FilteredRoomTypes rt
    LEFT JOIN OccupiedPerDay occ ON occ.RoomTypeId = rt.RoomTypeId AND occ.StayDate = d.StayDate
    LEFT JOIN RatesAll b2c ON b2c.RoomTypeId = rt.RoomTypeId AND b2c.StayDate = d.StayDate AND b2c.CustomerType = 'B2C' AND b2c.rn = 1
    LEFT JOIN RatesAll b2b ON b2b.RoomTypeId = rt.RoomTypeId AND b2b.StayDate = d.StayDate AND b2b.CustomerType = 'B2B' AND b2b.rn = 1
    ORDER BY d.StayDate, rt.RoomTypeName
    
    OPTION (MAXRECURSION 365);
END
GO
