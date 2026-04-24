-- ============================================================
--  ImportBatches
--
--  One row per CSV file upload attempt.
--  Tracks the lifecycle of an import from upload through
--  mapping, preview, execution, and completion.
--
--  FileHeaders stores the raw CSV headers as a JSON array
--  e.g. ["FirstName","LastName","DOB","DegreeLevel"]
--  so the mapping UI can reload them without re-parsing the file.
--
--  HeaderFingerprint is a deterministic hash of the sorted
--  header list — used to find saved mappings for auto-match
--  when the same file format is uploaded again.
--
--		 Status progression:
--		   pending    = uploaded, not yet mapped
--		   mapping    = admin is assigning column mappings
--		   preview    = mapping saved, previewing data
--		   importing  = execution in progress
--		   completed  = all rows processed
--		   failed     = execution failed (check ImportErrors)
--		   cancelled  = admin abandoned the import
-- ============================================================

CREATE TABLE dbo.ImportBatches (
	[Id]					[uniqueidentifier]	NOT NULL	DEFAULT (newsequentialid()),	
	[OrganizationId]		[uniqueidentifier]  NOT NULL,
    [FileName]				nvarchar(260)       NOT NULL,
    [FileHeaders]			nvarchar(MAX)       NOT NULL,   -- JSON array of CSV header strings
    [HeaderFingerprint]		nvarchar(64)        NOT NULL,   -- SHA-256 of sorted headers (hex)
    [TotalRows]				int                 NOT NULL    DEFAULT 0,
    [ImportedRows]			int                 NOT NULL    DEFAULT 0,
    [SkippedRows]			int                 NOT NULL    DEFAULT 0,
    [ErrorRows]				int                 NOT NULL    DEFAULT 0,
	[Status]				nvarchar(20)		NOT NULL	DEFAULT('pending'),
	[UploadedBy]			nvarchar(200)		NOT NULL,
    [UploadedAt]			DATETIME2			NOT NULL    DEFAULT SYSUTCDATETIME(),
    [MappingSavedAt]		DATETIME2			NULL,
    [ExecutionStartedAt]	DATETIME2			NULL,
    [CompletedAt]			DATETIME2			NULL,
    [Notes]					nvarchar(1000)		NULL,       -- optional admin notes

	CONSTRAINT [PK_ImportBatches] PRIMARY KEY CLUSTERED (Id),
		
	CONSTRAINT [FK_ImportBatches_Organizations] 
		FOREIGN KEY([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id]),

	CONSTRAINT [CK_ImportBatches_Status] CHECK (
        [Status] IN ('pending','mapping','preview','importing','completed','failed','cancelled')
    )
)
GO

CREATE INDEX [IX_ImportBatches_Organization] ON dbo.ImportBatches (OrganizationId, UploadedAt DESC)
GO

CREATE INDEX [IX_ImportBatches_Fingerprint] ON dbo.ImportBatches (OrganizationId, HeaderFingerprint)
GO

