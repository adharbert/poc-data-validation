-- ============================================================
--  MarketingProjects
--
--  Represents a marketing project under an organisation.
--  Multiple projects can be active simultaneously under the
--  same organisation (no single-active constraint).
--
--  Project IDs are INT IDENTITY starting at 8000 so they are
--  visually distinct from GUIDs and other numeric IDs.
--
--  ContractId is optional — a project may exist before a
--  formal contract is in place.
--
--  MarketingEndDate drives the dashboard expiry warning.
--  The DashboardSettings:WarningDaysThreshold in appsettings
--  controls how many days before expiry the warning appears.
-- ============================================================
CREATE TABLE dbo.MarketingProjects (
    [Id]                    int                 NOT NULL    IDENTITY(8000, 1),
    [OrganizationId]        [uniqueidentifier]  NOT NULL,
    [ContractId]            [uniqueidentifier]  NULL,       -- optional FK to Contracts
    [ProjectName]           nvarchar(200)       NOT NULL,
    [MarketingStartDate]    date                NOT NULL,
    [MarketingEndDate]      date                NULL,       -- NULL = ongoing
    [IsActive]              bit                 NOT NULL    DEFAULT (1),
    [Notes]                 nvarchar(1000)      NULL,
    [CreatedDt]             datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]             nvarchar(200)       NOT NULL,
    [ModifiedDt]            datetime            NULL,
    [ModifiedBy]            nvarchar(200)       NULL,

    CONSTRAINT [PK_MarketingProjects] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_MarketingProjects_Organizations]
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_MarketingProjects_Contracts]
        FOREIGN KEY (ContractId)
        REFERENCES dbo.Contracts (Id),

    CONSTRAINT [CK_MarketingProjects_Dates]
        CHECK (MarketingEndDate IS NULL OR MarketingEndDate >= MarketingStartDate)
)
GO

-- Supports the dashboard expiring-projects query
CREATE INDEX [IX_MarketingProjects_EndDate]
    ON dbo.MarketingProjects (MarketingEndDate)
    WHERE MarketingEndDate IS NOT NULL AND IsActive = 1
GO

CREATE INDEX [IX_MarketingProjects_Organization]
    ON dbo.MarketingProjects (OrganizationId, IsActive, MarketingEndDate)
GO
