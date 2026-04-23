-- ============================================================
--  FIELD_VALUE_HISTORY
--  Append-only audit trail.  Every time a value changes, the
--  old value is written here before the update.
-- ============================================================

CREATE TABLE FieldValuesHistory (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),
	[FieldValueId]			[uniqueidentifier]	NOT NULL,
	[CustomerId]			[uniqueidentifier]	NOT NULL,
	[FieldDefinitionId]		[uniqueidentifier]	NOT NULL,
	[ValueText]				nvarchar(MAX)		NULL,		-- text, dropdown
	[ValueNumber]			DECIMAL(10,4)		NULL,		-- number
	[ValueDate]				date				NULL,		-- date
	[ValueDatetime]			dateTime			NULL,		-- Datetime
	[ValueBoolean]			bit					NULL,		-- checkbox
	[ChangeBy]				nvarchar(200)		NULL,
	[ChangeAt]				datetime			NOT NULL	DEFAULT(GETUTCDATE()),
	[ChangeReason]			nvarchar(500)		NULL,

	CONSTRAINT [PK_FieldValuesHistory] PRIMARY KEY CLUSTERED (Id),
)

CREATE INDEX IX_FieldValuesHistory_Value    ON FieldValuesHistory ([FieldValueId], [ChangeAt] DESC);
CREATE INDEX IX_FieldValuesHistory_Customer ON FieldValuesHistory ([CustomerId], [ChangeAt] DESC);