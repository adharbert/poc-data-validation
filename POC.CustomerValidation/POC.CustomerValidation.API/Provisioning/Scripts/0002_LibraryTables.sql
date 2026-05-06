-- ============================================================
--  0002 — Global Field Library tables
--  Library data lives in the main database and is read from
--  there. These tables are created in isolated tenant databases
--  so the schema stays in sync, but they remain empty — all
--  reads go through the main DB connection.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibrarySections'))
BEGIN
    CREATE TABLE [dbo].[LibrarySections] (
        [Id]            [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [SectionName]   [nvarchar](200)      NOT NULL,
        [Description]   [nvarchar](500)      NULL,
        [DisplayOrder]  [int]                NOT NULL    DEFAULT (0),
        [IsActive]      [bit]                NOT NULL    DEFAULT (1),
        [CreatedDt]     [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        [ModifiedDt]    [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_LibrarySections] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibrarySections_Name] UNIQUE ([SectionName])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibraryFields'))
BEGIN
    CREATE TABLE [dbo].[LibraryFields] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [FieldKey]          [nvarchar](100)      NOT NULL,
        [FieldLabel]        [nvarchar](200)      NOT NULL,
        [FieldType]         [nvarchar](20)       NOT NULL,
        [PlaceHolderText]   [nvarchar](200)      NULL,
        [HelpText]          [nvarchar](500)      NULL,
        [IsRequired]        [bit]                NOT NULL    DEFAULT (0),
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        [MinValue]          [decimal](18,4)      NULL,
        [MaxValue]          [decimal](18,4)      NULL,
        [MinLength]         [int]                NULL,
        [MaxLength]         [int]                NULL,
        [RegExPattern]      [nvarchar](500)      NULL,
        [DisplayFormat]     [nvarchar](20)       NULL,
        [IsActive]          [bit]                NOT NULL    DEFAULT (1),
        [CreatedDt]         [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        [ModifiedDt]        [datetime]           NOT NULL    DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_LibraryFields] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibraryFields_Key] UNIQUE ([FieldKey]),
        CONSTRAINT [CK_LibraryFields_Type] CHECK (
            [FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone')
        )
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibrarySectionFields'))
BEGIN
    CREATE TABLE [dbo].[LibrarySectionFields] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [LibrarySectionId]  [uniqueidentifier]   NOT NULL,
        [LibraryFieldId]    [uniqueidentifier]   NOT NULL,
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        CONSTRAINT [PK_LibrarySectionFields] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibrarySectionFields] UNIQUE ([LibrarySectionId], [LibraryFieldId]),
        CONSTRAINT [FK_LibrarySectionFields_Section]
            FOREIGN KEY ([LibrarySectionId]) REFERENCES [dbo].[LibrarySections] ([Id]),
        CONSTRAINT [FK_LibrarySectionFields_Field]
            FOREIGN KEY ([LibraryFieldId]) REFERENCES [dbo].[LibraryFields] ([Id])
    );
    CREATE INDEX [IX_LibrarySectionFields_Section]
        ON [dbo].[LibrarySectionFields] ([LibrarySectionId], [DisplayOrder]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.LibraryFieldOptions'))
BEGIN
    CREATE TABLE [dbo].[LibraryFieldOptions] (
        [Id]                [uniqueidentifier]   NOT NULL    DEFAULT (newsequentialid()),
        [LibraryFieldId]    [uniqueidentifier]   NOT NULL,
        [OptionKey]         [nvarchar](100)      NOT NULL,
        [OptionLabel]       [nvarchar](200)      NOT NULL,
        [DisplayOrder]      [int]                NOT NULL    DEFAULT (0),
        [IsActive]          [bit]                NOT NULL    DEFAULT (1),
        CONSTRAINT [PK_LibraryFieldOptions] PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT [UQ_LibraryFieldOptions_Key] UNIQUE ([LibraryFieldId], [OptionKey]),
        CONSTRAINT [FK_LibraryFieldOptions_Field]
            FOREIGN KEY ([LibraryFieldId]) REFERENCES [dbo].[LibraryFields] ([Id])
    );
    CREATE INDEX [IX_LibraryFieldOptions_Field]
        ON [dbo].[LibraryFieldOptions] ([LibraryFieldId], [DisplayOrder]);
END
GO
