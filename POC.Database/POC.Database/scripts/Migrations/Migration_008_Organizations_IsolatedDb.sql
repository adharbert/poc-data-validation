-- ============================================================
--  Migration 008 — Organizations: isolated database support
--
--  Adds three columns to support per-client database isolation:
--    RequiresIsolatedDatabase  — flag set by admin
--    IsolatedConnectionString  — populated by provisioning service
--    DatabaseProvisioningStatus — pending | provisioning | ready | failed
--
--  Safe to re-run: every ALTER is guarded by IF NOT EXISTS.
-- ============================================================
SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Organizations')
      AND  name = 'RequiresIsolatedDatabase'
)
BEGIN
    ALTER TABLE dbo.Organizations
        ADD [RequiresIsolatedDatabase] BIT NOT NULL DEFAULT (0);
    PRINT 'Added Organizations.RequiresIsolatedDatabase';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Organizations')
      AND  name = 'IsolatedConnectionString'
)
BEGIN
    ALTER TABLE dbo.Organizations
        ADD [IsolatedConnectionString] NVARCHAR(500) NULL;
    PRINT 'Added Organizations.IsolatedConnectionString';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE  object_id = OBJECT_ID('dbo.Organizations')
      AND  name = 'DatabaseProvisioningStatus'
)
BEGIN
    ALTER TABLE dbo.Organizations
        ADD [DatabaseProvisioningStatus] NVARCHAR(20) NULL;
    PRINT 'Added Organizations.DatabaseProvisioningStatus';
END
GO
