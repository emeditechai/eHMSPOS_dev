-- Script 133: Update RoleDashboardConfig for Cashier role
-- Maps the Cashier role to the new Payment Summary Dashboard
USE HMS_Dev;
GO

UPDATE RoleDashboardConfig
SET    DashboardController = 'CashierDashboard',
       DashboardAction     = 'Index',
       DisplayName         = 'Payment Summary Dashboard',
       IsActive            = 1,
       LastModifiedDate    = GETDATE()
WHERE  RoleId = (SELECT Id FROM Roles WHERE Name = 'Cashier');

-- If the Cashier role does not yet have a row, insert one
IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO RoleDashboardConfig
        (RoleId, DashboardController, DashboardAction, DisplayName, IsActive, CreatedDate, LastModifiedDate)
    SELECT
        Id, 'CashierDashboard', 'Index', 'Payment Summary Dashboard', 1, GETDATE(), GETDATE()
    FROM   Roles
    WHERE  Name = 'Cashier';
END
GO
