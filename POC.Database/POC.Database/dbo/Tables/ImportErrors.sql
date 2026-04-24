
-- ============================================================
--  ImportErrors
--
--  One row per CSV data row that failed to import.
--  RawData stores the original CSV row as JSON so the admin
--  can download a corrected file and re-import.
--
--  ErrorType values:
--    validation   = row failed field validation (missing required, bad format)
--    duplicate    = customer already exists (matched by email or CustomerCode)
--    system       = unexpected error during processing
-- ============================================================
CREATE TABLE dbo.ImportErrors (
	[Id]				[uniqueidentifier]		NOT NULL	DEFAULT (newsequentialid()),
	[ImportBatchId]		[uniqueidentifier]		NOT NULL,
    [RowNumber]			INT						NOT NULL,   -- 1-based row number in CSV (excl. header)
    [RawData]			nvarchar(MAX)			NOT NULL,   -- original CSV row as JSON object
    [ErrorType]			nvarchar(20)			NOT NULL    DEFAULT 'validation',
    [ErrorMessage]		nvarchar(2000)			NOT NULL,
    [CreatedAt]			datetime2				NOT NULL    DEFAULT SYSUTCDATETIME(),

	CONSTRAINT [PK_ImportErrors] PRIMARY KEY CLUSTERED (Id),
 
    CONSTRAINT [FK_ImportErrors_Batch]
        FOREIGN KEY (ImportBatchId)
        REFERENCES dbo.ImportBatches (Id),
 
    CONSTRAINT [CK_ImportErrors_Type] CHECK (
        ErrorType IN ('validation', 'duplicate', 'system')
	)
)
GO

CREATE INDEX [IX_ImportErrors_Batch] ON dbo.ImportErrors (ImportBatchId, RowNumber)
GO