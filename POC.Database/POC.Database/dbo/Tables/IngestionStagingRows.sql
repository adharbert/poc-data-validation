-- ============================================================
--  IngestionStagingRows
--
--  One row per source-file row per IngestionJob. Stores the
--  normalised field values as JSON before they are committed
--  to the customer tables.
--
--  RowJson format:
--  {
--    "customer":    { "FirstName": "...", "LastName": "...", ... },
--    "address":     { "AddressLine1": "...", "City": "...", ... },
--    "fieldValues": [ { "fieldDefinitionId": "...", "value": "..." } ]
--  }
--
--  Status progression:
--    Pending   = normalised, awaiting commit or review
--    Pass      = row confidence meets threshold
--    Flagged   = below threshold or failed validation
--    Rejected  = human reviewer explicitly rejected
--    Committed = successfully written to customer tables
-- ============================================================

CREATE TABLE dbo.IngestionStagingRows (
	[Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
	[IngestionJobId]    [uniqueidentifier]  NOT NULL,
	[RowNumber]         int                 NOT NULL,               -- 1-based, excluding header
	[RowJson]           nvarchar(MAX)       NOT NULL,               -- normalised field values
	[ConfidenceScore]   decimal(5,4)        NULL,                   -- 0.0000–1.0000
	[Status]            nvarchar(50)        NOT NULL    DEFAULT 'Pending',
	[FlagReasons]       nvarchar(MAX)       NULL,                   -- JSON array of reason strings
	[ReviewedBy]        nvarchar(200)       NULL,
	[ReviewedAt]        datetime2           NULL,

	CONSTRAINT [PK_IngestionStagingRows] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [FK_IngestionStagingRows_Jobs]
		FOREIGN KEY ([IngestionJobId])
		REFERENCES [dbo].[IngestionJobs] ([Id]),

	CONSTRAINT [CK_IngestionStagingRows_Status] CHECK (
		[Status] IN ('Pending','Pass','Flagged','Rejected','Committed')
	)
)
GO

CREATE INDEX [IX_IngestionStagingRows_Job_Status]
	ON dbo.IngestionStagingRows (IngestionJobId, Status)
GO
