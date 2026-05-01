-- ============================================================
--  Migration 006 — CustomerAddresses: AddressType
--
--  Adds AddressType so multiple simultaneous active addresses
--  can be classified (primary, secondary, mailing, vacation).
--
--  Existing rows default to 'primary'.
--  The check constraint enforces the allowed type list.
--
--  Temporal versioning stays on; SQL Server propagates a NOT NULL
--  column with a default to the history table automatically.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.CustomerAddresses')
          AND  name = 'AddressType'
    )
    BEGIN
        ALTER TABLE dbo.CustomerAddresses
            ADD [AddressType] NVARCHAR(20) NOT NULL
                CONSTRAINT [DF_CustomerAddresses_AddressType] DEFAULT ('primary');

        PRINT 'CustomerAddresses.AddressType column added.';
    END
    ELSE
        PRINT 'CustomerAddresses.AddressType already exists — skipped.';

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE  parent_object_id = OBJECT_ID('dbo.CustomerAddresses')
          AND  name = 'CK_CustomerAddresses_AddressType'
    )
    BEGIN
        ALTER TABLE dbo.CustomerAddresses
            ADD CONSTRAINT [CK_CustomerAddresses_AddressType]
            CHECK ([AddressType] IN ('primary', 'secondary', 'mailing', 'vacation', 'other'));

        PRINT 'CK_CustomerAddresses_AddressType constraint added.';
    END
    ELSE
        PRINT 'CK_CustomerAddresses_AddressType already exists — skipped.';

    COMMIT TRANSACTION;
    PRINT 'Migration 006 completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 006 FAILED — transaction rolled back.';
    THROW;
END CATCH;
