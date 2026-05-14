-- ============================================================
--  SavedColumnMappings
--
--  Persists successful column mappings from a completed import,
--  keyed by OrganizationId + HeaderFingerprint.
--
--  HeaderFingerprint is the SHA-256 hex of the sorted, lower-
--  cased header list from the uploaded file.  When a future
--  upload for the same organisation produces the same fingerprint,
--  the saved mappings are loaded and pre-applied, skipping the
--  manual mapping step entirely.
--
--  DestinationTable / DestinationField / TransformType mirror the
--  columns on ImportColumnMappings.  Split-transform token
--  assignments are stored in SavedColumnMappingOutputs.
--
--  UseCount and LastUsedAt track how often each mapping is reused.
-- ============================================================
CREATE TABLE dbo.SavedColumnMappings (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_SavedColumnMappings_Id]               DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    UNIQUEIDENTIFIER    NOT NULL,
    [HeaderFingerprint] NVARCHAR(64)        NOT NULL,
    [CsvHeader]         NVARCHAR(200)       NOT NULL,
    [CsvColumnIndex]    INT                 NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_SavedColumnMappings_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [TransformType]     NVARCHAR(30)        NOT NULL    CONSTRAINT [DF_SavedColumnMappings_TransformType]    DEFAULT ('direct'),
    [DisplayOrder]      INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappings_DisplayOrder]     DEFAULT (0),
    [LastUsedAt]        DATETIME2           NOT NULL    CONSTRAINT [DF_SavedColumnMappings_LastUsedAt]       DEFAULT (SYSUTCDATETIME()),
    [UseCount]          INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappings_UseCount]         DEFAULT (1),

    CONSTRAINT [PK_SavedColumnMappings] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_SavedColumnMappings_Organizations]
        FOREIGN KEY ([OrganizationId]) REFERENCES dbo.Organizations ([Id]),

    CONSTRAINT [FK_SavedColumnMappings_FieldDefinitions]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_SavedColumnMappings_OrgFingerprintHeader]
        UNIQUE ([OrganizationId], [HeaderFingerprint], [CsvHeader]),

    CONSTRAINT [CK_SavedColumnMappings_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    ),

    CONSTRAINT [CK_SavedColumnMappings_TransformType] CHECK (
        TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
    ),

    CONSTRAINT [CK_SavedColumnMappings_DestinationField] CHECK (
        DestinationTable IN ('skip', 'field_value')
        OR TransformType <> 'direct'
        OR (DestinationTable = 'customer' AND DestinationField IN (
            'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
            'Phone', 'Email', 'OriginalId', 'CustomerCode'
        ))
        OR (DestinationTable = 'customer_address' AND DestinationField IN (
            'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
            'Country', 'AddressType', 'Latitude', 'Longitude'
        ))
    )
)
GO

CREATE INDEX [IX_SavedColumnMappings_Fingerprint]
    ON dbo.SavedColumnMappings ([OrganizationId], [HeaderFingerprint])
GO
