-- ============================================================
--  LIBRARY_FIELDS
--  Reusable field templates that can be assigned to library
--  sections.  Mirror of FieldDefinitions minus the organisation
--  and section foreign keys — those are added at import time.
--
--  Supported FieldType values match FieldDefinitions:
--    text, number, date, datetime, checkbox,
--    dropdown, multiselect, phone
-- ============================================================
CREATE TABLE LibraryFields (
	[Id]				[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[FieldKey]			nvarchar(100)		NOT NULL,	-- internal key, exp 'lib_first_name'
	[FieldLabel]		nvarchar(200)		NOT NULL,	-- display label, exp 'First Name'
	[FieldType]			nvarchar(20)		NOT NULL,	-- see supported values above
	[PlaceHolderText]	nvarchar(200)		NULL,
	[HelpText]			nvarchar(500)		NULL,
	[IsRequired]		bit					NOT NULL	DEFAULT (0),
	[DisplayOrder]		int					NOT NULL	DEFAULT (0),
	[MinValue]			DECIMAL(18,4)		NULL,		-- for number fields
	[MaxValue]			DECIMAL(18,4)		NULL,		-- for number fields
	[MinLength]			int					NULL,		-- for text fields
	[MaxLength]			int					NULL,		-- for text fields
	[RegExPattern]		nvarchar(500)		NULL,		-- for text fields
	[DisplayFormat]		nvarchar(20)		NULL,		-- for phone fields
	[IsActive]			bit					NOT NULL	DEFAULT (1),
	[CreatedDt]			datetime			NOT NULL	DEFAULT (GETUTCDATE()),
	[ModifiedDt]		datetime			NOT NULL	DEFAULT (GETUTCDATE()),

	CONSTRAINT [PK_LibraryFields] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [UQ_LibraryFields_Key] UNIQUE ([FieldKey]),

	CONSTRAINT [CK_LibraryFields_Type] CHECK (
		[FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone')
	)
)
GO
