CREATE TABLE [dbo].[CustomerEmails] (
    [Id]            UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [CustomerId]    UNIQUEIDENTIFIER    NOT NULL,
    [EmailAddress]  NVARCHAR(320)       NOT NULL,
    [EmailType]     NVARCHAR(20)        NOT NULL    DEFAULT ('personal'),
    [IsPrimary]     BIT                 NOT NULL    DEFAULT (0),
    [IsActive]      BIT                 NOT NULL    DEFAULT (1),
    [CreatedUtcDt]  DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
    [ModifiedUtcDt] DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_CustomerEmails] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_CustomerEmails_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),

    CONSTRAINT [CK_CustomerEmails_EmailType]
        CHECK ([EmailType] IN ('personal', 'work', 'other'))
);
GO

CREATE INDEX [IX_CustomerEmails_CustomerId]
    ON [dbo].[CustomerEmails] ([CustomerId]);
GO
