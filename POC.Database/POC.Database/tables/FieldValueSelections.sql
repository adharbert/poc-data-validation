CREATE TABLE [dbo].[FieldValueSelections] (
    [Id]            [uniqueidentifier] NOT NULL CONSTRAINT [DF_FieldValueSelections_Id] DEFAULT (newsequentialid()),
    [FieldValueId]  [uniqueidentifier] NOT NULL,
    [FieldOptionId] [uniqueidentifier] NOT NULL,

    CONSTRAINT [PK_FieldValueSelections] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldValueSelections_FieldValues]  FOREIGN KEY ([FieldValueId])  REFERENCES [dbo].[FieldValues] ([Id]),
    CONSTRAINT [FK_FieldValueSelections_FieldOptions] FOREIGN KEY ([FieldOptionId]) REFERENCES [dbo].[FieldOptions] ([Id]),
    CONSTRAINT [UQ_FieldValueSelections_Values_Options] UNIQUE ([FieldValueId], [FieldOptionId])
);
