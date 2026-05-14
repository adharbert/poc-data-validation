-- ============================================================
--  FieldOptionAliases
--
--  Per-organisation value mapping for import.
--  Maps a client-supplied raw value (AliasValue) to the
--  canonical OptionKey stored in FieldOptions.
--
--  Example: OrganisationId=LSU, Field=Gender
--    AliasValue='M'   → CanonicalValue='male'
--    AliasValue='F'   → CanonicalValue='female'
--
--  Applied automatically during import execution for
--  dropdown and multiselect field types.
-- ============================================================
CREATE TABLE dbo.FieldOptionAliases (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_FieldOptionAliases_Id]          DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    UNIQUEIDENTIFIER    NOT NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NOT NULL,
    [AliasValue]        NVARCHAR(200)       NOT NULL,
    [CanonicalValue]    NVARCHAR(200)       NOT NULL,
    [CreatedDt]         DATETIME            NOT NULL    CONSTRAINT [DF_FieldOptionAliases_CreatedDt]   DEFAULT (GETUTCDATE()),
    [ModifiedDt]        DATETIME            NOT NULL    CONSTRAINT [DF_FieldOptionAliases_ModifiedDt]  DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_FieldOptionAliases]       PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldOptionAliases_Org]   FOREIGN KEY ([OrganizationId])    REFERENCES dbo.Organizations ([Id]),
    CONSTRAINT [FK_FieldOptionAliases_Field] FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),
    CONSTRAINT [UQ_FieldOptionAliases]       UNIQUE ([OrganizationId], [FieldDefinitionId], [AliasValue])
)
GO

CREATE INDEX IX_FieldOptionAliases_Org ON dbo.FieldOptionAliases ([OrganizationId], [FieldDefinitionId])
GO
