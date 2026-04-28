-- ============================================================
--  ImportColumnStaging
--
--  Persistent record of CSV/Excel column headers that could
--  not be auto-matched to a FieldDefinition or Customer column
--  during an import upload.
--
--  One row per unique header per organisation (UQ on
--  OrganizationId + HeaderNormalized). Duplicate uploads with
--  the same header increment SeenCount and update LastSeenAt
--  rather than inserting a new row.
--
--  Status values:
--    unmatched  = no mapping assigned yet
--    resolved   = admin has assigned a mapping (MappingType set)
--    skipped    = admin has marked this column as not needed
--
--  When Status = 'resolved', the resolved mapping is applied
--  automatically as a suggestion on the next upload for this
--  organisation that contains the same header.
-- ============================================================
CREATE TABLE dbo.ImportColumnStaging (
    [Id]                    [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]        [uniqueidentifier]  NOT NULL,
    [CsvHeader]             nvarchar(200)       NOT NULL,   -- exact header text from file
    [HeaderNormalized]      nvarchar(200)       NOT NULL,   -- lowercased + trimmed for matching
    [Status]                nvarchar(20)        NOT NULL    DEFAULT ('unmatched'),
    [MappingType]           nvarchar(20)        NULL,       -- 'customer_field' or 'field_definition'
    [CustomerFieldName]     nvarchar(100)       NULL,
    [FieldDefinitionId]     [uniqueidentifier]  NULL,
    [FirstSeenAt]           datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [LastSeenAt]            datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [SeenCount]             int                 NOT NULL    DEFAULT (1),
    [ResolvedAt]            datetime2           NULL,
    [ResolvedBy]            nvarchar(200)       NULL,
    [Notes]                 nvarchar(500)       NULL,

    CONSTRAINT [PK_ImportColumnStaging] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ImportColumnStaging_Organizations]
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_ImportColumnStaging_FieldDefinitions]
        FOREIGN KEY (FieldDefinitionId)
        REFERENCES dbo.FieldDefinitions (Id),

    CONSTRAINT [CK_ImportColumnStaging_Status]
        CHECK (Status IN ('unmatched', 'resolved', 'skipped')),

    -- One staging record per unique header per organisation
    CONSTRAINT [UQ_ImportColumnStaging_OrgHeader]
        UNIQUE (OrganizationId, HeaderNormalized)
)
GO

CREATE INDEX [IX_ImportColumnStaging_OrgStatus]
    ON dbo.ImportColumnStaging (OrganizationId, Status)
GO
