-- ============================================================
--  Migration 001 — Customers.OriginalId
--
--  Adds an OriginalId column to the Customers table to store
--  the client's own identifier for a customer (member number,
--  account ID, or any opaque string from the source system).
--
--  Also updates the ImportColumnMappings customer-field check
--  constraint to include OriginalId as a valid mapping target,
--  and removes CustomerCode (which is always system-generated
--  and should never be imported from a CSV column).
-- ============================================================
SET NOCOUNT ON


BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Add OriginalId column to Customers
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Customers')
          AND name = 'OriginalId'
    )
    BEGIN
        ALTER TABLE dbo.Customers
            ADD [OriginalId] nvarchar(100) NULL;

        PRINT 'Customers.OriginalId column added.';
    END
    ELSE
        PRINT 'Customers.OriginalId already exists — skipped.';


    -- 2. Add index for efficient lookup by OriginalId within an org
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Customers')
          AND name = 'IX_Customers_OriginalId'
    )
    BEGIN
        CREATE INDEX [IX_Customers_OriginalId]
            ON dbo.Customers (OrganizationId, OriginalId)
            WHERE OriginalId IS NOT NULL;

        PRINT 'IX_Customers_OriginalId index created.';
    END
    ELSE
        PRINT 'IX_Customers_OriginalId already exists — skipped.';


    -- 3. Update ImportColumnMappings check constraint to add OriginalId
    --    and remove CustomerCode (system-generated; never from CSV).
    --    SQL Server requires DROP + recreate for CHECK constraints.
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.ImportColumnMappings')
          AND name = 'CK_ImportColumnMappings_CustomerField'
    )
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings
            DROP CONSTRAINT [CK_ImportColumnMappings_CustomerField];

        PRINT 'CK_ImportColumnMappings_CustomerField dropped for recreation.';
    END

    ALTER TABLE dbo.ImportColumnMappings
        ADD CONSTRAINT [CK_ImportColumnMappings_CustomerField]
        CHECK (
            CustomerFieldName IS NULL OR CustomerFieldName IN (
                'FirstName', 'LastName', 'MiddleName', 'Email', 'OriginalId'
            )
        );

    PRINT 'CK_ImportColumnMappings_CustomerField recreated with OriginalId.';


    COMMIT TRANSACTION;
    PRINT 'Migration 001 completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 001 FAILED — transaction rolled back.';
    THROW;
END CATCH;
