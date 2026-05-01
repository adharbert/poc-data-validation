-- ============================================================
--  Migration 007 — ImportColumnMappings + SavedColumnMappings:
--  Column mapping transform redesign
--
--  Changes applied to ImportColumnMappings:
--    • MappingType column replaced by DestinationTable
--        customer_field   → customer
--        field_definition → field_value
--        skip             → skip
--    • CustomerFieldName column renamed to DestinationField;
--      valid values expanded to include address fields
--    • TransformType column added (direct | split_full_name |
--      split_full_address | strip_credentials)
--
--  Same changes mirrored to SavedColumnMappings.
--
--  New tables ImportColumnMappingOutputs and
--  SavedColumnMappingOutputs are created here so that this
--  single script brings any existing database fully up to date.
--
--  Safe to re-run: every step is guarded by IF EXISTS /
--  IF NOT EXISTS.  sp_rename calls run outside TRY/CATCH to
--  avoid the "uncommittable transaction" warning SQL Server
--  emits when sp_rename executes inside a TRY block.
-- ============================================================
SET NOCOUNT ON;

-- ============================================================
--  1. ImportColumnMappings — additive changes + backfill
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ImportColumnMappings') AND name = 'DestinationTable')
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings
            ADD [DestinationTable] NVARCHAR(20) NULL;
        PRINT 'ImportColumnMappings.DestinationTable added.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ImportColumnMappings') AND name = 'TransformType')
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings
            ADD [TransformType] NVARCHAR(30) NOT NULL
                CONSTRAINT [DF_ImportColumnMappings_TransformType] DEFAULT ('direct');
        PRINT 'ImportColumnMappings.TransformType added.';
    END

    -- Backfill DestinationTable from MappingType where present
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ImportColumnMappings') AND name = 'MappingType')
    BEGIN
        UPDATE dbo.ImportColumnMappings
        SET    DestinationTable = CASE MappingType
                   WHEN 'customer_field'   THEN 'customer'
                   WHEN 'field_definition' THEN 'field_value'
                   ELSE 'skip'
               END
        WHERE  DestinationTable IS NULL;
    END

    -- Default any remaining NULLs (re-run safety)
    UPDATE dbo.ImportColumnMappings
    SET    DestinationTable = 'skip'
    WHERE  DestinationTable IS NULL;

    ALTER TABLE dbo.ImportColumnMappings
        ALTER COLUMN [DestinationTable] NVARCHAR(20) NOT NULL;

    COMMIT TRANSACTION;
    PRINT 'ImportColumnMappings — phase 1 complete.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 007 phase 1 FAILED.';
    THROW;
END CATCH;
GO

-- ============================================================
--  2. ImportColumnMappings — rename CustomerFieldName
--     (sp_rename outside TRY to avoid warning)
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ImportColumnMappings') AND name = 'CustomerFieldName')
BEGIN
    EXEC sp_rename 'dbo.ImportColumnMappings.CustomerFieldName', 'DestinationField', 'COLUMN';
    PRINT 'Renamed ImportColumnMappings.CustomerFieldName → DestinationField.';
END
GO

-- ============================================================
--  3. ImportColumnMappings — drop old constraints + column,
--     add new constraints
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ImportColumnMappings_Type' AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings'))
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings DROP CONSTRAINT CK_ImportColumnMappings_Type;
        PRINT 'Dropped CK_ImportColumnMappings_Type.';
    END

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ImportColumnMappings_CustomerField' AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings'))
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings DROP CONSTRAINT CK_ImportColumnMappings_CustomerField;
        PRINT 'Dropped CK_ImportColumnMappings_CustomerField.';
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ImportColumnMappings') AND name = 'MappingType')
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings DROP COLUMN MappingType;
        PRINT 'ImportColumnMappings.MappingType dropped.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ImportColumnMappings_DestinationTable' AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings'))
        ALTER TABLE dbo.ImportColumnMappings ADD CONSTRAINT CK_ImportColumnMappings_DestinationTable CHECK (
            DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
        );

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ImportColumnMappings_TransformType' AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings'))
        ALTER TABLE dbo.ImportColumnMappings ADD CONSTRAINT CK_ImportColumnMappings_TransformType CHECK (
            TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
        );

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ImportColumnMappings_DestinationField' AND parent_object_id = OBJECT_ID('dbo.ImportColumnMappings'))
        ALTER TABLE dbo.ImportColumnMappings ADD CONSTRAINT CK_ImportColumnMappings_DestinationField CHECK (
            DestinationTable IN ('skip', 'field_value')
            OR TransformType <> 'direct'
            OR (DestinationTable = 'customer' AND DestinationField IN (
                'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
                'Phone', 'Email', 'OriginalId', 'CustomerCode'
            ))
            OR (DestinationTable = 'customer_address' AND DestinationField IN (
                'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
                'Country', 'AddressType', 'Latitude', 'Longitude'
            ))
        );

    COMMIT TRANSACTION;
    PRINT 'ImportColumnMappings — phase 3 complete.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 007 phase 3 FAILED.';
    THROW;
END CATCH;
GO

-- ============================================================
--  4. SavedColumnMappings — additive changes + backfill
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SavedColumnMappings') AND name = 'DestinationTable')
    BEGIN
        ALTER TABLE dbo.SavedColumnMappings
            ADD [DestinationTable] NVARCHAR(20) NULL;
        PRINT 'SavedColumnMappings.DestinationTable added.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SavedColumnMappings') AND name = 'TransformType')
    BEGIN
        ALTER TABLE dbo.SavedColumnMappings
            ADD [TransformType] NVARCHAR(30) NOT NULL
                CONSTRAINT [DF_SavedColumnMappings_TransformType] DEFAULT ('direct');
        PRINT 'SavedColumnMappings.TransformType added.';
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SavedColumnMappings') AND name = 'MappingType')
    BEGIN
        UPDATE dbo.SavedColumnMappings
        SET    DestinationTable = CASE MappingType
                   WHEN 'customer_field'   THEN 'customer'
                   WHEN 'field_definition' THEN 'field_value'
                   ELSE 'skip'
               END
        WHERE  DestinationTable IS NULL;
    END

    UPDATE dbo.SavedColumnMappings
    SET    DestinationTable = 'skip'
    WHERE  DestinationTable IS NULL;

    ALTER TABLE dbo.SavedColumnMappings
        ALTER COLUMN [DestinationTable] NVARCHAR(20) NOT NULL;

    COMMIT TRANSACTION;
    PRINT 'SavedColumnMappings — phase 4 complete.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 007 phase 4 FAILED.';
    THROW;
END CATCH;
GO

-- ============================================================
--  5. SavedColumnMappings — rename CustomerFieldName
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SavedColumnMappings') AND name = 'CustomerFieldName')
BEGIN
    EXEC sp_rename 'dbo.SavedColumnMappings.CustomerFieldName', 'DestinationField', 'COLUMN';
    PRINT 'Renamed SavedColumnMappings.CustomerFieldName → DestinationField.';
END
GO

-- ============================================================
--  6. SavedColumnMappings — drop old column, add new constraints
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Drop any unnamed check constraints referencing MappingType dynamically
    DECLARE @sql NVARCHAR(500);
    SELECT @sql = 'ALTER TABLE dbo.SavedColumnMappings DROP CONSTRAINT ' + name
    FROM   sys.check_constraints
    WHERE  parent_object_id = OBJECT_ID('dbo.SavedColumnMappings')
      AND  definition LIKE '%MappingType%';
    IF @sql IS NOT NULL
    BEGIN
        EXEC sp_executesql @sql;
        PRINT 'Dropped legacy MappingType constraint from SavedColumnMappings.';
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SavedColumnMappings') AND name = 'MappingType')
    BEGIN
        ALTER TABLE dbo.SavedColumnMappings DROP COLUMN MappingType;
        PRINT 'SavedColumnMappings.MappingType dropped.';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SavedColumnMappings_DestinationTable' AND parent_object_id = OBJECT_ID('dbo.SavedColumnMappings'))
        ALTER TABLE dbo.SavedColumnMappings ADD CONSTRAINT CK_SavedColumnMappings_DestinationTable CHECK (
            DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
        );

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SavedColumnMappings_TransformType' AND parent_object_id = OBJECT_ID('dbo.SavedColumnMappings'))
        ALTER TABLE dbo.SavedColumnMappings ADD CONSTRAINT CK_SavedColumnMappings_TransformType CHECK (
            TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
        );

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SavedColumnMappings_DestinationField' AND parent_object_id = OBJECT_ID('dbo.SavedColumnMappings'))
        ALTER TABLE dbo.SavedColumnMappings ADD CONSTRAINT CK_SavedColumnMappings_DestinationField CHECK (
            DestinationTable IN ('skip', 'field_value')
            OR TransformType <> 'direct'
            OR (DestinationTable = 'customer' AND DestinationField IN (
                'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
                'Phone', 'Email', 'OriginalId', 'CustomerCode'
            ))
            OR (DestinationTable = 'customer_address' AND DestinationField IN (
                'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
                'Country', 'AddressType', 'Latitude', 'Longitude'
            ))
        );

    COMMIT TRANSACTION;
    PRINT 'SavedColumnMappings — phase 6 complete.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 007 phase 6 FAILED.';
    THROW;
END CATCH;
GO

-- ============================================================
--  7. ImportColumnMappingOutputs — create if absent
-- ============================================================

IF OBJECT_ID('dbo.ImportColumnMappingOutputs', 'U') IS NULL
    CREATE TABLE dbo.ImportColumnMappingOutputs (
        [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_Id]               DEFAULT (NEWSEQUENTIALID()),
        [MappingId]         UNIQUEIDENTIFIER    NOT NULL,
        [OutputToken]       NVARCHAR(50)        NOT NULL,
        [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_DestinationTable] DEFAULT ('skip'),
        [DestinationField]  NVARCHAR(100)       NULL,
        [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
        [SortOrder]         INT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_SortOrder]        DEFAULT (0),

        CONSTRAINT [PK_ImportColumnMappingOutputs]          PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ImportColumnMappingOutputs_Mapping]  FOREIGN KEY ([MappingId])         REFERENCES dbo.ImportColumnMappings ([Id]),
        CONSTRAINT [FK_ImportColumnMappingOutputs_FieldDef] FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),
        CONSTRAINT [UQ_ImportColumnMappingOutputs_Token]    UNIQUE ([MappingId], [OutputToken]),
        CONSTRAINT [CK_ImportColumnMappingOutputs_DestinationTable] CHECK (
            DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
        )
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ImportColumnMappingOutputs_MappingId' AND object_id = OBJECT_ID('dbo.ImportColumnMappingOutputs'))
    CREATE INDEX [IX_ImportColumnMappingOutputs_MappingId]
        ON dbo.ImportColumnMappingOutputs ([MappingId], [SortOrder]);

PRINT 'ImportColumnMappingOutputs ready.';
GO

-- ============================================================
--  8. SavedColumnMappingOutputs — create if absent
-- ============================================================

IF OBJECT_ID('dbo.SavedColumnMappingOutputs', 'U') IS NULL
    CREATE TABLE dbo.SavedColumnMappingOutputs (
        [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_Id]               DEFAULT (NEWSEQUENTIALID()),
        [MappingId]         UNIQUEIDENTIFIER    NOT NULL,
        [OutputToken]       NVARCHAR(50)        NOT NULL,
        [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_DestinationTable] DEFAULT ('skip'),
        [DestinationField]  NVARCHAR(100)       NULL,
        [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
        [SortOrder]         INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_SortOrder]        DEFAULT (0),

        CONSTRAINT [PK_SavedColumnMappingOutputs]          PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_SavedColumnMappingOutputs_Mapping]  FOREIGN KEY ([MappingId])         REFERENCES dbo.SavedColumnMappings ([Id]),
        CONSTRAINT [FK_SavedColumnMappingOutputs_FieldDef] FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),
        CONSTRAINT [UQ_SavedColumnMappingOutputs_Token]    UNIQUE ([MappingId], [OutputToken]),
        CONSTRAINT [CK_SavedColumnMappingOutputs_DestinationTable] CHECK (
            DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
        )
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SavedColumnMappingOutputs_MappingId' AND object_id = OBJECT_ID('dbo.SavedColumnMappingOutputs'))
    CREATE INDEX [IX_SavedColumnMappingOutputs_MappingId]
        ON dbo.SavedColumnMappingOutputs ([MappingId], [SortOrder]);

PRINT 'SavedColumnMappingOutputs ready.';
PRINT 'Migration 007 completed successfully.';
GO
