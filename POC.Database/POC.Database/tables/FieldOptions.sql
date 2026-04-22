CREATE TABLE [dbo].[FieldOptions] (
    [Id]                [uniqueidentifier] NOT NULL CONSTRAINT [DF_FieldOptions_Id] DEFAULT (newsequentialid()),
    [FieldDefinitionId] [uniqueidentifier] NOT NULL,
    [OptionKey]         [nvarchar](100)    NOT NULL,
    [OptionLabel]       [nvarchar](200)    NOT NULL,
    [DisplayOrder]      [int]              NOT NULL CONSTRAINT [DF_FieldOptions_DisplayOrder] DEFAULT (0),
    [IsActive]          [bit]              NOT NULL CONSTRAINT [DF_FieldOptions_IsActive] DEFAULT (1),

    CONSTRAINT [PK_FieldOptions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldOptions_FieldDefinitions] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [dbo].[FieldDefinitions] ([Id]),
    CONSTRAINT [UQ_FieldOptions_Key] UNIQUE ([FieldDefinitionId], [OptionKey])
);
