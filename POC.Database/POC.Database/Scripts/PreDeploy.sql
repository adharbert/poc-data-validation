/*
 Pre-Deployment Script
 Runs BEFORE the DACPAC applies schema changes.

 Purpose:
   - Disable system versioning on temporal tables so the DACPAC can
     safely ALTER them (SQL Server blocks ALTER on versioned tables).
   - Re-enable versioning is handled in PostDeploy.sql after the diff
     has been applied.

 Safe to run on:
   - A brand-new empty database  (IF EXISTS guards prevent errors)
   - An existing database        (versioning is toggled gracefully)
*/

SET NOCOUNT ON;

-- ============================================================
--  Disable system versioning before DACPAC alters these tables
--  so ALTER TABLE statements don't fail on versioned tables.
-- ============================================================

IF OBJECT_ID(N'dbo.Organizations', 'U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID(N'dbo.Organizations', 'U'), 'TableTemporalType') = 2
BEGIN
    ALTER TABLE [dbo].[Organizations] SET (SYSTEM_VERSIONING = OFF);
END

IF OBJECT_ID(N'dbo.Customers', 'U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID(N'dbo.Customers', 'U'), 'TableTemporalType') = 2
BEGIN
    ALTER TABLE [dbo].[Customers] SET (SYSTEM_VERSIONING = OFF);
END
