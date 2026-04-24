-- ============================================================
--  FIELD_VALUE_SELECTIONS
--  Junction table for multiselect fields.
--  Each chosen option is a separate row linked to field_values.
-- ============================================================

CREATE TABLE FieldValueSelections (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[FieldValueId]			[uniqueidentifier]	NOT NULL,
	[FieldOptionId]			[uniqueidentifier]	NOT NULL,

	CONSTRAINT [PK_FieldValueSelections] PRIMARY KEY CLUSTERED (Id),
	
	CONSTRAINT [FK_FieldValueSelections_FieldValues] 
		FOREIGN KEY([FieldValueId])
		REFERENCES [dbo].[FieldValues] ([Id]),
			
	CONSTRAINT [FK_FieldValueSelections_FieldOptions] 
		FOREIGN KEY([FieldOptionId])
		REFERENCES [dbo].[FieldOptions] ([Id]),

	CONSTRAINT [UQ_FieldValueSelections_Values_Options] UNIQUE ([FieldValueId], [FieldOptionId])

)
GO

CREATE INDEX IX_FieldValueSelections_FieldValue  ON FieldValueSelections ([FieldValueId]);
GO

CREATE INDEX IX_FieldValueSelections_FieldOption ON FieldValueSelections ([FieldOptionId]);
GO