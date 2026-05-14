-- ============================================================
--  SavedColumnMappingOutputs
--
--  Persists split-transform token assignments alongside
--  SavedColumnMappings so the full mapping — including how
--  parsed tokens are routed — can be replayed on future uploads.
--
--  Mirrors ImportColumnMappingOutputs exactly; parent FK points
--  to SavedColumnMappings instead of ImportColumnMappings.
-- ============================================================
CREATE TABLE dbo.SavedColumnMappingOutputs (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_Id]               DEFAULT (NEWSEQUENTIALID()),
    [MappingId]         UNIQUEIDENTIFIER    NOT NULL,
    [OutputToken]       NVARCHAR(50)        NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [SortOrder]         INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappingOutputs_SortOrder]        DEFAULT (0),

    CONSTRAINT [PK_SavedColumnMappingOutputs] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_SavedColumnMappingOutputs_Mapping]
        FOREIGN KEY ([MappingId]) REFERENCES dbo.SavedColumnMappings ([Id]),

    CONSTRAINT [FK_SavedColumnMappingOutputs_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_SavedColumnMappingOutputs_Token]
        UNIQUE ([MappingId], [OutputToken]),

    CONSTRAINT [CK_SavedColumnMappingOutputs_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    )
)
GO

CREATE INDEX [IX_SavedColumnMappingOutputs_MappingId]
    ON dbo.SavedColumnMappingOutputs ([MappingId], [SortOrder])
GO
