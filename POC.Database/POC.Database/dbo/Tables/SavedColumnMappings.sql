-- ============================================================
--  SavedColumnMappings
--
--  Persists successful column-to-field mappings from a
--  completed import, keyed by OrganizationId + HeaderFingerprint.
--
--  HeaderFingerprint is the SHA-256 hex of the sorted, lower-
--  cased header list from the uploaded file. When a future
--  upload for the same organisation produces the same fingerprint,
--  the saved mappings are loaded and pre-applied, skipping the
--  manual mapping step entirely.
--
--  UseCount and LastUsedAt track how often each mapping is reused.
-- ============================================================
CREATE TABLE dbo.SavedColumnMappings (
    [Id]                    [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]        [uniqueidentifier]  NOT NULL,
    [HeaderFingerprint]     nvarchar(64)        NOT NULL,   -- SHA-256 of sorted headers (hex)
    [CsvHeader]             nvarchar(200)       NOT NULL,
    [CsvColumnIndex]        int                 NOT NULL,
    [MappingType]           nvarchar(20)        NOT NULL,
    [CustomerFieldName]     nvarchar(100)       NULL,
    [FieldDefinitionId]     [uniqueidentifier]  NULL,
    [DisplayOrder]          int                 NOT NULL    DEFAULT (0),
    [LastUsedAt]            datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [UseCount]              int                 NOT NULL    DEFAULT (1),

    CONSTRAINT [PK_SavedColumnMappings] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_SavedColumnMappings_Organizations]
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_SavedColumnMappings_FieldDefinitions]
        FOREIGN KEY (FieldDefinitionId)
        REFERENCES dbo.FieldDefinitions (Id),

    CONSTRAINT [UQ_SavedColumnMappings_OrgFingerprintHeader]
        UNIQUE (OrganizationId, HeaderFingerprint, CsvHeader)
)
GO

-- Primary lookup for auto-match on upload
CREATE INDEX [IX_SavedColumnMappings_Fingerprint]
    ON dbo.SavedColumnMappings (OrganizationId, HeaderFingerprint)
GO
