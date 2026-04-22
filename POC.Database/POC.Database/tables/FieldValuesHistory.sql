CREATE TABLE [dbo].[FieldValuesHistory] (
    [Id]                [uniqueidentifier] NOT NULL CONSTRAINT [DF_FieldValuesHistory_Id] DEFAULT (newsequentialid()),
    [FieldValueId]      [uniqueidentifier] NOT NULL,
    [CustomerId]        [uniqueidentifier] NOT NULL,
    [FieldDefinitionId] [uniqueidentifier] NOT NULL,
    [ValueText]         [nvarchar](max)    NULL,
    [ValueNumber]       [decimal](10, 4)   NULL,
    [ValueDate]         [date]             NULL,
    [ValueDatetime]     [datetime]         NULL,
    [ValueBoolean]      [bit]              NULL,
    [ChangeBy]          [nvarchar](200)    NULL,
    [ChangeAt]          [datetime]         NOT NULL CONSTRAINT [DF_FieldValuesHistory_ChangeAt] DEFAULT (getutcdate()),
    [ChangeReason]      [nvarchar](500)    NULL,

    CONSTRAINT [PK_FieldValuesHistory] PRIMARY KEY CLUSTERED ([Id])
);
