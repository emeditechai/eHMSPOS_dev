-- ============================================================
--  DATA CLEANUP SCRIPT
--  Database : HMS_Dev
--  Purpose  : Remove ALL transactional / operational data.
--             Preserve Admin user and all master / config data.
--
--  WHAT IS DELETED:
--    • All Banquet bookings, payments, audit logs, cancellations
--    • All Hotel bookings, payments, guests, rooms, audit logs
--    • B2B agreements, rates, e-invoice logs
--    • Guest records, guest feedback
--    • Asset movements, allocations, damage/loss records, stock balances
--    • Room maintenance history
--    • License validation history, IRP tokens
--    • All Users EXCEPT Username = 'Admin'
--    • User-role and user-branch mappings for non-Admin users
--
--  WHAT IS KEPT (master / configuration):
--    • Users (Admin only)
--    • Roles, NavMenuItems, RoleNavMenuItems, RoleDashboardConfig
--    • AuthorizationResources, AuthorizationRolePermissions
--    • HotelSettings, BranchMaster, Banks, MailConfigurations, UpiSettings
--    • Rooms, Floors, RoomTypes, RoomServices, Amenities
--    • RateMaster, RateTypes, WeekendRates, SpecialDayRates
--    • GstSlabs, GstSlabBands
--    • CancellationPolicies, CancellationPolicyRules
--    • OtherCharges, BillingsHeads, BookingReceiptTemplateSettings
--    • BanquetVenues, BanquetPackages, BanquetAddonServices, BanquetEventTypes
--    • AssetItems, AssetDepartments, AssetItemDepartments,
--      AssetMakers, AssetUnits, AssetConsumableStandards
--    • B2BClients (client directory — kept as master)
--    • Countries, States, Cities, Nationalities
--    • ClientAppLicense
--
--  SEQUENCES RESET:
--    • InvoiceSequence     → CurrentNumber = 0
--    • CreditNoteSequence  → CurrentNumber = 0
--    • BanquetBookingCounter → CurrentNumber = 0
--    • BanquetReceiptCounter → CurrentNumber = 0
--    • EInvoiceVersionSequence → CurrentNumber = 0
--
--  ⚠  WARNING: This operation is IRREVERSIBLE.
--              Take a full database backup before running.
--
--  Generated : 2026-05-17
-- ============================================================

SET NOCOUNT ON;
PRINT '============================================================';
PRINT ' HMS Data Cleanup — ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

    -- --------------------------------------------------------
    -- STEP 1 : Banquet child records (must precede BanquetBookings)
    -- --------------------------------------------------------
    PRINT '--- STEP 1: Banquet child records ---';

    DELETE FROM BanquetBookingAuditLog;
    PRINT 'Deleted BanquetBookingAuditLog        : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BanquetBookingPayments;
    PRINT 'Deleted BanquetBookingPayments         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BanquetBookingAddonLines;
    PRINT 'Deleted BanquetBookingAddonLines       : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BanquetBookingPackageLines;
    PRINT 'Deleted BanquetBookingPackageLines     : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BanquetCancellations;
    PRINT 'Deleted BanquetCancellations           : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 2 : BanquetBookings
    --          (FK → Bookings, Guests, B2BAgreements — must come
    --           before those parent tables are touched)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 2: BanquetBookings ---';

    DELETE FROM BanquetBookings;
    PRINT 'Deleted BanquetBookings                : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 3 : Hotel booking child records (before Bookings)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 3: Hotel booking child records ---';

    DELETE FROM BookingAuditLog;
    PRINT 'Deleted BookingAuditLog                : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM B2BEInvoiceJsonLogs;
    PRINT 'Deleted B2BEInvoiceJsonLogs            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM B2BBookingRoomLines;
    PRINT 'Deleted B2BBookingRoomLines            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BookingOtherCharges;
    PRINT 'Deleted BookingOtherCharges            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BookingPayments;
    PRINT 'Deleted BookingPayments                : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BookingCancellations;
    PRINT 'Deleted BookingCancellations           : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- BookingGuests has FK → Bookings AND → Guests; delete before both
    DELETE FROM BookingGuests;
    PRINT 'Deleted BookingGuests                  : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM CreditNotes;
    PRINT 'Deleted CreditNotes                    : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BookingRoomNights;
    PRINT 'Deleted BookingRoomNights              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM ReservationRoomNights;
    PRINT 'Deleted ReservationRoomNights          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM BookingRooms;
    PRINT 'Deleted BookingRooms                   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 4 : Bookings
    --          (FK → Users [CreatedBy/LastModifiedBy], B2BAgreements)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 4: Bookings ---';

    DELETE FROM Bookings;
    PRINT 'Deleted Bookings                       : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 5 : B2B agreements and rates
    --          (BanquetBookings + Bookings already deleted above)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 5: B2B agreements ---';

    -- B2BAgreementRoomRates → child of B2BAgreements (must go first)
    DELETE FROM B2BAgreementRoomRates;
    PRINT 'Deleted B2BAgreementRoomRates          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- B2BClients.AgreementId → FK → B2BAgreements (B2BClients is kept as master,
    -- so NULL out the FK reference before deleting agreements)
    UPDATE B2BClients SET AgreementId = NULL WHERE AgreementId IS NOT NULL;
    PRINT 'Cleared  B2BClients.AgreementId        : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- B2BAgreements.TermsConditionId → FK → B2BTermsConditions
    -- so B2BAgreements must be deleted BEFORE B2BTermsConditions
    DELETE FROM B2BAgreements;
    PRINT 'Deleted B2BAgreements                  : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM B2BTermsConditions;
    PRINT 'Deleted B2BTermsConditions             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- NOTE: B2BClients is kept as a master/client directory.
    -- Uncomment the line below if you also want to clear the client list:
    -- DELETE FROM B2BClients;
    -- PRINT 'Deleted B2BClients                     : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 6 : Guests and feedback
    --          (BanquetBookings + BookingGuests already deleted)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 6: Guests ---';

    DELETE FROM GuestFeedback;
    PRINT 'Deleted GuestFeedback                  : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM Guests;
    PRINT 'Deleted Guests                         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 7 : Asset transactional records
    --          (master tables AssetItems, AssetDepartments etc. are KEPT)
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 7: Asset transactional records ---';

    DELETE FROM AssetDamageLossRecoveries;
    PRINT 'Deleted AssetDamageLossRecoveries      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM AssetDamageLoss;
    PRINT 'Deleted AssetDamageLoss                : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM AssetAllocations;
    PRINT 'Deleted AssetAllocations               : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM AssetMovementLines;
    PRINT 'Deleted AssetMovementLines             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM AssetMovements;
    PRINT 'Deleted AssetMovements                 : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM AssetStockBalances;
    PRINT 'Deleted AssetStockBalances             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 8 : Room maintenance history
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 8: Room maintenance ---';

    DELETE FROM RoomMaintenanceHistory;
    PRINT 'Deleted RoomMaintenanceHistory         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 9 : Logs and tokens
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 9: Logs and tokens ---';

    DELETE FROM LicenseValidationHistory;
    PRINT 'Deleted LicenseValidationHistory       : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    DELETE FROM EInvoiceIrpTokens;
    PRINT 'Deleted EInvoiceIrpTokens              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows';

    -- --------------------------------------------------------
    -- STEP 10 : Reset all sequence / counter tables
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 10: Reset sequences ---';

    UPDATE InvoiceSequence       SET LastSequence = 0;
    PRINT 'Reset  InvoiceSequence';

    UPDATE CreditNoteSequence    SET LastSequence = 0;
    PRINT 'Reset  CreditNoteSequence';

    UPDATE BanquetBookingCounter SET LastNumber = 0;
    PRINT 'Reset  BanquetBookingCounter';

    UPDATE BanquetReceiptCounter SET LastNumber = 0;
    PRINT 'Reset  BanquetReceiptCounter';

    UPDATE EInvoiceVersionSequence SET LastSequence = 0
    WHERE EXISTS (SELECT 1 FROM EInvoiceVersionSequence);
    PRINT 'Reset  EInvoiceVersionSequence';

    -- --------------------------------------------------------
    -- STEP 11 : Remove all non-Admin users and their mappings
    -- --------------------------------------------------------
    PRINT '';
    PRINT '--- STEP 11: Non-Admin users ---';

    DECLARE @AdminId INT = (
        SELECT Id FROM Users WHERE Username = 'Admin'
    );

    IF @AdminId IS NULL
    BEGIN
        RAISERROR('CRITICAL: Admin user not found! Aborting cleanup to prevent full user deletion.', 16, 1);
    END

    PRINT 'Admin user Id = ' + CAST(@AdminId AS VARCHAR);

    DELETE FROM AuthorizationUserPermissions WHERE UserId <> @AdminId;
    PRINT 'Deleted AuthorizationUserPermissions   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows (non-Admin)';

    DELETE FROM UserBranchRoles WHERE UserId <> @AdminId;
    PRINT 'Deleted UserBranchRoles                : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows (non-Admin)';

    DELETE FROM UserBranches WHERE UserId <> @AdminId;
    PRINT 'Deleted UserBranches                   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows (non-Admin)';

    DELETE FROM UserRoles WHERE UserId <> @AdminId;
    PRINT 'Deleted UserRoles                      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows (non-Admin)';

    DELETE FROM Users WHERE Id <> @AdminId;
    PRINT 'Deleted Users                          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows (non-Admin)';

    -- --------------------------------------------------------
    -- Summary
    -- --------------------------------------------------------
    PRINT '';
    PRINT '============================================================';
    PRINT ' Cleanup COMPLETED successfully.';
    PRINT ' Admin user (Id=' + CAST(@AdminId AS VARCHAR) + ') preserved.';
    PRINT ' All master / configuration data preserved.';
    PRINT '============================================================';

    COMMIT TRANSACTION;

END TRY
BEGIN CATCH

    ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!';
    PRINT ' ERROR — Cleanup FAILED. Transaction rolled back.';
    PRINT ' Error ' + CAST(ERROR_NUMBER() AS VARCHAR) + ': ' + ERROR_MESSAGE();
    PRINT ' Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!';

    THROW;

END CATCH;
