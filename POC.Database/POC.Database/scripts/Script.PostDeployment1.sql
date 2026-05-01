/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

SET NOCOUNT ON;

-- ============================================================
--  1. Re-enable system versioning
--     History tables are created automatically if they do not
--     already exist.
-- ============================================================

IF OBJECT_ID(N'dbo.Organizations', 'U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID(N'dbo.Organizations', 'U'), 'TableTemporalType') <> 2
BEGIN
    IF OBJECT_ID(N'dbo.Organizations_History', 'U') IS NULL
        ALTER TABLE [dbo].[Organizations]
            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Organizations_History], DATA_CONSISTENCY_CHECK = ON));
    ELSE
        ALTER TABLE [dbo].[Organizations]
            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Organizations_History], DATA_CONSISTENCY_CHECK = ON));
END

IF OBJECT_ID(N'dbo.Customers', 'U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID(N'dbo.Customers', 'U'), 'TableTemporalType') <> 2
BEGIN
    IF OBJECT_ID(N'dbo.Customers_History', 'U') IS NULL
        ALTER TABLE [dbo].[Customers]
            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Customers_History], DATA_CONSISTENCY_CHECK = ON));
    ELSE
        ALTER TABLE [dbo].[Customers]
            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Customers_History], DATA_CONSISTENCY_CHECK = ON));
END


-- ============================================================
--  2. Seed Data — executed in dependency order
--     Each script is idempotent: it checks for existing data
--     before inserting so re-running the deployment never
--     duplicates rows or loses existing data.
--
--     Each :r is separated by GO so that a RETURN inside an
--     included script exits only that batch, not the entire
--     post-deployment script.
-- ============================================================

GO
--:r .\Post-Deployment\01_StateOptions.sql
--GO
--:r .\Post-Deployment\02_HighestSchoolingOptions.sql
--GO
--:r .\Migrations\Migration_006b_CustomerAddresses_GeographyPoint.sql
--GO

PRINT 'PostDeploy completed successfully.';