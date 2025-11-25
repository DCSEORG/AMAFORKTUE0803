-- Script to configure managed identity database roles
-- Replace MANAGED-IDENTITY-NAME with your actual managed identity name

-- Drop and recreate the managed identity user with correct SID
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'MANAGED-IDENTITY-NAME')
BEGIN
    DROP USER [MANAGED-IDENTITY-NAME];
END
GO

CREATE USER [MANAGED-IDENTITY-NAME] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

ALTER ROLE db_datawriter ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

GRANT EXECUTE TO [MANAGED-IDENTITY-NAME];
GO
