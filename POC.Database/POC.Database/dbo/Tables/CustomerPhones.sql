CREATE TABLE [dbo].[CustomerPhones] (
    [Id]            UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [CustomerId]    UNIQUEIDENTIFIER    NOT NULL,
    [PhoneNumber]   NVARCHAR(30)        NOT NULL,
    [PhoneType]     NVARCHAR(20)        NOT NULL    DEFAULT ('mobile'),
    [IsPrimary]     BIT                 NOT NULL    DEFAULT (0),
    [IsActive]      BIT                 NOT NULL    DEFAULT (1),
    [CreatedUtcDt]  DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
    [ModifiedUtcDt] DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),

    CONSTRAINT [PK_CustomerPhones] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_CustomerPhones_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),

    CONSTRAINT [CK_CustomerPhones_PhoneType]
        CHECK ([PhoneType] IN ('mobile', 'home', 'work', 'fax', 'other'))
);
GO

CREATE INDEX [IX_CustomerPhones_CustomerId]
    ON [dbo].[CustomerPhones] ([CustomerId]);
GO
