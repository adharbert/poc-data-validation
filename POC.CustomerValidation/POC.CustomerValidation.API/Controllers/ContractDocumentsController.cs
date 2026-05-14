using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[ApiController]
[Route("api/organisations/{organisationId:guid}/contracts/{contractId:guid}/documents")]
[Produces("application/json")]
public class ContractDocumentsController(IContractDocumentService service) : ControllerBase
{
    /// <summary>List all documents for a contract (contract-level and amendment-level).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContractDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid organisationId, Guid contractId)
    {
        var docs = await service.GetByContractIdAsync(contractId);
        return Ok(docs);
    }



    /// <summary>Upload a document. Set amendmentId in the form to attach to a specific amendment.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContractDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(Guid organisationId, Guid contractId, IFormFile file, [FromForm] UploadContractDocumentRequest request)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiError("BAD_REQUEST", "No file provided."));

        var doc = await service.UploadAsync(contractId, file, request);
        return CreatedAtAction(nameof(Download), new { organisationId, contractId, documentId = doc.DocumentId }, doc);
    }




    /// <summary>
    /// Download a document. Returns the file with Content-Disposition: inline so
    /// the browser opens supported types (e.g. PDF) directly.
    /// </summary>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid organisationId, Guid contractId, Guid documentId)
    {
        var (stream, meta) = await service.DownloadAsync(documentId);
        var encodedName = Uri.EscapeDataString(meta.OriginalFileName);
        Response.Headers.Append("Content-Disposition", $"inline; filename=\"{encodedName}\"; filename*=UTF-8''{encodedName}");
        return File(stream, meta.ContentType);
    }




    /// <summary>Delete a document record and its file from disk.</summary>
    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid organisationId, Guid contractId, Guid documentId)
    {
        await service.DeleteAsync(documentId);
        return NoContent();
    }
}
