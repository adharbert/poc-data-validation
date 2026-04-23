
-- ============================================================
--  ImportValueMappings
--
--  Optional translation table for dropdown and multiselect
--  fields where the CSV value doesn't match the OptionKey
--  stored in FieldValues.
--
--  Example:
--    CSV has "Bachelor's Degree"  =>  system stores "bach"
--    CSV has "Masters"            =>  system stores "mast"
--    CSV has "Y" or "Yes"         =>  system stores "1" (checkbox)
--
--  CsvValue is stored lowercase trimmed for case-insensitive matching.
--  SystemValue must match an OptionKey in FieldOptions for the
--  mapped FieldDefinition, or '0'/'1' for checkbox fields.
--
--  If no value mapping exists for a CSV value the import engine
--  writes the raw CSV value to ValueText and flags it as a warning.
-- ============================================================
CREATE TABLE dbo.ImportValueMappings (
	[Id]						[uniqueidentifier]		NOT NULL	DEFAULT (newsequentialid()),
	[ImportColumnMappingId]		[uniqueidentifier]		NOT NULL,
	[CsvValue]					nvarchar(500)			NOT NULL,   -- raw value from CSV (lowercased)
    [SystemValue]				nvarchar(500)			NOT NULL,   -- OptionKey or '0'/'1'
    [CreatedAt]					datetime2				NOT NULL    DEFAULT SYSUTCDATETIME(),

	CONSTRAINT [PK_ImportValueMappings] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [FK_ImportValueMappings_ColumnMapping]
            FOREIGN KEY (ImportColumnMappingId)
            REFERENCES dbo.ImportColumnMappings (Id),

	-- One translation per raw CSV value per column mapping
    CONSTRAINT [UQ_ImportValueMappings_CsvValue] UNIQUE (ImportColumnMappingId, CsvValue)
)

CREATE INDEX [IX_ImportValueMappings_ColumnMapping] ON dbo.ImportValueMappings (ImportColumnMappingId)