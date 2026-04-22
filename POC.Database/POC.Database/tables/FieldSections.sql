CREATE TABLE [dbo].[FieldSections] (
    [Id]             [uniqueidentifier] NOT NULL CONSTRAINT [DF_FieldSections_Id] DEFAULT (newsequentialid()),
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [SectionName]    [nvarchar](150)    NOT NULL,
    [DisplayOrder]   [int]              NOT NULL CONSTRAINT [DF_FieldSections_DisplayOrder] DEFAULT (0),
    [IsActive]       [bit]              NOT NULL CONSTRAINT [DF_FieldSections_IsActive] DEFAULT (1),

    CONSTRAINT [PK_FieldSections] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldSections_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id])
);
