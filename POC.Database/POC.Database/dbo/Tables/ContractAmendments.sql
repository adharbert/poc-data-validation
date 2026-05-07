-- ============================================================
--  ContractAmendments
--
--  Tracks each amendment applied to a contract. Amendments may
--  only be added when the parent contract IsActive = 1 AND
--  EndDate >= today (enforced at the service layer).
--
--  What an amendment can change:
--    NewEndDate    — replaces the contract's current EndDate.
--                   PreviousEndDate captures the value before change.
--    AmendmentCost — an additive increase to TotalCost.
--                   TotalCost on Contracts = OriginalCost + SUM(AmendmentCost).
--
--  At least one of NewEndDate or AmendmentCost must be supplied
--  (enforced by CK_ContractAmendments_HasChange).
--
--  AmendmentNumber is sequential per contract: 1, 2, 3 …
--  Enforced by UQ_ContractAmendments_Number.
-- ============================================================
CREATE TABLE dbo.ContractAmendments (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [ContractId]        [uniqueidentifier]  NOT NULL,
    [AmendmentNumber]   int                 NOT NULL,       -- sequential per contract, starting at 1
    [AmendmentDate]     date                NOT NULL,       -- date the amendment was signed/executed
    [PreviousEndDate]   date                NULL,           -- snapshot of EndDate before this amendment
    [NewEndDate]        date                NULL,           -- replacement end date; NULL = not changing
    [AmendmentCost]     decimal(18,2)       NULL,           -- additive cost increase; NULL = not changing
    [Notes]             nvarchar(1000)      NULL,
    [CreatedDt]         datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]         nvarchar(200)       NOT NULL,

    CONSTRAINT [PK_ContractAmendments] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ContractAmendments_Contracts]
        FOREIGN KEY (ContractId)
        REFERENCES dbo.Contracts (Id),

    CONSTRAINT [UQ_ContractAmendments_Number]
        UNIQUE (ContractId, AmendmentNumber),

    CONSTRAINT [CK_ContractAmendments_HasChange]
        CHECK (NewEndDate IS NOT NULL OR AmendmentCost IS NOT NULL),

    CONSTRAINT [CK_ContractAmendments_CostPositive]
        CHECK (AmendmentCost IS NULL OR AmendmentCost > 0)
)
GO

CREATE INDEX [IX_ContractAmendments_Contract]
    ON dbo.ContractAmendments (ContractId, AmendmentNumber)
GO
