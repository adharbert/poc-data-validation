-- ============================================================
--  Seed: Field Options — US States
--
--  FieldDefinition targeted by FieldKey = 'state'.
--  Uses MERGE so the script is fully idempotent:
--    - New database  → all rows inserted
--    - Existing database → only missing rows inserted; existing
--      rows are left untouched (no data loss, no duplicates)
-- ============================================================

SET NOCOUNT ON;

DECLARE @FieldDefinitionId UNIQUEIDENTIFIER;

SELECT @FieldDefinitionId = [Id]
FROM   [dbo].[FieldDefinitions]
WHERE  [FieldKey] = 'state';

IF @FieldDefinitionId IS NULL
BEGIN
    PRINT 'State FieldDefinition not found — skipping state options seed.';
    RETURN;
END

-- Reference data for the merge
DECLARE @Options TABLE (
    OptionKey    NVARCHAR(100) NOT NULL,
    OptionLabel  NVARCHAR(200) NOT NULL,
    DisplayOrder INT           NOT NULL
);

INSERT INTO @Options (OptionKey, OptionLabel, DisplayOrder) VALUES
    ('AL', 'Alabama',                1),
    ('AK', 'Alaska',                 2),
    ('AZ', 'Arizona',                3),
    ('AR', 'Arkansas',               4),
    ('CA', 'California',             5),
    ('CO', 'Colorado',               6),
    ('CT', 'Connecticut',            7),
    ('DE', 'Delaware',               8),
    ('FL', 'Florida',                9),
    ('GA', 'Georgia',               10),
    ('HI', 'Hawaii',                11),
    ('ID', 'Idaho',                 12),
    ('IL', 'Illinois',              13),
    ('IN', 'Indiana',               14),
    ('IA', 'Iowa',                  15),
    ('KS', 'Kansas',                16),
    ('KY', 'Kentucky',              17),
    ('LA', 'Louisiana',             18),
    ('ME', 'Maine',                 19),
    ('MD', 'Maryland',              20),
    ('MA', 'Massachusetts',         21),
    ('MI', 'Michigan',              22),
    ('MN', 'Minnesota',             23),
    ('MS', 'Mississippi',           24),
    ('MO', 'Missouri',              25),
    ('MT', 'Montana',               26),
    ('NE', 'Nebraska',              27),
    ('NV', 'Nevada',                28),
    ('NH', 'New Hampshire',         29),
    ('NJ', 'New Jersey',            30),
    ('NM', 'New Mexico',            31),
    ('NY', 'New York',              32),
    ('NC', 'North Carolina',        33),
    ('ND', 'North Dakota',          34),
    ('OH', 'Ohio',                  35),
    ('OK', 'Oklahoma',              36),
    ('OR', 'Oregon',                37),
    ('PA', 'Pennsylvania',          38),
    ('RI', 'Rhode Island',          39),
    ('SC', 'South Carolina',        40),
    ('SD', 'South Dakota',          41),
    ('TN', 'Tennessee',             42),
    ('TX', 'Texas',                 43),
    ('UT', 'Utah',                  44),
    ('VT', 'Vermont',               45),
    ('VA', 'Virginia',              46),
    ('WA', 'Washington',            47),
    ('WV', 'West Virginia',         48),
    ('WI', 'Wisconsin',             49),
    ('WY', 'Wyoming',               50),
    ('DC', 'District of Columbia',  51),
    ('PR', 'Puerto Rico',           52),
    ('GU', 'Guam',                  53),
    ('VI', 'U.S. Virgin Islands',   54);

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

PRINT 'State options seed completed.';
