-- ============================================================
--  Seed: Field Options — Highest Degree / Schooling
--
--  FieldDefinition targeted by FieldKey = 'highest_degree'.
--  Uses MERGE so the script is fully idempotent:
--    - New database  → all rows inserted
--    - Existing database → only missing rows inserted; existing
--      rows are left untouched (no data loss, no duplicates)
-- ============================================================

SET NOCOUNT ON;

DECLARE @FieldDefinitionId UNIQUEIDENTIFIER;

SELECT @FieldDefinitionId = [Id]
FROM   [dbo].[FieldDefinitions]
WHERE  [FieldKey] = 'highest_degree';

IF @FieldDefinitionId IS NULL
BEGIN
    PRINT 'Highest degree FieldDefinition not found — skipping schooling options seed.';
    RETURN;
END

DECLARE @Options TABLE (
    OptionKey    NVARCHAR(100) NOT NULL,
    OptionLabel  NVARCHAR(200) NOT NULL,
    DisplayOrder INT           NOT NULL
);

INSERT INTO @Options (OptionKey, OptionLabel, DisplayOrder) VALUES
    ('none', 'No formal qualification',     1),
    ('hs',   'High school diploma (GED)',   2),
    ('assoc','Associates degree',           3),
    ('bach', 'Bachelors degree',            4),
    ('mast', 'Masters degree',              5),
    ('phd',  'Doctorate / PhD',             6),
    ('prof', 'Professional degree (MD,JD)', 7);

MERGE [dbo].[FieldOptions] AS target
USING (
    SELECT @FieldDefinitionId AS FieldDefinitionId,
           OptionKey, OptionLabel, DisplayOrder
    FROM   @Options
) AS source
    ON target.[FieldDefinitionId] = source.FieldDefinitionId
   AND target.[OptionKey]         = source.OptionKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [FieldDefinitionId], [OptionKey], [OptionLabel], [DisplayOrder], [IsActive])
    VALUES (NEWID(), source.FieldDefinitionId, source.OptionKey, source.OptionLabel, source.DisplayOrder, 1);

PRINT 'Highest schooling options seed completed.';
