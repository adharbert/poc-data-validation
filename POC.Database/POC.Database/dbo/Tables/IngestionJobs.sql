-- ============================================================
--  IngestionJobs
--
--  Tracks the lifecycle of each automated ingestion pipeline
--  run. Created when a file is submitted; updated as the
--  background processor normalises and stages rows.
--
--  Status progression:
--    Pending        = file received, queued for processing
--    Processing     = background processor is running
--    AwaitingReview = staged rows need business-user approval
--    AwaitingETL    = low-confidence; queued for ETL team
--    Committing     = approved rows are being written to
--                     customer tables
--    Complete       = all rows committed
--    Failed         = unrecoverable error (see ErrorMessage)
--
--  Tier routing:
--    Auto   = confidence >= 0.92 + known template -> auto-commit
--    Review = confidence >= 0.75 -> business user reviews
--    ETL    = confidence < 0.75  -> ETL team queue
--
--  MappingJson stores the column-to-field mapping used to
--  normalise rows into IngestionStagingRows. Populated by
--  auto-match logic today; will be replaced/augmented by
--  AiMappingService once the Claude integration is wired up.
-- ============================================================

CREATE TABLE dbo.IngestionJobs (
	[Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
	[OrganizationId]    [uniqueidentifier]  NOT NULL,
	[FileName]          nvarchar(500)       NOT NULL,
	[FileType]          nvarchar(10)        NOT NULL,               -- csv, xlsx, xls
	[FileSizeBytes]     bigint              NOT NULL,
	[FileHash]          nvarchar(64)        NOT NULL,               -- SHA-256 hex; dedup guard
	[FileStoragePath]   nvarchar(500)       NULL,                   -- disk path while processing
	[HeaderFingerprint] nvarchar(64)        NOT NULL,               -- SHA-256 of sorted headers
	[MappingJson]       nvarchar(MAX)       NULL,                   -- JSON array of column mappings
	[UploadedBy]        nvarchar(200)       NOT NULL,
	[UploadedAt]        datetime2           NOT NULL    DEFAULT SYSUTCDATETIME(),
	[Status]            nvarchar(50)        NOT NULL    DEFAULT 'Pending',
	[Tier]              nvarchar(20)        NULL,                   -- Auto | Review | ETL
	[TotalRows]         int                 NULL,
	[PassedRows]        int                 NULL,
	[FlaggedRows]       int                 NULL,
	[FailedRows]        int                 NULL,
	[ErrorMessage]      nvarchar(MAX)       NULL,
	[CompletedAt]       datetime2           NULL,

	CONSTRAINT [PK_IngestionJobs] PRIMARY KEY CLUSTERED (Id),

	CONSTRAINT [FK_IngestionJobs_Organizations]
		FOREIGN KEY ([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id]),

	CONSTRAINT [CK_IngestionJobs_Status] CHECK (
		[Status] IN (
			'Pending','Processing','AwaitingReview','AwaitingETL',
			'Committing','Complete','Failed'
		)
	),

	CONSTRAINT [CK_IngestionJobs_Tier] CHECK (
		[Tier] IS NULL OR [Tier] IN ('Auto','Review','ETL')
	),

	CONSTRAINT [CK_IngestionJobs_FileType] CHECK (
		[FileType] IN ('csv','xlsx','xls')
	)
)
GO

CREATE INDEX [IX_IngestionJobs_Org_Status]
	ON dbo.IngestionJobs (OrganizationId, Status, UploadedAt DESC)
GO

CREATE INDEX [IX_IngestionJobs_Status]
	ON dbo.IngestionJobs (Status)
	WHERE Status IN ('Pending','Committing')     -- narrow index for background processor poll
GO
