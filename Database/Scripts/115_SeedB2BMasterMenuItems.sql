    -- Seed B2B master menu items under Settings

    IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_B2B_CLIENT_MASTER')
    BEGIN
        INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
        VALUES ('SETTINGS_B2B_CLIENT_MASTER', 'B2B Client Master', 'fas fa-building-user', 'B2BClientMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 77, 1);
    END
    GO

    IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_AGREEMENT_MASTER')
    BEGIN
        INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
        VALUES ('SETTINGS_AGREEMENT_MASTER', 'Agreement Master', 'fas fa-file-signature', 'AgreementMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 78, 1);
    END
    GO

    IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_GST_SLAB_MASTER')
    BEGIN
        INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
        VALUES ('SETTINGS_GST_SLAB_MASTER', 'GST Slab Master', 'fas fa-percent', 'GstSlabMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 79, 1);
    END
    GO

    IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER')
    BEGIN
        INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
        VALUES ('SETTINGS_TERMS_CONDITION_MASTER', 'Terms & Conditions Master', 'fas fa-scroll', 'TermsConditionMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 80, 1);
    END
    GO