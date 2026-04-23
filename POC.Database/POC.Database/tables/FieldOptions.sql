-- ============================================================
--  FIELD_OPTIONS
--  The allowed choices for dropdown and multiselect fields.
--  Stored separately so options can be managed independently.
-- ============================================================

CREATE TABLE FieldOptions (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[FieldDefinitionId]		[uniqueidentifier]	NOT NULL,
	[OptionKey]				nvarchar(100)		NOT NULL,	-- stored value, exp 'bach'
	[OptionLabel]			nvarchar(200)		NOT NULL,	-- displayed text, exp "Bachelor's degree"
	[DisplayOrder]			int					NOT NULL	DEFAULT(0),
	[IsActive]				bit					NOT NULL	DEFAULT(1),

	CONSTRAINT [PK_FieldOptions] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [FK_FieldOptions_FieldDefinitions] 
		FOREIGN KEY([FieldDefinitionId])
		REFERENCES [dbo].[FieldDefinitions] ([Id]),

	CONSTRAINT [UQ_FieldOptions_Key] UNIQUE ([FieldDefinitionId], [OptionKey])
)

CREATE INDEX IX_FieldOptions_Field ON FieldOptions ([FieldDefinitionId], [DisplayOrder]);