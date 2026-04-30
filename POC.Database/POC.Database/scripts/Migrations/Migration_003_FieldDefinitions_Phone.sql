-- ============================================================
--  Migration 003 — FieldDefinitions: phone field type + DisplayFormat
--
--  Adds the DisplayFormat column for storing the admin-chosen
--  phone display format per field definition.
--
--  The CK_FieldDefinitions_Type constraint (updated in
--  FieldDefinitions.sql to include 'phone') is handled by
--  SSDT schema compare on publish, or run manually:
--
--    ALTER TABLE dbo.FieldDefinitions DROP CONSTRAINT [CK_FieldDefinitions_Type];
--    ALTER TABLE dbo.FieldDefinitions ADD CONSTRAINT [CK_FieldDefinitions_Type]
--        CHECK ([FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone'));
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE  object_id = OBJECT_ID('dbo.FieldDefinitions')
          AND  name = 'DisplayFormat'
    )
    BEGIN
        ALTER TABLE dbo.FieldDefinitions
            ADD [DisplayFormat] nvarchar(20) NULL;

        PRINT 'FieldDefinitions.DisplayFormat column added.';
    END
    ELSE
        PRINT 'FieldDefinitions.DisplayFormat already exists — skipped.';


    COMMIT TRANSACTION;
    PRINT 'Migration 003 completed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Migration 003 FAILED — transaction rolled back.';
    THROW;
END CATCH;
