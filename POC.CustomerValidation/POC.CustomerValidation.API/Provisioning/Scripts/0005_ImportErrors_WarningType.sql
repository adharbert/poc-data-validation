SET NOCOUNT ON;
GO

-- Add 'warning' to the ImportErrors ErrorType check constraint.
-- Warnings are used for rows that imported successfully but with partial data
-- (e.g., address skipped due to missing required fields).

IF EXISTS (
    SELECT 1
    FROM   sys.check_constraints
    WHERE  name = 'CK_ImportErrors_Type'
      AND  parent_object_id = OBJECT_ID('dbo.ImportErrors')
)
BEGIN
    ALTER TABLE dbo.ImportErrors DROP CONSTRAINT [CK_ImportErrors_Type];
END
GO

ALTER TABLE dbo.ImportErrors
    ADD CONSTRAINT [CK_ImportErrors_Type]
        CHECK (ErrorType IN ('validation', 'duplicate', 'system', 'warning'));
GO
