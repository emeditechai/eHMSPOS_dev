-- Seed Terms & Conditions Master under Settings

IF NOT EXISTS (SELECT 1 FROM NavMenuItems WHERE Code = 'SETTINGS_TERMS_CONDITION_MASTER')
BEGIN
    INSERT INTO NavMenuItems (Code, Title, IconClass, Controller, Action, ParentId, SortOrder, IsActive)
    VALUES ('SETTINGS_TERMS_CONDITION_MASTER', 'Terms & Conditions Master', 'fas fa-scroll', 'TermsConditionMaster', 'Index', (SELECT Id FROM NavMenuItems WHERE Code = 'SETTINGS'), 80, 1);
END
GO