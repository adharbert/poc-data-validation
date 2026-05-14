-- ============================================================
--  LIBRARY_FIELD_OPTIONS
--  Dropdown / multiselect choices for library fields.
--  Mirrors FieldOptions but references LibraryFields instead
--  of FieldDefinitions.
-- ============================================================
CREATE TABLE LibraryFieldOptions (
	[Id]			[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[LibraryFieldId]	[uniqueidentifier]	NOT NULL,
	[OptionKey]		nvarchar(100)		NOT NULL,	-- stored value, exp 'bachelor'
	[OptionLabel]	nvarchar(200)		NOT NULL,	-- displayed text, exp "Bachelor's Degree"
	[DisplayOrder]	int					NOT NULL	DEFAULT (0),
	[IsActive]		bit					NOT NULL	DEFAULT (1),

	CONSTRAINT [PK_LibraryFieldOptions] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [UQ_LibraryFieldOptions_Key] UNIQUE ([LibraryFieldId], [OptionKey]),

	CONSTRAINT [FK_LibraryFieldOptions_Field]
		FOREIGN KEY ([LibraryFieldId])
		REFERENCES [dbo].[LibraryFields] ([Id])
)
GO

CREATE INDEX IX_LibraryFieldOptions_Field ON LibraryFieldOptions ([LibraryFieldId], [DisplayOrder]);
GO
