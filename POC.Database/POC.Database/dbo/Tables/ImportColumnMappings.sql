-- ============================================================
--  ImportColumnMappings
--
--  One row per CSV column, per import batch.
--  Defines how each CSV header maps to either a Customer
--  table column or a FieldDefinition field.
--
--  MappingType values:
--    customer_field     = maps to a fixed Customers column
--    field_definition   = maps to a FieldDefinitions row
--    skip               = column is ignored during import
--
--  CustomerFieldName is used when MappingType = 'customer_field'
--  Valid values: FirstName, LastName, MiddleName, Email, CustomerCode
--
--  FieldDefinitionId is used when MappingType = 'field_definition'
--  Must reference a FieldDefinition belonging to the same Organization.
--
--  IsAutoMatched flags whether the mapping was set automatically
--  (header matched FieldKey or FieldLabel) or manually by the user.
--
--  SavedForReuse = 1 means this mapping should be persisted and
--  used to auto-match future uploads with the same HeaderFingerprint.
-- ============================================================
CREATE TABLE dbo.ImportColumnMappings (
	[Id]					[uniqueidentifier]		NOT NULL	DEFAULT (newsequentialid()),
	[ImportBatchId]			[uniqueidentifier]		NOT NULL,
	[CsvHeader]				nvarchar(200)			NOT NULL,   -- exact header text from CSV
	[CsvColumnIndex]		int						NOT NULL,   -- zero-based position in CSV
	[MappingType]			nvarchar(20)			NOT NULL    DEFAULT ('skip'),
    [CustomerFieldName]		nvarchar(100)			NULL,       -- e.g. 'FirstName', 'Email'
    [FieldDefinitionId]		uniqueidentifier		NULL,
    [IsAutoMatched]			bit						NOT NULL    DEFAULT 0,
    [IsRequired]			bit						NOT NULL    DEFAULT 0,   -- copied from FieldDefinition
    [SavedForReuse]			bit						NOT NULL    DEFAULT 1,
    [DisplayOrder]			int						NOT NULL    DEFAULT 0,

	CONSTRAINT [PK_ImportColumnMappings] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [FK_ImportColumnMappings_Batch]
            FOREIGN KEY (ImportBatchId)
            REFERENCES dbo.ImportBatches (Id),

	CONSTRAINT [FK_ImportColumnMappings_FieldDefinition]
            FOREIGN KEY (FieldDefinitionId)
            REFERENCES dbo.FieldDefinitions (Id),

	-- One mapping row per CSV/Excel header per batch
	CONSTRAINT [UQ_ImportColumnMappings_Header] UNIQUE (ImportBatchId, CsvHeader),

	CONSTRAINT [CK_ImportColumnMappings_Type] CHECK (
            MappingType IN ('customer_field', 'field_definition', 'skip')
	),

	-- CustomerFieldName must be a known Customers column
    CONSTRAINT [CK_ImportColumnMappings_CustomerField] CHECK (
        CustomerFieldName IS NULL OR CustomerFieldName IN (
            'FirstName', 'LastName', 'MiddleName', 'Email', 'CustomerCode'
        )
    )
)	
GO

CREATE INDEX [IX_ImportColumnMappings_Batch] ON dbo.ImportColumnMappings (ImportBatchId, DisplayOrder)
GO

CREATE INDEX [IX_ImportColumnMappings_FieldDefinition] ON dbo.ImportColumnMappings (FieldDefinitionId)
GO
