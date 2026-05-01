-- ============================================================
--  Migration 005 — Customers: MaidenName + DateOfBirth
--
--  Adds the two boilerplate fields needed for standardised
--  customer identity across all organisations.
--
--  Both columns are nullable — existing rows are unaffected.
--  Temporal versioning stays on; SQL Server propagates nullable
--  ADD columns to the history table automatically.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.Customers')
          AND  name = 'MaidenName'
    )
    BEGIN
        ALTER TABLE dbo.Customers
            ADD [MaidenName] NVARCHAR(50) NULL;

        PRINT 'Customers.MaidenName column added.';
    END
    ELSE
        PRINT 'Customers.MaidenName already exists — skipped.';

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.Customers')
          AND  name = 'DateOfBirth'
    )
    BEGIN
        ALTER TABLE dbo.Customers
            ADD [DateOfBirth] DATE NULL;

        PRINT 'Customers.DateOfBirth column added.';
    END
    ELSE
        PRINT 'Customers.DateOfBirth already exists — skipped.';

    COMMIT TRANSACTION;
    PRINT 'Migration 005 completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 005 FAILED — transaction rolled back.';
    THROW;
END CATCH;
