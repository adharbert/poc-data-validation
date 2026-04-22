CREATE TABLE [dbo].[FieldValues] (
    [Id]                [uniqueidentifier] NOT NULL CONSTRAINT [DF_FieldValues_Id] DEFAULT (newsequentialid()),
    [CustomerId]        [uniqueidentifier] NOT NULL,
    [FieldDefinitionId] [uniqueidentifier] NOT NULL,
    [ValueText]         [nvarchar](max)    NULL,
    [ValueNumber]       [decimal](10, 4)   NULL,
    [ValueDate]         [date]             NULL,
    [ValueDatetime]     [datetime]         NULL,
    [ValueBoolean]      [bit]              NULL,
    [ConfirmedAt]       [datetime]         NULL,
    [ConfirmedBy]       [nvarchar](200)    NULL,
    [FlaggedAt]         [datetime]         NULL,
    [FlagNote]          [nvarchar](1000)   NULL,
    [CreatedDt]         [datetime]         NOT NULL CONSTRAINT [DF_FieldValues_CreatedDt] DEFAULT (getutcdate()),
    [ModifiedDt]        [datetime]         NOT NULL CONSTRAINT [DF_FieldValues_ModifiedDt] DEFAULT (getutcdate()),

    CONSTRAINT [PK_FieldValues] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FieldValues_Customers]         FOREIGN KEY ([CustomerId])        REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [FK_FieldValues_FieldDefinitions]  FOREIGN KEY ([FieldDefinitionId]) REFERENCES [dbo].[FieldDefinitions] ([Id]),
    CONSTRAINT [UQ_FieldValues_Customer_Field]    UNIQUE ([CustomerId], [FieldDefinitionId])
);
