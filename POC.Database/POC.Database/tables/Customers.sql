CREATE TABLE [dbo].[Customers] (
    [Id]             [uniqueidentifier] NOT NULL CONSTRAINT [DF_Customers_Id] DEFAULT (newsequentialid()),
    [FirstName]      [nvarchar](30)     NOT NULL,
    [LastName]       [nvarchar](50)     NOT NULL,
    [MiddleName]     [nvarchar](30)     NULL,
    [CustomerCode]   [nvarchar](26)     NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [Email]          [nvarchar](150)    NULL,
    [IsActive]       [bit]              NULL CONSTRAINT [DF_Customers_IsActive] DEFAULT (1),
    [CreatedDt]      [datetime]         NOT NULL CONSTRAINT [DF_Customers_CreatedDt] DEFAULT (getutcdate()),
    [ModifiedDt]     [datetime]         NOT NULL CONSTRAINT [DF_Customers_ModifiedDt] DEFAULT (getutcdate()),
    [ValidFrom]      DATETIME2(2)       GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Customers_ValidFrom] DEFAULT (getutcdate()) NOT NULL,
    [ValidTo]        DATETIME2(2)       GENERATED ALWAYS AS ROW END HIDDEN   CONSTRAINT [DF_Customers_ValidTo]   DEFAULT ('9999.12.31 23:59:59.99') NOT NULL,

    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Customers_Organizations] FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id]),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Customers_History], DATA_CONSISTENCY_CHECK = ON));
