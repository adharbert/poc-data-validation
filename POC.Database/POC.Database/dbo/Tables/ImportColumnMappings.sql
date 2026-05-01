-- ============================================================
--  ImportColumnMappings
--
--  One row per CSV column, per import batch.
--  Defines how each source header maps to a destination field.
--
--  DestinationTable values:
--    customer         = maps to a fixed Customers column
--    customer_address = maps to a CustomerAddresses column
--    field_value      = maps to a FieldDefinitions row (key/value)
--    skip             = column is ignored during import
--
--  TransformType values:
--    direct               = value copied as-is (1:1)
--    split_full_name      = parsed into First/Middle/Last tokens;
--                           outputs defined in ImportColumnMappingOutputs
--    split_full_address   = parsed into street/city/state/zip tokens;
--                           outputs defined in ImportColumnMappingOutputs
--    strip_credentials    = professional suffixes stripped before mapping
--
--  For TransformType = 'direct':
--    DestinationField holds the target column name (customer / customer_address)
--    FieldDefinitionId is used when DestinationTable = 'field_value'
--
--  For split transforms:
--    DestinationField and FieldDefinitionId are NULL on this row;
--    child rows in ImportColumnMappingOutputs define each token destination.
--
--  SavedForReuse = 1 means this mapping persists to SavedColumnMappings
--  when the batch completes, keyed by HeaderFingerprint.
-- ============================================================
CREATE TABLE dbo.ImportColumnMappings (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_ImportColumnMappings_Id]               DEFAULT (NEWSEQUENTIALID()),
    [ImportBatchId]     UNIQUEIDENTIFIER    NOT NULL,
    [CsvHeader]         NVARCHAR(200)       NOT NULL,
    [CsvColumnIndex]    INT                 NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_ImportColumnMappings_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [TransformType]     NVARCHAR(30)        NOT NULL    CONSTRAINT [DF_ImportColumnMappings_TransformType]    DEFAULT ('direct'),
    [IsAutoMatched]     BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_IsAutoMatched]    DEFAULT (0),
    [IsRequired]        BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_IsRequired]       DEFAULT (0),
    [SavedForReuse]     BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_SavedForReuse]    DEFAULT (1),
    [DisplayOrder]      INT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_DisplayOrder]     DEFAULT (0),

    CONSTRAINT [PK_ImportColumnMappings] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_ImportColumnMappings_Batch]
        FOREIGN KEY ([ImportBatchId]) REFERENCES dbo.ImportBatches ([Id]),

    CONSTRAINT [FK_ImportColumnMappings_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_ImportColumnMappings_Header]
        UNIQUE ([ImportBatchId], [CsvHeader]),

    CONSTRAINT [CK_ImportColumnMappings_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    ),

    CONSTRAINT [CK_ImportColumnMappings_TransformType] CHECK (
        TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
    ),

    -- For direct mappings, DestinationField must be a known column for the target table.
    -- Split transforms store outputs in ImportColumnMappingOutputs; DestinationField is NULL.
    CONSTRAINT [CK_ImportColumnMappings_DestinationField] CHECK (
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

CREATE INDEX [IX_ImportColumnMappings_Batch]
    ON dbo.ImportColumnMappings ([ImportBatchId], [DisplayOrder])
GO

CREATE INDEX [IX_ImportColumnMappings_FieldDefinition]
    ON dbo.ImportColumnMappings ([FieldDefinitionId])
GO
