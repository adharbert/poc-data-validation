-- ============================================================
--  CustomerSegmentations
--
--  Junction table assigning customers to segmentation categories
--  within a project. A customer may belong to many segments.
--
--  Source values:
--    field_split  — created by splitting an existing imported field
--    import_file  — created by loading a separate segmentation file
--                   matched on customer OriginalId
--    manual       — assigned directly through the admin UI
-- ============================================================
CREATE TABLE dbo.CustomerSegmentations (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [CustomerId]        [uniqueidentifier]  NOT NULL,
    [SegmentationId]    [uniqueidentifier]  NOT NULL,
    [Source]            nvarchar(20)        NOT NULL    DEFAULT ('manual'),
    [AssignedAt]        datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [AssignedBy]        nvarchar(200)       NULL,

    CONSTRAINT [PK_CustomerSegmentations] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_CustomerSegmentations_Customers]
        FOREIGN KEY (CustomerId)
        REFERENCES dbo.Customers (Id),

    CONSTRAINT [FK_CustomerSegmentations_Segmentations]
        FOREIGN KEY (SegmentationId)
        REFERENCES dbo.Segmentations (Id),

    CONSTRAINT [UQ_CustomerSegmentations_CustomerSeg]
        UNIQUE (CustomerId, SegmentationId),

    CONSTRAINT [CK_CustomerSegmentations_Source]
        CHECK (Source IN ('field_split', 'import_file', 'manual'))
)
GO

CREATE INDEX [IX_CustomerSegmentations_Customer]
    ON dbo.CustomerSegmentations (CustomerId)
GO

CREATE INDEX [IX_CustomerSegmentations_Segmentation]
    ON dbo.CustomerSegmentations (SegmentationId)
GO
