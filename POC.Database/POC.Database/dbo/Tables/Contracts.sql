-- ============================================================
--  Contracts
--
--  Header record for a contract with an organisation.
--  Only one contract per organisation may be active at a time,
--  enforced by the filtered unique index UQ_Contracts_ActivePerOrg.
--
--  Amendments (ContractAmendments) may extend the end date and/or
--  add cost to an active, non-expired contract.
--    - OriginalEndDate / OriginalCost are set at creation and never change.
--    - EndDate / TotalCost reflect the current effective values after
--      all amendments have been applied.
--    - Amendments are additive for cost: TotalCost = OriginalCost + SUM(AmendmentCost).
--    - Amendments are replacement for end date: EndDate = latest NewEndDate.
--
--  ContractNumber is an optional external reference (e.g. from a CRM system).
/*
    SEED the data with fake information:
        insert into [Contracts]([OrganizationId],[ContractName],[ContractNumber],[StartDate],[OriginalEndDate],[EndDate],[OriginalCost],[TotalCost],[IsActive],[Notes],[CreatedDt],[CreatedBy])
        values ('3CFDCADA-ADC0-F011-B692-A0B339B26E42','PCI-ADX-Data Validation','PCI156481','2026-01-15','2027-02-01','2027-02-01',100000.00,100000.00,1,NULL,GETUTCDATE(),'andrew.harbert@publishingconcepts.com')
*/
-- ============================================================
CREATE TABLE dbo.Contracts (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    [uniqueidentifier]  NOT NULL,
    [ContractName]      nvarchar(200)       NOT NULL,
    [ContractNumber]    nvarchar(100)       NULL,           -- optional external CRM reference
    [StartDate]         date                NOT NULL,
    [OriginalEndDate]   date                NULL,           -- set at creation, never changed
    [EndDate]           date                NULL,           -- current effective end date; updated by amendments
    [OriginalCost]      decimal(18,2)       NULL,           -- base contracted amount; never changed
    [TotalCost]         decimal(18,2)       NULL,           -- OriginalCost + SUM of all amendment costs
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
        CHECK (EndDate IS NULL OR EndDate >= StartDate),

    CONSTRAINT [CK_Contracts_OriginalDates]
        CHECK (OriginalEndDate IS NULL OR OriginalEndDate >= StartDate),

    CONSTRAINT [CK_Contracts_Cost]
        CHECK (TotalCost IS NULL OR TotalCost >= OriginalCost)
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
