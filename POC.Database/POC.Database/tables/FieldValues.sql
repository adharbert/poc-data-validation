-- ============================================================
--  FIELD_VALUES
--  One row per customer per field.
--  Uses typed columns rather than a single NVARCHAR so values
--  remain queryable (e.g. date range filters, numeric aggregates).
--
--  For multiselect fields, value_text is NULL and selections
--  are stored in field_value_selections below.
--
--  confirmed_at is set when the customer validates the value;
--  NULL means unreviewed.
-- ============================================================

CREATE TABLE FieldValues (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[CustomerId]			[uniqueidentifier]	NOT NULL,
	[FieldDefinitionId]		[uniqueidentifier]	NOT NULL,
	[ValueText]				nvarchar(MAX)		NULL,		-- text, dropdown
	[ValueNumber]			DECIMAL(10,4)		NULL,		-- number
	[ValueDate]				date				NULL,		-- date
	[ValueDatetime]			dateTime			NULL,		-- Datetime
	[ValueBoolean]			bit					NULL,		-- checkbox
	-- Audit / validation
	[ConfirmedAt]			Datetime			NULL,		-- NULL = not yet reviewed by customer
	[ConfirmedBy]			nvarchar(200)		NULL,		-- identity of the person who confirmed
	[FlaggedAt]				Datetime			NULL,		-- set if customer flags a data problem
	[FlagNote]				nvarchar(1000)		NULL,
	[CreatedDt]				datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[ModifiedDt]			datetime			NOT NULL	DEFAULT(GETUTCDATE()),

	CONSTRAINT [PK_FieldValues] PRIMARY KEY CLUSTERED (Id),
	
	CONSTRAINT [FK_FieldValues_Customers] 
		FOREIGN KEY([CustomerId])
		REFERENCES [dbo].[Customers] ([Id]),

	CONSTRAINT [FK_FieldValues_FieldDefinitions] 
		FOREIGN KEY([FieldDefinitionId])
		REFERENCES [dbo].[FieldDefinitions] ([Id]),

	CONSTRAINT [UQ_FieldValues_Customer_Field] UNIQUE ([CustomerId], [FieldDefinitionId])
	
)

CREATE INDEX IX_FieldValues_Customer ON FieldValues ([CustomerId]);
CREATE INDEX IX_FieldValues_Field    ON FieldValues ([FieldDefinitionId]);