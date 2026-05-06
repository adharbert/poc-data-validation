-- ============================================================
--  Isolated database full schema
--  Deployed by DbUp when a new isolated client database is
--  provisioned. Contains the complete schema for one org.
-- ============================================================
SET NOCOUNT ON;
GO

-- -------------------------------------------------------
-- Organizations
-- -------------------------------------------------------
CREATE TABLE [dbo].[Organizations](
    [Id]                        [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [Name]                      [nvarchar](200)     NOT NULL,
    [FilingName]                [nvarchar](200)     NULL,
    [MarketingName]             [nvarchar](100)     NULL,
    [Abbreviation]              [nvarchar](50)      NULL,
    [OrganizationCode]          [nvarchar](26)      NOT NULL,
    [Website]                   [nvarchar](800)     NULL,
    [Phone]                     [nvarchar](11)      NULL,
    [CompanyEmail]              [nvarchar](255)     NULL,
    [IsActive]                  [bit]               NULL        DEFAULT(1),
    [RequiresIsolatedDatabase]  [bit]               NOT NULL    DEFAULT(1),
    [IsolatedConnectionString]  [nvarchar](500)     NULL,
    [DatabaseProvisioningStatus][nvarchar](20)      NULL,
    [CreateUtcDt]               [datetime]          NULL        DEFAULT (getutcdate()),
    [CreatedBy]                 [nvarchar](50)      NOT NULL,
    [ModifiedUtcDt]             [datetime]          NULL        DEFAULT (getutcdate()),
    [ModifiedBy]                [nvarchar](50)      NULL,
    [ValidFrom]                 DATETIME2 (2)       GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Organizations_ValidFrom] DEFAULT (getutcdate())              NOT NULL,
    [ValidTo]                   DATETIME2 (2)       GENERATED ALWAYS AS ROW END   HIDDEN CONSTRAINT [DF_Organizations_ValidTo]   DEFAULT ('9999.12.31 23:59:59.99') NOT NULL,

    CONSTRAINT [PK_Organizations] PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Organizations_Abbreviation_Length CHECK (LEN(Abbreviation) <= 4),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Organizations_History], DATA_CONSISTENCY_CHECK=ON));
GO

CREATE UNIQUE INDEX IX_OrganizationsCode ON [dbo].[Organizations]([OrganizationCode]);
GO

CREATE UNIQUE INDEX UQ_Organizations_Abbreviation ON dbo.Organizations (Abbreviation) WHERE Abbreviation IS NOT NULL;
GO


-- -------------------------------------------------------
-- Customers
-- -------------------------------------------------------
CREATE TABLE [dbo].[Customers] (
    [Id]            [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [OriginalId]    nvarchar(12)        NULL,
    [FirstName]     nvarchar(30)        NOT NULL,
    [LastName]      nvarchar(50)        NOT NULL,
    [MiddleName]    nvarchar(30)        NULL,
    [MaidenName]    nvarchar(50)        NULL,
    [DateOfBirth]   date                NULL,
    [CustomerCode]  [nvarchar](26)      NOT NULL,
    [OrganizationId][uniqueidentifier]  NOT NULL,
    [Email]         nvarchar(150)       NULL,
    [Phone]         nvarchar(11)        NULL,
    [IsActive]      bit                 NULL        DEFAULT(1),
    [CreatedDt]     datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [ModifiedDt]    datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [ValidFrom]     DATETIME2 (2)       GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_Customers_ValidFrom] NOT NULL DEFAULT (getutcdate()),
    [ValidTo]       DATETIME2 (2)       GENERATED ALWAYS AS ROW END   HIDDEN CONSTRAINT [DF_Customers_ValidTo]   NOT NULL DEFAULT ('9999.12.31 23:59:59.99'),

    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED (Id),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),

    CONSTRAINT [FK_Customers_Organizations]
        FOREIGN KEY([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Customers_History], DATA_CONSISTENCY_CHECK=ON));
GO

CREATE UNIQUE INDEX IX_CustomerCode ON [dbo].[Customers]([CustomerCode]);
GO


-- -------------------------------------------------------
-- CustomerAddresses
-- -------------------------------------------------------
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

CREATE INDEX [IX_CustomerAddresses_CustomerId_IsCurrent] ON [dbo].[CustomerAddresses] ([CustomerId], [IsCurrent]);
GO


-- -------------------------------------------------------
-- CustomerPhones
-- -------------------------------------------------------
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

CREATE INDEX [IX_CustomerPhones_CustomerId] ON [dbo].[CustomerPhones] ([CustomerId]);
GO


-- -------------------------------------------------------
-- CustomerEmails
-- -------------------------------------------------------
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

CREATE INDEX [IX_CustomerEmails_CustomerId] ON [dbo].[CustomerEmails] ([CustomerId]);
GO


-- -------------------------------------------------------
-- FieldSections
-- -------------------------------------------------------
CREATE TABLE FieldSections (
    [Id]            [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [OrganizationId][uniqueidentifier]  NOT NULL,
    [SectionName]   nvarchar(150)       NOT NULL,
    [DisplayOrder]  int                 NOT NULL    DEFAULT(0),
    [IsActive]      bit                 NOT NULL    DEFAULT(1),

    CONSTRAINT [PK_FieldSections] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_FieldSections_Organizations]
        FOREIGN KEY([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id])
);
GO

CREATE INDEX IX_FieldSectionsOrganization ON FieldSections (OrganizationId);
GO


-- -------------------------------------------------------
-- FieldDefinitions
-- -------------------------------------------------------
CREATE TABLE FieldDefinitions (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [OrganizationId]    [uniqueidentifier]  NOT NULL,
    [FieldSectionId]    [uniqueidentifier]  NULL,
    [FieldKey]          nvarchar(100)       NOT NULL,
    [FieldLabel]        nvarchar(200)       NOT NULL,
    [FieldType]         nvarchar(20)        NOT NULL,
    [PlaceHolderText]   nvarchar(200)       NULL,
    [HelpText]          nvarchar(500)       NULL,
    [IsRequired]        bit                 NOT NULL    DEFAULT(0),
    [IsActive]          bit                 NOT NULL    DEFAULT(1),
    [DisplayOrder]      int                 NOT NULL    DEFAULT(0),
    [MinValue]          DECIMAL(18,4)       NULL,
    [MaxValue]          DECIMAL(18,4)       NULL,
    [MinLength]         int                 NULL,
    [MaxLength]         int                 NULL,
    [RegExPattern]      nvarchar(500)       NULL,
    [DisplayFormat]     nvarchar(20)        NULL,
    [CreatedDt]         datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [ModifiedDt]        datetime            NOT NULL    DEFAULT(GETUTCDATE()),

    CONSTRAINT [PK_FieldDefinitions] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_FieldDefinitions_Organizations]
        FOREIGN KEY([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id]),

    CONSTRAINT [FK_FieldDefinitions_FieldSections]
        FOREIGN KEY([FieldSectionId]) REFERENCES [dbo].[FieldSections] ([Id]),

    CONSTRAINT [UQ_FieldDefinitions_Key] UNIQUE ([OrganizationId], [FieldKey]),

    CONSTRAINT [CK_FieldDefinitions_Type] CHECK (
        [FieldType] IN ('text','number','date','datetime','checkbox','dropdown','multiselect','phone')
    )
);
GO

CREATE INDEX IX_field_definitions_client  ON FieldDefinitions ([OrganizationId], [DisplayOrder]);
GO

CREATE INDEX IX_field_definitions_section ON FieldDefinitions ([FieldSectionId]);
GO


-- -------------------------------------------------------
-- FieldOptions
-- -------------------------------------------------------
CREATE TABLE FieldOptions (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [FieldDefinitionId] [uniqueidentifier]  NOT NULL,
    [OptionKey]         nvarchar(100)       NOT NULL,
    [OptionLabel]       nvarchar(200)       NOT NULL,
    [DisplayOrder]      int                 NOT NULL    DEFAULT(0),
    [IsActive]          bit                 NOT NULL    DEFAULT(1),

    CONSTRAINT [PK_FieldOptions] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_FieldOptions_FieldDefinitions]
        FOREIGN KEY([FieldDefinitionId]) REFERENCES [dbo].[FieldDefinitions] ([Id]),

    CONSTRAINT [UQ_FieldOptions_Key] UNIQUE ([FieldDefinitionId], [OptionKey])
);
GO

CREATE INDEX IX_FieldOptions_Field ON FieldOptions ([FieldDefinitionId], [DisplayOrder]);
GO


-- -------------------------------------------------------
-- FieldValues
-- -------------------------------------------------------
CREATE TABLE FieldValues (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [CustomerId]        [uniqueidentifier]  NOT NULL,
    [FieldDefinitionId] [uniqueidentifier]  NOT NULL,
    [ValueText]         nvarchar(MAX)       NULL,
    [ValueNumber]       DECIMAL(10,4)       NULL,
    [ValueDate]         date                NULL,
    [ValueDatetime]     dateTime            NULL,
    [ValueBoolean]      bit                 NULL,
    [ConfirmedAt]       Datetime            NULL,
    [ConfirmedBy]       nvarchar(200)       NULL,
    [FlaggedAt]         Datetime            NULL,
    [FlagNote]          nvarchar(1000)      NULL,
    [CreatedDt]         datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [ModifiedDt]        datetime            NOT NULL    DEFAULT(GETUTCDATE()),

    CONSTRAINT [PK_FieldValues] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_FieldValues_Customers]
        FOREIGN KEY([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),

    CONSTRAINT [FK_FieldValues_FieldDefinitions]
        FOREIGN KEY([FieldDefinitionId]) REFERENCES [dbo].[FieldDefinitions] ([Id]),

    CONSTRAINT [UQ_FieldValues_Customer_Field] UNIQUE ([CustomerId], [FieldDefinitionId])
);
GO

CREATE INDEX IX_FieldValues_Customer ON FieldValues ([CustomerId]);
GO

CREATE INDEX IX_FieldValues_Field ON FieldValues ([FieldDefinitionId]);
GO


-- -------------------------------------------------------
-- FieldValuesHistory
-- -------------------------------------------------------
CREATE TABLE FieldValuesHistory (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [FieldValueId]      [uniqueidentifier]  NOT NULL,
    [CustomerId]        [uniqueidentifier]  NOT NULL,
    [FieldDefinitionId] [uniqueidentifier]  NOT NULL,
    [ValueText]         nvarchar(MAX)       NULL,
    [ValueNumber]       DECIMAL(10,4)       NULL,
    [ValueDate]         date                NULL,
    [ValueDatetime]     dateTime            NULL,
    [ValueBoolean]      bit                 NULL,
    [ChangeBy]          nvarchar(200)       NULL,
    [ChangeAt]          datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [ChangeReason]      nvarchar(500)       NULL,

    CONSTRAINT [PK_FieldValuesHistory] PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE INDEX IX_FieldValuesHistory_Value    ON FieldValuesHistory ([FieldValueId], [ChangeAt] DESC);
GO

CREATE INDEX IX_FieldValuesHistory_Customer ON FieldValuesHistory ([CustomerId], [ChangeAt] DESC);
GO


-- -------------------------------------------------------
-- FieldValueSelections (multiselect junction)
-- -------------------------------------------------------
CREATE TABLE FieldValueSelections (
    [Id]            [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [FieldValueId]  [uniqueidentifier]  NOT NULL,
    [FieldOptionId] [uniqueidentifier]  NOT NULL,

    CONSTRAINT [PK_FieldValueSelections] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_FieldValueSelections_FieldValues]
        FOREIGN KEY([FieldValueId]) REFERENCES [dbo].[FieldValues] ([Id]),

    CONSTRAINT [FK_FieldValueSelections_FieldOptions]
        FOREIGN KEY([FieldOptionId]) REFERENCES [dbo].[FieldOptions] ([Id]),

    CONSTRAINT [UQ_FieldValueSelections_Values_Options] UNIQUE ([FieldValueId], [FieldOptionId])
);
GO

CREATE INDEX IX_FieldValueSelections_FieldValue  ON FieldValueSelections ([FieldValueId]);
GO

CREATE INDEX IX_FieldValueSelections_FieldOption ON FieldValueSelections ([FieldOptionId]);
GO


-- -------------------------------------------------------
-- ValidationSessions
-- -------------------------------------------------------
CREATE TABLE ValidationSessions (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [CustomerId]        [uniqueidentifier]  NOT NULL,
    [StartedAt]         datetime            NOT NULL    DEFAULT(GETUTCDATE()),
    [CompletedAt]       datetime            NULL,
    [Status]            nvarchar(30)        NOT NULL    DEFAULT('in progress'),
    [TotalFields]       int                 NULL,
    [ConfirmedFields]   int                 NULL        DEFAULT(0),
    [FlaggedFields]     int                 NULL        DEFAULT(0),
    [IpAddress]         nvarchar(45)        NULL,
    [UserAgent]         nvarchar(500)       NULL,

    CONSTRAINT [PK_ValidationSessions] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ValidationSessions_Customers]
        FOREIGN KEY([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),

    CONSTRAINT [CK_ValidationSessions_Status] CHECK (
        [Status] IN ('in progress','completed','abandoned')
    )
);
GO

CREATE INDEX IX_ValidationSessions_Customer ON ValidationSessions ([CustomerId], [StartedAt] DESC);
GO


-- -------------------------------------------------------
-- Contracts
-- -------------------------------------------------------
CREATE TABLE dbo.Contracts (
    [Id]            [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId][uniqueidentifier]  NOT NULL,
    [ContractName]  nvarchar(200)       NOT NULL,
    [ContractNumber]nvarchar(100)       NULL,
    [StartDate]     date                NOT NULL,
    [EndDate]       date                NULL,
    [IsActive]      bit                 NOT NULL    DEFAULT (1),
    [Notes]         nvarchar(1000)      NULL,
    [CreatedDt]     datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]     nvarchar(200)       NOT NULL,
    [ModifiedDt]    datetime            NULL,
    [ModifiedBy]    nvarchar(200)       NULL,

    CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_Contracts_Organizations]
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations (Id),

    CONSTRAINT [CK_Contracts_Dates]
        CHECK (EndDate IS NULL OR EndDate >= StartDate)
);
GO

CREATE UNIQUE INDEX [UQ_Contracts_ActivePerOrg] ON dbo.Contracts (OrganizationId) WHERE IsActive = 1;
GO

CREATE INDEX [IX_Contracts_Organization] ON dbo.Contracts (OrganizationId, StartDate DESC);
GO


-- -------------------------------------------------------
-- MarketingProjects
-- -------------------------------------------------------
CREATE TABLE dbo.MarketingProjects (
    [Id]                    int                 NOT NULL    IDENTITY(8000, 1),
    [OrganizationId]        [uniqueidentifier]  NOT NULL,
    [ContractId]            [uniqueidentifier]  NULL,
    [ProjectName]           nvarchar(200)       NOT NULL,
    [MarketingStartDate]    date                NOT NULL,
    [MarketingEndDate]      date                NULL,
    [IsActive]              bit                 NOT NULL    DEFAULT (1),
    [Notes]                 nvarchar(1000)      NULL,
    [CreatedDt]             datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]             nvarchar(200)       NOT NULL,
    [ModifiedDt]            datetime            NULL,
    [ModifiedBy]            nvarchar(200)       NULL,

    CONSTRAINT [PK_MarketingProjects] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_MarketingProjects_Organizations]
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_MarketingProjects_Contracts]
        FOREIGN KEY (ContractId) REFERENCES dbo.Contracts (Id),

    CONSTRAINT [CK_MarketingProjects_Dates]
        CHECK (MarketingEndDate IS NULL OR MarketingEndDate >= MarketingStartDate)
);
GO

CREATE INDEX [IX_MarketingProjects_EndDate] ON dbo.MarketingProjects (MarketingEndDate)
    WHERE MarketingEndDate IS NOT NULL AND IsActive = 1;
GO

CREATE INDEX [IX_MarketingProjects_Organization] ON dbo.MarketingProjects (OrganizationId, IsActive, MarketingEndDate);
GO


-- -------------------------------------------------------
-- ImportBatches
-- -------------------------------------------------------
CREATE TABLE dbo.ImportBatches (
    [Id]                    [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [OrganizationId]        [uniqueidentifier]  NOT NULL,
    [FileName]              nvarchar(260)       NOT NULL,
    [FileHeaders]           nvarchar(MAX)       NOT NULL,
    [HeaderFingerprint]     nvarchar(64)        NOT NULL,
    [TotalRows]             int                 NOT NULL    DEFAULT 0,
    [ImportedRows]          int                 NOT NULL    DEFAULT 0,
    [SkippedRows]           int                 NOT NULL    DEFAULT 0,
    [ErrorRows]             int                 NOT NULL    DEFAULT 0,
    [Status]                nvarchar(20)        NOT NULL    DEFAULT('pending'),
    [UploadedBy]            nvarchar(200)       NOT NULL,
    [UploadedAt]            DATETIME2           NOT NULL    DEFAULT SYSUTCDATETIME(),
    [MappingSavedAt]        DATETIME2           NULL,
    [ExecutionStartedAt]    DATETIME2           NULL,
    [CompletedAt]           DATETIME2           NULL,
    [Notes]                 nvarchar(1000)      NULL,

    CONSTRAINT [PK_ImportBatches] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ImportBatches_Organizations]
        FOREIGN KEY([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id]),

    CONSTRAINT [CK_ImportBatches_Status] CHECK (
        [Status] IN ('pending','mapping','preview','importing','completed','failed','cancelled')
    )
);
GO

CREATE INDEX [IX_ImportBatches_Organization] ON dbo.ImportBatches (OrganizationId, UploadedAt DESC);
GO

CREATE INDEX [IX_ImportBatches_Fingerprint] ON dbo.ImportBatches (OrganizationId, HeaderFingerprint);
GO


-- -------------------------------------------------------
-- ImportColumnMappings
-- -------------------------------------------------------
CREATE TABLE dbo.ImportColumnMappings (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_ImportColumnMappings_Id]               DEFAULT (NEWSEQUENTIALID()),
    [ImportBatchId]     UNIQUEIDENTIFIER    NOT NULL,
    [CsvHeader]         NVARCHAR(200)       NOT NULL,
    [CsvColumnIndex]    INT                 NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_ImportColumnMappings_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [TransformType]     NVARCHAR(30)        NOT NULL    CONSTRAINT [DF_ImportColumnMappings_TransformType]    DEFAULT ('direct'),
    [IsAutoMatched]     BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_IsAutoMatched]    DEFAULT (0),
    [IsRequired]        BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_IsRequired]       DEFAULT (0),
    [SavedForReuse]     BIT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_SavedForReuse]    DEFAULT (1),
    [DisplayOrder]      INT                 NOT NULL    CONSTRAINT [DF_ImportColumnMappings_DisplayOrder]     DEFAULT (0),

    CONSTRAINT [PK_ImportColumnMappings] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_ImportColumnMappings_Batch]
        FOREIGN KEY ([ImportBatchId]) REFERENCES dbo.ImportBatches ([Id]),

    CONSTRAINT [FK_ImportColumnMappings_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_ImportColumnMappings_Header]
        UNIQUE ([ImportBatchId], [CsvHeader]),

    CONSTRAINT [CK_ImportColumnMappings_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    ),

    CONSTRAINT [CK_ImportColumnMappings_TransformType] CHECK (
        TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
    ),

    CONSTRAINT [CK_ImportColumnMappings_DestinationField] CHECK (
        DestinationTable IN ('skip', 'field_value')
        OR TransformType <> 'direct'
        OR (DestinationTable = 'customer' AND DestinationField IN (
            'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
            'Phone', 'Email', 'OriginalId', 'CustomerCode'
        ))
        OR (DestinationTable = 'customer_address' AND DestinationField IN (
            'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
            'Country', 'AddressType', 'Latitude', 'Longitude'
        ))
    )
);
GO

CREATE INDEX [IX_ImportColumnMappings_Batch] ON dbo.ImportColumnMappings ([ImportBatchId], [DisplayOrder]);
GO

CREATE INDEX [IX_ImportColumnMappings_FieldDefinition] ON dbo.ImportColumnMappings ([FieldDefinitionId]);
GO


-- -------------------------------------------------------
-- ImportColumnMappingOutputs
-- -------------------------------------------------------
CREATE TABLE dbo.ImportColumnMappingOutputs (
    [Id]                    UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [ImportColumnMappingId] UNIQUEIDENTIFIER    NOT NULL,
    [OutputToken]           NVARCHAR(50)        NOT NULL,
    [DestinationTable]      NVARCHAR(20)        NOT NULL,
    [DestinationField]      NVARCHAR(100)       NOT NULL,
    [FieldDefinitionId]     UNIQUEIDENTIFIER    NULL,
    [DisplayOrder]          INT                 NOT NULL    DEFAULT (0),

    CONSTRAINT [PK_ImportColumnMappingOutputs] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_ImportColumnMappingOutputs_Mapping]
        FOREIGN KEY ([ImportColumnMappingId]) REFERENCES dbo.ImportColumnMappings ([Id]),

    CONSTRAINT [FK_ImportColumnMappingOutputs_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id])
);
GO

CREATE INDEX [IX_ImportColumnMappingOutputs_Mapping] ON dbo.ImportColumnMappingOutputs ([ImportColumnMappingId]);
GO


-- -------------------------------------------------------
-- ImportValueMappings
-- -------------------------------------------------------
CREATE TABLE dbo.ImportValueMappings (
    [Id]                        [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [ImportColumnMappingId]     [uniqueidentifier]  NOT NULL,
    [CsvValue]                  nvarchar(500)       NOT NULL,
    [SystemValue]               nvarchar(500)       NOT NULL,
    [CreatedAt]                 datetime2           NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_ImportValueMappings] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ImportValueMappings_ColumnMapping]
        FOREIGN KEY (ImportColumnMappingId) REFERENCES dbo.ImportColumnMappings (Id),

    CONSTRAINT [UQ_ImportValueMappings_CsvValue] UNIQUE (ImportColumnMappingId, CsvValue)
);
GO

CREATE INDEX [IX_ImportValueMappings_ColumnMapping] ON dbo.ImportValueMappings (ImportColumnMappingId);
GO


-- -------------------------------------------------------
-- ImportErrors
-- -------------------------------------------------------
CREATE TABLE dbo.ImportErrors (
    [Id]            [uniqueidentifier]  NOT NULL    DEFAULT (newsequentialid()),
    [ImportBatchId] [uniqueidentifier]  NOT NULL,
    [RowNumber]     INT                 NOT NULL,
    [RawData]       nvarchar(MAX)       NOT NULL,
    [ErrorType]     nvarchar(20)        NOT NULL    DEFAULT 'validation',
    [ErrorMessage]  nvarchar(2000)      NOT NULL,
    [CreatedAt]     datetime2           NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_ImportErrors] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ImportErrors_Batch]
        FOREIGN KEY (ImportBatchId) REFERENCES dbo.ImportBatches (Id),

    CONSTRAINT [CK_ImportErrors_Type] CHECK (
        ErrorType IN ('validation', 'duplicate', 'system')
    )
);
GO

CREATE INDEX [IX_ImportErrors_Batch] ON dbo.ImportErrors (ImportBatchId, RowNumber);
GO


-- -------------------------------------------------------
-- ImportColumnStaging
-- -------------------------------------------------------
CREATE TABLE dbo.ImportColumnStaging (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    [uniqueidentifier]  NOT NULL,
    [CsvHeader]         nvarchar(200)       NOT NULL,
    [HeaderNormalized]  nvarchar(200)       NOT NULL,
    [Status]            nvarchar(20)        NOT NULL    DEFAULT ('unmatched'),
    [MappingType]       nvarchar(20)        NULL,
    [CustomerFieldName] nvarchar(100)       NULL,
    [FieldDefinitionId] [uniqueidentifier]  NULL,
    [FirstSeenAt]       datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [LastSeenAt]        datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [SeenCount]         int                 NOT NULL    DEFAULT (1),
    [ResolvedAt]        datetime2           NULL,
    [ResolvedBy]        nvarchar(200)       NULL,
    [Notes]             nvarchar(500)       NULL,

    CONSTRAINT [PK_ImportColumnStaging] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ImportColumnStaging_Organizations]
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_ImportColumnStaging_FieldDefinitions]
        FOREIGN KEY (FieldDefinitionId) REFERENCES dbo.FieldDefinitions (Id),

    CONSTRAINT [CK_ImportColumnStaging_Status]
        CHECK (Status IN ('unmatched', 'resolved', 'skipped')),

    CONSTRAINT [UQ_ImportColumnStaging_OrgHeader]
        UNIQUE (OrganizationId, HeaderNormalized)
);
GO

CREATE INDEX [IX_ImportColumnStaging_OrgStatus] ON dbo.ImportColumnStaging (OrganizationId, Status);
GO


-- -------------------------------------------------------
-- SavedColumnMappings
-- -------------------------------------------------------
CREATE TABLE dbo.SavedColumnMappings (
    [Id]                UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_SavedColumnMappings_Id]               DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    UNIQUEIDENTIFIER    NOT NULL,
    [HeaderFingerprint] NVARCHAR(64)        NOT NULL,
    [CsvHeader]         NVARCHAR(200)       NOT NULL,
    [CsvColumnIndex]    INT                 NOT NULL,
    [DestinationTable]  NVARCHAR(20)        NOT NULL    CONSTRAINT [DF_SavedColumnMappings_DestinationTable] DEFAULT ('skip'),
    [DestinationField]  NVARCHAR(100)       NULL,
    [FieldDefinitionId] UNIQUEIDENTIFIER    NULL,
    [TransformType]     NVARCHAR(30)        NOT NULL    CONSTRAINT [DF_SavedColumnMappings_TransformType]    DEFAULT ('direct'),
    [DisplayOrder]      INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappings_DisplayOrder]     DEFAULT (0),
    [LastUsedAt]        DATETIME2           NOT NULL    CONSTRAINT [DF_SavedColumnMappings_LastUsedAt]       DEFAULT (SYSUTCDATETIME()),
    [UseCount]          INT                 NOT NULL    CONSTRAINT [DF_SavedColumnMappings_UseCount]         DEFAULT (1),

    CONSTRAINT [PK_SavedColumnMappings] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_SavedColumnMappings_Organizations]
        FOREIGN KEY ([OrganizationId]) REFERENCES dbo.Organizations ([Id]),

    CONSTRAINT [FK_SavedColumnMappings_FieldDefinitions]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id]),

    CONSTRAINT [UQ_SavedColumnMappings_OrgFingerprintHeader]
        UNIQUE ([OrganizationId], [HeaderFingerprint], [CsvHeader]),

    CONSTRAINT [CK_SavedColumnMappings_DestinationTable] CHECK (
        DestinationTable IN ('customer', 'customer_address', 'field_value', 'skip')
    ),

    CONSTRAINT [CK_SavedColumnMappings_TransformType] CHECK (
        TransformType IN ('direct', 'split_full_name', 'split_full_address', 'strip_credentials')
    ),

    CONSTRAINT [CK_SavedColumnMappings_DestinationField] CHECK (
        DestinationTable IN ('skip', 'field_value')
        OR TransformType <> 'direct'
        OR (DestinationTable = 'customer' AND DestinationField IN (
            'FirstName', 'LastName', 'MiddleName', 'MaidenName', 'DateOfBirth',
            'Phone', 'Email', 'OriginalId', 'CustomerCode'
        ))
        OR (DestinationTable = 'customer_address' AND DestinationField IN (
            'AddressLine1', 'AddressLine2', 'City', 'State', 'PostalCode',
            'Country', 'AddressType', 'Latitude', 'Longitude'
        ))
    )
);
GO

CREATE INDEX [IX_SavedColumnMappings_Fingerprint] ON dbo.SavedColumnMappings ([OrganizationId], [HeaderFingerprint]);
GO


-- -------------------------------------------------------
-- SavedColumnMappingOutputs
-- -------------------------------------------------------
CREATE TABLE dbo.SavedColumnMappingOutputs (
    [Id]                        UNIQUEIDENTIFIER    NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [SavedColumnMappingId]      UNIQUEIDENTIFIER    NOT NULL,
    [OutputToken]               NVARCHAR(50)        NOT NULL,
    [DestinationTable]          NVARCHAR(20)        NOT NULL,
    [DestinationField]          NVARCHAR(100)       NOT NULL,
    [FieldDefinitionId]         UNIQUEIDENTIFIER    NULL,
    [DisplayOrder]              INT                 NOT NULL    DEFAULT (0),

    CONSTRAINT [PK_SavedColumnMappingOutputs] PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_SavedColumnMappingOutputs_Mapping]
        FOREIGN KEY ([SavedColumnMappingId]) REFERENCES dbo.SavedColumnMappings ([Id]),

    CONSTRAINT [FK_SavedColumnMappingOutputs_FieldDefinition]
        FOREIGN KEY ([FieldDefinitionId]) REFERENCES dbo.FieldDefinitions ([Id])
);
GO

CREATE INDEX [IX_SavedColumnMappingOutputs_Mapping] ON dbo.SavedColumnMappingOutputs ([SavedColumnMappingId]);
GO
