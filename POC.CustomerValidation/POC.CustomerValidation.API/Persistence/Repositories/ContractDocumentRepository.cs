using Dapper;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.Entites;

namespace POC.CustomerValidation.API.Persistence.Repositories;

public class ContractDocumentRepository(IDbConnectionFactory db) : IContractDocumentRepository
{
    public async Task<IEnumerable<ContractDocument>> GetByContractIdAsync(Guid contractId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<ContractDocument>("""
            SELECT
                Id               AS DocumentId,
                ContractId,
                AmendmentId,
                OriginalFileName,
                StoredFileName,
                StoragePath,
                ContentType,
                FileSizeBytes,
                UploadedAt,
                UploadedBy
            FROM dbo.ContractDocuments
            WHERE ContractId = @ContractId
            ORDER BY AmendmentId, UploadedAt DESC
            """, new { ContractId = contractId });
    }

    public async Task<ContractDocument?> GetByIdAsync(Guid documentId)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<ContractDocument>("""
            SELECT
                Id               AS DocumentId,
                ContractId,
                AmendmentId,
                OriginalFileName,
                StoredFileName,
                StoragePath,
                ContentType,
                FileSizeBytes,
                UploadedAt,
                UploadedBy
            FROM dbo.ContractDocuments
            WHERE Id = @DocumentId
            """, new { DocumentId = documentId });
    }

    public async Task<ContractDocument> CreateAsync(ContractDocument document)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("""
            INSERT INTO dbo.ContractDocuments
                (Id, ContractId, AmendmentId, OriginalFileName, StoredFileName,
                 StoragePath, ContentType, FileSizeBytes, UploadedAt, UploadedBy)
            VALUES
                (@DocumentId, @ContractId, @AmendmentId, @OriginalFileName, @StoredFileName,
                 @StoragePath, @ContentType, @FileSizeBytes, @UploadedAt, @UploadedBy)
            """, document);
        return document;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM dbo.ContractDocuments WHERE Id = @DocumentId",
            new { DocumentId = documentId });
        return rows > 0;
    }
}
