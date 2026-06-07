-- ============================================================
-- View  : dbo.vw_RoomTypeRateDetails
-- Purpose: Consolidated Room Type + Rate Master report for
--          Source = 'Websites' bookings.
--
-- Columns
--   Branch              : from BranchMaster
--   Room Type fields    : TypeName, MaxOccupancy, BedType,
--                         AreaSqFt, RoomView, Description
--   Amenities           : stored as JSON array string
--   Rate fields         : CustomerType, Source, MealType,
--                         BaseRate, ExtraPaxRate, Tax%,
--                         CGST%, SGST%, ApplyDiscount
--
-- Actual Room Rate Calculation (for Websites / B2C):
--   Step 1  Price with GST  = BaseRate × (1 + TaxPercentage / 100)
--   Step 2  After Discount  = PriceWithGST × (1 − ApplyDiscount / 100)
--   ApplyDiscount is NVARCHAR → TRY_CAST used; NULL/non-numeric = 0
-- ============================================================

IF OBJECT_ID('dbo.vw_RoomTypeRateDetails', 'V') IS NOT NULL
    DROP VIEW dbo.vw_RoomTypeRateDetails;
GO

CREATE VIEW dbo.vw_RoomTypeRateDetails
AS
SELECT
    /* ── Branch ─────────────────────────────────────────── */
    bm.BranchID                                             AS [Branch ID],
    bm.BranchName                                           AS [Branch],

    /* ── Room Type ───────────────────────────────────────── */
    rt.Id                                                   AS [Room Type ID],
    rt.TypeName                                             AS [Room Type],
    rt.MaxOccupancy                                         AS [Max Occupancy],
    rt.BedType                                              AS [Bed Type],
    rt.AreaSqFt                                             AS [Area (sq ft)],
    rt.RoomView                                             AS [Room View],
    rt.Description                                          AS [Description],

    /* ── Amenities as JSON array ─────────────────────────── */
    CASE
        WHEN NULLIF(LTRIM(RTRIM(rt.Amenities)), '') IS NULL
            THEN N'[]'
        ELSE
            -- Normalise: replace ', ' and ',' separators uniformly, then wrap
            N'["' +
            REPLACE(
                LTRIM(RTRIM(
                    REPLACE(
                        REPLACE(LTRIM(RTRIM(rt.Amenities)), N', ', N'|~|'),
                        N',', N'|~|'
                    )
                )),
                N'|~|', N'","'
            ) +
            N'"]'
    END                                                     AS [Amenities],

    /* ── Rate Master ─────────────────────────────────────── */
    rm.CustomerType                                         AS [Customer Type],
    rm.Source                                               AS [Source],
    ISNULL(rm.MealType, 'EP')                               AS [Meal Type],
    rm.BaseRate                                             AS [Base Rate],
    rm.ExtraPaxRate                                         AS [Extra Pax Rate],
    rm.TaxPercentage                                        AS [Tax %],
    rm.CGSTPercentage                                       AS [CGST %],
    rm.SGSTPercentage                                       AS [SGST %],
    rm.ApplyDiscount                                        AS [Apply Discount %],

    /* ── Step 1 : Base Rate + GST ────────────────────────── */
    CAST(
        rm.BaseRate * (1 + rm.TaxPercentage / 100.0)
    AS DECIMAL(18,2))                                       AS [Rate with GST],

    /* ── Step 2 : After Discount ─────────────────────────── */
    CAST(
        rm.BaseRate
        * (1 + rm.TaxPercentage / 100.0)
        * (1 - ISNULL(TRY_CAST(rm.ApplyDiscount AS DECIMAL(18,2)), 0) / 100.0)
    AS DECIMAL(18,2))                                       AS [Actual Room Rate]

FROM
    dbo.RateMaster   AS rm
    INNER JOIN dbo.RoomTypes    AS rt ON rt.Id       = rm.RoomTypeId
                                     AND rt.BranchID = rm.BranchID
    INNER JOIN dbo.BranchMaster AS bm ON bm.BranchID = rm.BranchID

WHERE
    rm.Source   = 'Websites'
    AND rm.IsActive = 1
    AND rt.IsActive = 1;
GO

