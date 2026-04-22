/*
 Post-Deployment Script
 Runs AFTER the DACPAC has applied all schema changes.

 Execution order:
   1. Re-enable system versioning on temporal tables
   2. Seed static reference data (idempotent — safe to re-run)
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
-- ============================================================

:r ".\SeedData\01_StateOptions.sql"
:r ".\SeedData\02_HighestSchoolingOptions.sql"

PRINT 'PostDeploy completed successfully.';
