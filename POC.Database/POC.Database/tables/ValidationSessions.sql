CREATE TABLE [dbo].[ValidationSessions] (
    [Id]              [uniqueidentifier] NOT NULL CONSTRAINT [DF_ValidationSessions_Id] DEFAULT (newsequentialid()),
    [CustomerId]      [uniqueidentifier] NOT NULL,
    [StartedAt]       [datetime]         NOT NULL CONSTRAINT [DF_ValidationSessions_StartedAt] DEFAULT (getutcdate()),
    [CompletedAt]     [datetime]         NULL,
    [Status]          [nvarchar](30)     NOT NULL CONSTRAINT [DF_ValidationSessions_Status] DEFAULT ('in progress'),
    [TotalFields]     [int]              NULL,
    [ConfirmedFields] [int]              NULL     CONSTRAINT [DF_ValidationSessions_ConfirmedFields] DEFAULT (0),
    [FlaggedFields]   [int]              NULL     CONSTRAINT [DF_ValidationSessions_FlaggedFields] DEFAULT (0),
    [IpAddress]       [nvarchar](45)     NULL,
    [UserAgent]       [nvarchar](500)    NULL,

    CONSTRAINT [PK_ValidationSessions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ValidationSessions_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
    CONSTRAINT [CK_ValidationSessions_Status] CHECK ([Status] IN ('in progress', 'completed', 'abandoned'))
);
