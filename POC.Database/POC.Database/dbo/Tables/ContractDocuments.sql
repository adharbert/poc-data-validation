-- ============================================================
--  ContractDocuments
--
--  Stores uploaded documents for contracts and amendments.
--  AmendmentId = NULL  →  document belongs to the contract itself.
--  AmendmentId = <id>  →  document belongs to that amendment.
--
--  OriginalFileName  — the name the user uploaded.
--  StoredFileName    — the name used on disk (GUID-based, collision-safe).
--  StoragePath       — full path or relative path on the storage volume.
--  ContentType       — MIME type (e.g. application/pdf) used when serving
--                      the file back to the browser.
-- ============================================================
CREATE TABLE dbo.ContractDocuments (
    [Id]                [uniqueidentifier]  NOT NULL    DEFAULT (NEWSEQUENTIALID()),
    [ContractId]        [uniqueidentifier]  NOT NULL,
    [AmendmentId]       [uniqueidentifier]  NULL,           -- NULL = contract-level document
    [OriginalFileName]  nvarchar(260)       NOT NULL,
    [StoredFileName]    nvarchar(260)       NOT NULL,
    [StoragePath]       nvarchar(1000)      NOT NULL,
    [ContentType]       nvarchar(100)       NOT NULL,
    [FileSizeBytes]     bigint              NOT NULL,
    [UploadedAt]        datetime2           NOT NULL    DEFAULT (SYSUTCDATETIME()),
    [UploadedBy]        nvarchar(200)       NOT NULL,

    CONSTRAINT [PK_ContractDocuments] PRIMARY KEY CLUSTERED (Id),

    CONSTRAINT [FK_ContractDocuments_Contracts]
        FOREIGN KEY (ContractId)
        REFERENCES dbo.Contracts (Id),

    CONSTRAINT [FK_ContractDocuments_ContractAmendments]
        FOREIGN KEY (AmendmentId)
        REFERENCES dbo.ContractAmendments (Id)
)
GO

CREATE INDEX [IX_ContractDocuments_Contract]
    ON dbo.ContractDocuments (ContractId, AmendmentId, UploadedAt DESC)
GO
