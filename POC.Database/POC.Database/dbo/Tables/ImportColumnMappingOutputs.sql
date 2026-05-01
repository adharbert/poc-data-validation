-- ============================================================
--  ImportColumnMappingOutputs
--
--  Child rows for split-transform column mappings.
--  Only exists when the parent ImportColumnMappings row has
--  TransformType IN ('split_full_name', 'split_full_address').
--
--  Each row maps one parsed token from the transform to a
--  specific destination field.
--
--  OutputToken examples for split_full_name:
--    'FirstName', 'MiddleName', 'LastName', 'Suffix', 'Credentials'
--
--  OutputToken examples for split_full_address:
--    'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode', 'Country'
--
--  DestinationTable / DestinationField follow the same rules as
--  the parent ImportColumnMappings columns.
--  Set DestinationTable = 'skip' to discard a token (e.g. credentials).
-- ============================================================
CREATE TABLE dbo.ImportColumnMappingOutputs (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_Id]               DEFAULT (NEWSEQUENTIALID()),
    [MappingId]         UNIQUEIDENTIFIER    NOT NULL,
    [OutputToken]       NVARCHAR(50)        NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [SortOrder]         INT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappingOutputs_SortOrder]        DEFAULT (0),

    CONSTRAINT [PK_ImportColumnMappingOutputs] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_ImportColumnMappingOutputs_Mapping]
        FOREIGN KEY ([MappingId]) REFERENCES dbo.ImportColumnMappings ([Id]),

    CONSTRAINT [FK_ImportColumnMappingOutputs_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_ImportColumnMappingOutputs_Token]
        UNIQUE ([MappingId], [OutputToken]),

    CONSTRAINT [CK_ImportColumnMappingOutputs_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    )
)
GO

CREATE INDEX [IX_ImportColumnMappingOutputs_MappingId]
    ON dbo.ImportColumnMappingOutputs ([MappingId], [SortOrder])
GO
