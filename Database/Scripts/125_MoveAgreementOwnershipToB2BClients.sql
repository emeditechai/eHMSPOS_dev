IF COL_LENGTH('dbo.B2BClients', 'AgreementId') IS NULL
BEGIN
    ALTER TABLE dbo.B2BClients ADD AgreementId INT NULL;
END
GO

IF COL_LENGTH('dbo.B2BAgreements', 'ClientId') IS NOT NULL
BEGIN
    ;WITH RankedAssignments AS
    (
        SELECT a.Id AS AgreementId,
               a.ClientId,
               ROW_NUMBER() OVER
               (
                   PARTITION BY a.ClientId
                   ORDER BY CASE WHEN a.IsActive = 1 THEN 0 ELSE 1 END,
                            a.EffectiveTo DESC,
                            a.EffectiveFrom DESC,
                            ISNULL(a.UpdatedDate, a.CreatedDate) DESC,
                            a.Id DESC
               ) AS RowNumber
          FROM dbo.B2BAgreements a
         WHERE a.ClientId IS NOT NULL
    )
    UPDATE client
       SET AgreementId = ranked.AgreementId
      FROM dbo.B2BClients client
      JOIN RankedAssignments ranked ON ranked.ClientId = client.Id AND ranked.RowNumber = 1
     WHERE client.AgreementId IS NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BAgreements_Client')
BEGIN
    ALTER TABLE dbo.B2BAgreements DROP CONSTRAINT FK_B2BAgreements_Client;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_B2BAgreements_Client' AND object_id = OBJECT_ID('dbo.B2BAgreements'))
BEGIN
    DROP INDEX IX_B2BAgreements_Client ON dbo.B2BAgreements;
END
GO

IF COL_LENGTH('dbo.B2BAgreements', 'ClientId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.B2BAgreements ALTER COLUMN ClientId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_B2BClients_Agreement')
    AND COL_LENGTH('dbo.B2BClients', 'AgreementId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.B2BClients WITH CHECK
        ADD CONSTRAINT FK_B2BClients_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.B2BAgreements(Id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_B2BClients_AgreementId' AND object_id = OBJECT_ID('dbo.B2BClients'))
BEGIN
    CREATE INDEX IX_B2BClients_AgreementId ON dbo.B2BClients(AgreementId);
END
GO

PRINT 'Agreement ownership moved to B2BClients.AgreementId.';
GO