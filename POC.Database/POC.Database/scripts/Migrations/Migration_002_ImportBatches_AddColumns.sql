-- ============================================================
--  Migration 002 — ImportBatches: FileType, DuplicateStrategy,
--                  FileStoragePath
--
--  FileType records the uploaded file format (csv, xlsx, xls)
--  so the correct parser is used during preview and execution.
--
--  DuplicateStrategy controls how the import handles customers
--  that already exist (matched by Email within the same org):
--    skip   = ignore the incoming row
--    update = overwrite existing FieldValues with new data
--    error  = write the row to ImportErrors as a duplicate
--
--  FileStoragePath stores the server-side path to the uploaded
--  file so it can be re-parsed for preview and execution without
--  requiring the admin to re-upload.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    -- FileType
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.ImportBatches')
          AND name = 'FileType'
    )
    BEGIN
        ALTER TABLE dbo.ImportBatches
            ADD [FileType] nvarchar(10) NULL;

        PRINT 'ImportBatches.FileType column added.';
    END
    ELSE
        PRINT 'ImportBatches.FileType already exists — skipped.';


    -- DuplicateStrategy
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.ImportBatches')
          AND name = 'DuplicateStrategy'
    )
    BEGIN
        ALTER TABLE dbo.ImportBatches
            ADD [DuplicateStrategy] nvarchar(10) NOT NULL DEFAULT ('skip');

        PRINT 'ImportBatches.DuplicateStrategy column added.';
    END
    ELSE
        PRINT 'ImportBatches.DuplicateStrategy already exists — skipped.';


    -- FileStoragePath
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.ImportBatches')
          AND name = 'FileStoragePath'
    )
    BEGIN
        ALTER TABLE dbo.ImportBatches
            ADD [FileStoragePath] nvarchar(500) NULL;

        PRINT 'ImportBatches.FileStoragePath column added.';
    END
    ELSE
        PRINT 'ImportBatches.FileStoragePath already exists — skipped.';


    -- Add check constraint for FileType values
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.ImportBatches')
          AND name = 'CK_ImportBatches_FileType'
    )
    BEGIN
        ALTER TABLE dbo.ImportBatches
            ADD CONSTRAINT [CK_ImportBatches_FileType]
            CHECK (FileType IS NULL OR FileType IN ('csv', 'xlsx', 'xls'));

        PRINT 'CK_ImportBatches_FileType constraint added.';
    END
    ELSE
        PRINT 'CK_ImportBatches_FileType already exists — skipped.';


    -- Add check constraint for DuplicateStrategy values
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.ImportBatches')
          AND name = 'CK_ImportBatches_DuplicateStrategy'
    )
    BEGIN
        ALTER TABLE dbo.ImportBatches
            ADD CONSTRAINT [CK_ImportBatches_DuplicateStrategy]
            CHECK (DuplicateStrategy IN ('skip', 'update', 'error'));

        PRINT 'CK_ImportBatches_DuplicateStrategy constraint added.';
    END
    ELSE
        PRINT 'CK_ImportBatches_DuplicateStrategy already exists — skipped.';


    COMMIT TRANSACTION;
    PRINT 'Migration 002 completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 002 FAILED — transaction rolled back.';
    THROW;
END CATCH;
