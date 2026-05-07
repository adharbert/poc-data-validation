-- ============================================================
--  Segmentations
--
--  Defines segmentation categories for a marketing project.
--  Each project can have many named segments; customers are
--  assigned via CustomerSegmentations (many-to-many).
--
--  SegmentationKey is the normalised machine key (lowercase,
--  no spaces) used for import matching and deduplication.
--
--  Segments can be created two ways:
--    1. Split from an existing imported field — distinct values
--       become segments and customers are assigned automatically.
--    2. Imported from a separate file keyed on customer OriginalId.
-- ============================================================
CREATE TABLE dbo.Segmentations (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [OrganizationId]    [uniqueidentifier]  NOT NULL,
    [ProjectId]         int                 NOT NULL,
    [SegmentationName]  nvarchar(200)       NOT NULL,
    [SegmentationKey]   nvarchar(100)       NOT NULL,   -- normalised, lowercase, no spaces
    [Description]       nvarchar(500)       NULL,
    [IsActive]          bit                 NOT NULL    DEFAULT (1),
    [DisplayOrder]      int                 NOT NULL    DEFAULT (0),
    [CreatedDt]         datetime            NOT NULL    DEFAULT (GETUTCDATE()),
    [CreatedBy]         nvarchar(200)       NOT NULL,
    [ModifiedDt]        datetime            NULL,
    [ModifiedBy]        nvarchar(200)       NULL,

    CONSTRAINT [PK_Segmentations] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_Segmentations_Organizations]
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations (Id),

    CONSTRAINT [FK_Segmentations_MarketingProjects]
        FOREIGN KEY (ProjectId)
        REFERENCES dbo.MarketingProjects (Id),

    CONSTRAINT [UQ_Segmentations_ProjectKey]
        UNIQUE (ProjectId, SegmentationKey)
)
GO

CREATE INDEX [IX_Segmentations_Organization]
    ON dbo.Segmentations (OrganizationId, IsActive)
GO

CREATE INDEX [IX_Segmentations_Project]
    ON dbo.Segmentations (ProjectId, DisplayOrder)
GO
