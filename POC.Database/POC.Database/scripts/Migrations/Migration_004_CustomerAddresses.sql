-- ============================================================
--  Migration 004 — CustomerAddresses
--
--  Stores a full address history per customer.
--  IsCurrent = 1 marks the customer's active address.
--  When a customer moves, the old row stays (IsCurrent flips to 0)
--  and a new row is inserted (IsCurrent = 1).
--
--  MelissaValidated  — set by the API after calling the Melissa
--                      address-verification service.
--  CustomerConfirmed — set by the customer in the validation portal
--                      when they confirm the address is correct.
--
--  The table is system-versioned (same pattern as Customers) so
--  every column change is tracked automatically in the history table.
-- ============================================================
SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_NAME = 'CustomerAddresses'
    )
    BEGIN
        RAISERROR('CustomerAddresses already exists. Migration 004 aborted.', 16, 1);
        RETURN;
    END

    -- Main table
    CREATE TABLE [dbo].[CustomerAddresses] (
        [Id]                UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
        [CustomerId]        UNIQUEIDENTIFIER    NOT NULL,
        [AddressLine1]      NVARCHAR(200)       NOT NULL,
        [AddressLine2]      NVARCHAR(100)       NULL,
        [City]              NVARCHAR(100)       NOT NULL,
        [State]             CHAR(2)             NOT NULL,
        [PostalCode]        VARCHAR(10)         NOT NULL,
        [Country]           CHAR(2)             NOT NULL    DEFAULT ('US'),
        [MelissaValidated]  BIT                 NOT NULL    DEFAULT (0),
        [CustomerConfirmed] BIT                 NOT NULL    DEFAULT (0),
        [IsCurrent]         BIT                 NOT NULL    DEFAULT (1),
        [CreatedUtcDt]      DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
        [ModifiedUtcDt]     DATETIME2           NOT NULL    DEFAULT (GETUTCDATE()),
        [ValidFrom]         DATETIME2 (2)       GENERATED ALWAYS AS ROW START HIDDEN
                                CONSTRAINT [DF_CustomerAddresses_ValidFrom] NOT NULL DEFAULT (GETUTCDATE()),
        [ValidTo]           DATETIME2 (2)       GENERATED ALWAYS AS ROW END   HIDDEN
                                CONSTRAINT [DF_CustomerAddresses_ValidTo]   NOT NULL DEFAULT ('9999-12-31 23:59:59.99'),

        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY CLUSTERED ([Id]),
        PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),

        CONSTRAINT [FK_CustomerAddresses_Customers]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[CustomerAddresses_History], DATA_CONSISTENCY_CHECK = ON));

    PRINT 'CustomerAddresses table created.';

    -- Partial index — only one current address per customer enforced at the app layer,
    -- but this index makes the IsCurrent=1 lookup fast.
    CREATE INDEX [IX_CustomerAddresses_CustomerId_IsCurrent]
        ON [dbo].[CustomerAddresses] ([CustomerId], [IsCurrent]);

    PRINT 'Index IX_CustomerAddresses_CustomerId_IsCurrent created.';

    COMMIT TRANSACTION;
    PRINT 'Migration 004 completed successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Migration 004 FAILED — transaction rolled back.';
    THROW;
END CATCH;
