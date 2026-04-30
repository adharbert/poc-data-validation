-- ============================================================
--  FIELD_DEFINITIONS
--  One row per field, per Organization.  The field_type column
--  drives which UI widget to render and which value column
--  to read/write in field_values.
--
--  Supported field_type values:
--    text        -> free-text input      -> value_text
--    number      -> numeric input        -> value_number
--    date        -> date picker          -> value_date
--    datetime    -> date+time picker     -> value_datetime
--    checkbox    -> single checkbox      -> value_boolean
--    dropdown    -> single-select list   -> value_text  (stores the chosen key)
--    multiselect -> multi-select list    -> field_value_selections (junction)
-- ============================================================
CREATE TABLE FieldDefinitions (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[OrganizationId]		[uniqueidentifier]	NOT NULL,
	[FieldSectionId]		[uniqueidentifier]	NULL,		-- optional grouping
	[FieldKey]				nvarchar(100)		NOT NULL,	-- internal key, exp 'hightest_degree'
	[FieldLabel]			nvarchar(200)		NOT NULL,	-- display label, exp 'Highest degree'
	[FieldType]				nvarchar(20)		NOT NULL,	-- see supported values above
	[PlaceHolderText]		nvarchar(200)		NULL,
	[HelpText]				nvarchar(500)		NULL,
	[IsRequired]			bit					NOT NULL	DEFAULT(0),
	[IsActive]				bit					NOT NULL	DEFAULT(1),
	[DisplayOrder]			int					NOT NULL	DEFAULT(0),
	[MinValue]				DECIMAL(18,4)		NULL,		-- for number fields
	[MaxValue]				DECIMAL(18,4)		NULL,		-- for number fields
	[MinLength]				int					NULL,		-- for text fields
	[MaxLength]				int					NULL,		-- for text fields
	[RegExPattern]			nvarchar(500)		NULL,		-- for text fields
	[DisplayFormat]			nvarchar(20)		NULL,		-- for phone fields: '(XXX) XXX-XXXX', 'XXX-XXX-XXXX', 'XXX.XXX.XXXX'
	[CreatedDt]				datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[ModifiedDt]			datetime			NOT NULL	DEFAULT(GETUTCDATE()),	
	
	CONSTRAINT [PK_FieldDefinitions] PRIMARY KEY CLUSTERED (Id),
	
	CONSTRAINT [FK_FieldDefinitions_Organizations] 
		FOREIGN KEY([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id]),
		
	CONSTRAINT [FK_FieldDefinitions_FieldSections] 
		FOREIGN KEY([FieldSectionId])
		REFERENCES [dbo].[FieldSections] ([Id]),

	CONSTRAINT [UQ_FieldDefinitions_Key] UNIQUE ([OrganizationId], [FieldKey]),

	-- make sure only these types are used in this field
	CONSTRAINT [CK_FieldDefinitions_Type] CHECK (
        [FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone')
    )

)
GO

CREATE INDEX IX_field_definitions_client  ON FieldDefinitions ([OrganizationId], [DisplayOrder]);
GO

CREATE INDEX IX_field_definitions_section ON FieldDefinitions ([FieldSectionId]);
GO