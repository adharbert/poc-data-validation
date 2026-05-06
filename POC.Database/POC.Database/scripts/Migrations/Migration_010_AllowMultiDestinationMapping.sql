-- ============================================================
--  Migration 010 — Allow multiple destination mappings per CSV column
--
--  Drops the unique constraint on (ImportBatchId, CsvHeader) so that
--  the same source column can be mapped to more than one destination
--  (e.g. an Email column going to both customer.Email AND a field_value).
--  The import service already iterates all mapping rows per column
--  using CsvColumnIndex, so no service changes are required.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE name = 'UQ_ImportColumnMappings_Header' AND type = 'UQ'
    )
    BEGIN
        ALTER TABLE dbo.ImportColumnMappings
            DROP CONSTRAINT [UQ_ImportColumnMappings_Header];
        PRINT 'Dropped UQ_ImportColumnMappings_Header — multi-destination mapping now allowed.';
    END
    ELSE PRINT 'UQ_ImportColumnMappings_Header already absent — skipped.';

    COMMIT TRANSACTION;
    PRINT 'Migration 010 completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 010 FAILED — transaction rolled back.';
    THROW;
END CATCH;
