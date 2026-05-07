-- ============================================================
--  ContractLineItems
--
--  Detail records for a contract, organised by original terms
--  and each subsequent amendment.
--
--  AmendmentId = NULL  →  line item belongs to the original contract.
--  AmendmentId = <id>  →  line item was introduced by that amendment.
--
--  TotalCost can be stored directly or derived from Quantity * UnitCost.
--  Both are nullable to support flexible line item descriptions that
--  may not have a per-unit breakdown.
-- ============================================================
CREATE TABLE dbo.ContractLineItems (
    [Id]                    [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [ContractId]            [uniqueidentifier]  NOT NULL,
    [AmendmentId]           [uniqueidentifier]  NULL,       -- NULL = original contract line item
    [LineItemDescription]   nvarchar(500)       NOT NULL,
    [Quantity]              decimal(10,2)       NULL,
    [UnitCost]              decimal(18,2)       NULL,
    [TotalCost]             decimal(18,2)       NULL,
    [Notes]                 nvarchar(500)       NULL,
    [DisplayOrder]          int                 NOT NULL    DEFAULT (0),
    [CreatedDt]             datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]             nvarchar(200)       NOT NULL,

    CONSTRAINT [PK_ContractLineItems] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ContractLineItems_Contracts]
        FOREIGN KEY (ContractId)
        REFERENCES dbo.Contracts (Id),

    CONSTRAINT [FK_ContractLineItems_ContractAmendments]
        FOREIGN KEY (AmendmentId)
        REFERENCES dbo.ContractAmendments (Id)
)
GO

CREATE INDEX [IX_ContractLineItems_Contract]
    ON dbo.ContractLineItems (ContractId, AmendmentId, DisplayOrder)
GO
