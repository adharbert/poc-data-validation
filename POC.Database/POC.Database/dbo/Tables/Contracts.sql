-- ============================================================
--  Contracts
--
--  One row per contract per organisation. Only one contract
--  per organisation may be active at a time, enforced by the
--  filtered unique index UQ_Contracts_ActivePerOrg.
--
--  To update an end date, create a new contract rather than
--  editing the active one. Deactivated contracts are retained
--  for history.
--
--  ContractNumber is an optional external reference (e.g. from
--  a CRM system) and is not required to be unique.
/*
    SEED the data with fake information:
        insert into [Contracts]([OrganizationId] ,[ContractName] ,[ContractNumber] ,[StartDate] ,[EndDate] ,[IsActive] ,[Notes] ,[CreatedDt] ,[CreatedBy] ,[ModifiedDt] ,[ModifiedBy])
        values ('3CFDCADA-ADC0-F011-B692-A0B339B26E42', 'PCI-ADX-Data Validation', 'PCI156481', '2026-01-15', '2027-02-01', 1, NULL, GETUTCDATE(), 'andrew.harbert@publishingconcepts.com', GETUTCDATE(), 'andrew.harbert@publishingconcepts.com')
*/
-- ============================================================
CREATE TABLE dbo.Contracts (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    [uniqueidentifier]  NOT NULL,
    [ContractName]      nvarchar(200)       NOT NULL,
    [ContractNumber]    nvarchar(100)       NULL,       -- optional external reference
    [StartDate]         date                NOT NULL,
    [EndDate]           date                NULL,       -- NULL = open-ended
    [IsActive]          bit                 NOT NULL    DEFAULT (1),
    [Notes]             nvarchar(1000)      NULL,
    [CreatedDt]         datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]         nvarchar(200)       NOT NULL,
    [ModifiedDt]        datetime            NULL,
    [ModifiedBy]        nvarchar(200)       NULL,

    CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_Contracts_Organizations]
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations (Id),

    CONSTRAINT [CK_Contracts_Dates]
        CHECK (EndDate IS NULL OR EndDate >= StartDate)
)
GO

-- Only one active contract per organisation at a time
CREATE UNIQUE INDEX [UQ_Contracts_ActivePerOrg]
    ON dbo.Contracts (OrganizationId)
    WHERE IsActive = 1
GO

CREATE INDEX [IX_Contracts_Organization]
    ON dbo.Contracts (OrganizationId, StartDate DESC)
GO
