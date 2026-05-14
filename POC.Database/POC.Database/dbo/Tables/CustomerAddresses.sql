CREATE TABLE [dbo].[CustomerAddresses] (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [CustomerId]        UNIQUEIDENTIFIER    NOT NULL,
    [AddressLine1]      NVARCHAR(200)       NOT NULL,
    [AddressLine2]      NVARCHAR(100)       NULL,
    [City]              NVARCHAR(100)       NOT NULL,
    [State]             CHAR(2)             NOT NULL,
    [PostalCode]        VARCHAR(10)         NOT NULL,
    [Country]           CHAR(2)             NOT NULL    DEFAULT ('US'),
    [AddressType]       NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_CustomerAddresses_AddressType] DEFAULT ('primary'),
    [MelissaValidated]  BIT                 NOT NULL    DEFAULT (0),
    [CustomerConfirmed] BIT                 NOT NULL    DEFAULT (0),
    [IsCurrent]         BIT                 NOT NULL    DEFAULT (1),
    [Latitude]          FLOAT               NULL,
    [Longitude]         FLOAT               NULL,
    -- GeographyPoint (computed) is NOT defined here — SSDT does not support computed columns
    -- in system-versioned temporal tables (SQL71610). The column is created manually via
    -- Migration_009. See scripts/Migrations/Migration_009_CustomerAddresses_GeoFields.sql.
    [CreatedUtcDt]      DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
    [ModifiedUtcDt]     DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
    [ValidFrom]         DATETIME2 (2)       GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_CustomerAddresses_ValidFrom] NOT NULL DEFAULT (GETUTCDATE()),
    [ValidTo]           DATETIME2 (2)       GENERATED ALWAYS AS ROW END   HIDDEN CONSTRAINT [DF_CustomerAddresses_ValidTo]   NOT NULL DEFAULT ('9999-12-31 23:59:59.99'),

    CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY CLUSTERED ([Id]),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),

    CONSTRAINT [FK_CustomerAddresses_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),

    CONSTRAINT [CK_CustomerAddresses_AddressType]
        CHECK ([AddressType] IN ('primary', 'secondary', 'mailing', 'vacation', 'other'))
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[CustomerAddresses_History], DATA_CONSISTENCY_CHECK = ON));
GO

CREATE INDEX [IX_CustomerAddresses_CustomerId_IsCurrent]
    ON [dbo].[CustomerAddresses] ([CustomerId], [IsCurrent]);
GO
