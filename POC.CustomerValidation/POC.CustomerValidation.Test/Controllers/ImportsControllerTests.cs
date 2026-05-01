using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class ImportsControllerTests
{
    private readonly Mock<IImportService>        _importMock  = new();
    private readonly Mock<IImportStagingService> _stagingMock = new();
    private readonly Guid _orgId = Guid.NewGuid();

    private ImportsController CreateSut() =>
        new(_importMock.Object, _stagingMock.Object, NullLogger<ImportsController>.Instance);

    // ── Upload — guard branches ───────────────────────────────────────────────

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsNull()
    {
        var result = await CreateSut().Upload(_orgId, null!);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ApiError>(bad.Value);
        Assert.Equal("BAD_REQUEST", error.Code);
    }

    [Fact]
    public async Task Upload_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var file = BuildFile("test.csv", length: 0);

        var result = await CreateSut().Upload(_orgId, file.Object);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ApiError>(bad.Value);
    }

    [Fact]
    public async Task Upload_ReturnsCreatedAtAction_WhenFileIsValid()
    {
        var file = BuildFile("data.csv", length: 1024);
        var response = new UploadImportResponseDto { BatchId = Guid.NewGuid(), RowCount = 10 };
        _importMock.Setup(s => s.UploadAsync(_orgId, file.Object, "System", "skip")).ReturnsAsync(response);

        var result = await CreateSut().Upload(_orgId, file.Object);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ImportsController.GetBatch), created.ActionName);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Upload_PassesCustomUploadedByAndStrategy()
    {
        var file = BuildFile("import.xlsx", length: 2048);
        var response = new UploadImportResponseDto { BatchId = Guid.NewGuid() };
        _importMock.Setup(s => s.UploadAsync(_orgId, file.Object, "UserA", "update")).ReturnsAsync(response);

        await CreateSut().Upload(_orgId, file.Object, "UserA", "update");

        _importMock.Verify(s => s.UploadAsync(_orgId, file.Object, "UserA", "update"), Times.Once);
    }

    // ── GetBatches ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBatches_ReturnsOk_WithDefaultPaging()
    {
        var paged = new PagedResult<ImportBatchDto>([], 0, 1, 20);
        _importMock.Setup(s => s.GetBatchesAsync(_orgId, 1, 20)).ReturnsAsync(paged);

        var result = await CreateSut().GetBatches(_orgId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(paged, ok.Value);
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(3, 5)]
    public async Task GetBatches_ReturnsOk_WithCustomPaging(int page, int pageSize)
    {
        _importMock.Setup(s => s.GetBatchesAsync(_orgId, page, pageSize))
            .ReturnsAsync(new PagedResult<ImportBatchDto>([], 0, page, pageSize));

        var result = await CreateSut().GetBatches(_orgId, page, pageSize);

        Assert.IsType<OkObjectResult>(result);
        _importMock.Verify(s => s.GetBatchesAsync(_orgId, page, pageSize), Times.Once);
    }

    // ── GetBatch ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBatch_ReturnsOk_WhenFound()
    {
        var batchId = Guid.NewGuid();
        var batch = BuildBatch(batchId);
        _importMock.Setup(s => s.GetBatchAsync(batchId)).ReturnsAsync(batch);

        var result = await CreateSut().GetBatch(_orgId, batchId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(batch, ok.Value);
    }

    [Fact]
    public async Task GetBatch_ReturnsNotFound_WhenNull()
    {
        _importMock.Setup(s => s.GetBatchAsync(It.IsAny<Guid>())).ReturnsAsync((ImportBatchDto?)null);

        var result = await CreateSut().GetBatch(_orgId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetSavedMappings ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSavedMappings_ReturnsOk()
    {
        var matches = new List<ColumnMatchResultDto>();
        _importMock.Setup(s => s.GetSavedMappingsAsync(_orgId, "fp123")).ReturnsAsync(matches);

        var result = await CreateSut().GetSavedMappings(_orgId, "fp123");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(matches, ok.Value);
    }

    [Fact]
    public async Task GetSavedMappings_PassesOrgIdAndFingerprint()
    {
        _importMock.Setup(s => s.GetSavedMappingsAsync(_orgId, "abc")).ReturnsAsync([]);

        await CreateSut().GetSavedMappings(_orgId, "abc");

        _importMock.Verify(s => s.GetSavedMappingsAsync(_orgId, "abc"), Times.Once);
    }

    [Fact]
    public async Task GetSavedMappings_ReturnsMappingsWithDestinationFields()
    {
        var match = BuildDirectMatchResult();
        _importMock.Setup(s => s.GetSavedMappingsAsync(_orgId, "fp1")).ReturnsAsync([match]);

        var result = await CreateSut().GetSavedMappings(_orgId, "fp1");

        var ok    = Assert.IsType<OkObjectResult>(result);
        var list  = Assert.IsAssignableFrom<IEnumerable<ColumnMatchResultDto>>(ok.Value);
        var first = Assert.Single(list);
        Assert.Equal("customer",  first.DestinationTable);
        Assert.Equal("FirstName", first.DestinationField);
        Assert.Equal("direct",    first.TransformType);
        Assert.True(first.IsAutoMatched);
    }

    [Fact]
    public async Task GetSavedMappings_ReturnsSplitTransformWithOutputs()
    {
        var match = BuildSplitMatchResult();
        _importMock.Setup(s => s.GetSavedMappingsAsync(_orgId, "fp2")).ReturnsAsync([match]);

        var result = await CreateSut().GetSavedMappings(_orgId, "fp2");

        var ok    = Assert.IsType<OkObjectResult>(result);
        var list  = Assert.IsAssignableFrom<IEnumerable<ColumnMatchResultDto>>(ok.Value);
        var first = Assert.Single(list);
        Assert.Equal("split_full_name", first.TransformType);
        Assert.Equal(5, first.Outputs.Count());
        var fn = first.Outputs.Single(o => o.OutputToken == "FirstName");
        Assert.Equal("customer",  fn.DestinationTable);
        Assert.Equal("FirstName", fn.DestinationField);
        var cred = first.Outputs.Single(o => o.OutputToken == "Credentials");
        Assert.Equal("skip", cred.DestinationTable);
        Assert.Null(cred.DestinationField);
    }

    // ── SaveMappings ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveMappings_ReturnsNoContent()
    {
        var batchId = Guid.NewGuid();
        var request = new SaveMappingsRequest();
        _importMock.Setup(s => s.SaveMappingsAsync(batchId, request)).Returns(Task.CompletedTask);

        var result = await CreateSut().SaveMappings(_orgId, batchId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SaveMappings_PassesBatchIdAndRequest()
    {
        var batchId = Guid.NewGuid();
        var request = new SaveMappingsRequest { DuplicateStrategy = "update" };
        _importMock.Setup(s => s.SaveMappingsAsync(batchId, request)).Returns(Task.CompletedTask);

        await CreateSut().SaveMappings(_orgId, batchId, request);

        _importMock.Verify(s => s.SaveMappingsAsync(batchId, request), Times.Once);
    }

    [Fact]
    public async Task SaveMappings_WithDirectMapping_PassesDestinationTableAndField()
    {
        var batchId = Guid.NewGuid();
        var request = new SaveMappingsRequest
        {
            DuplicateStrategy = "skip",
            Mappings          = [BuildDirectMapping(), BuildAddressMapping()],
        };
        _importMock.Setup(s => s.SaveMappingsAsync(batchId, request)).Returns(Task.CompletedTask);

        var result = await CreateSut().SaveMappings(_orgId, batchId, request);

        Assert.IsType<NoContentResult>(result);
        _importMock.Verify(s => s.SaveMappingsAsync(batchId, request), Times.Once);
        var customer = request.Mappings.First();
        Assert.Equal("customer", customer.DestinationTable);
        Assert.Equal("Email",    customer.DestinationField);
        Assert.Equal("direct",   customer.TransformType);
        var address = request.Mappings.Last();
        Assert.Equal("customer_address", address.DestinationTable);
        Assert.Equal("AddressLine1",     address.DestinationField);
    }

    [Fact]
    public async Task SaveMappings_WithSplitTransform_PassesOutputTokens()
    {
        var batchId = Guid.NewGuid();
        var request = new SaveMappingsRequest
        {
            DuplicateStrategy = "skip",
            Mappings          = [BuildSplitMapping()],
        };
        _importMock.Setup(s => s.SaveMappingsAsync(batchId, request)).Returns(Task.CompletedTask);

        await CreateSut().SaveMappings(_orgId, batchId, request);

        _importMock.Verify(s => s.SaveMappingsAsync(batchId, request), Times.Once);
        var mapping = request.Mappings.Single();
        Assert.Equal("split_full_name", mapping.TransformType);
        Assert.Null(mapping.DestinationField);
        Assert.Equal(5, mapping.Outputs.Count());
        Assert.Contains(mapping.Outputs, o => o.OutputToken == "FirstName"    && o.DestinationTable == "customer" && o.DestinationField == "FirstName");
        Assert.Contains(mapping.Outputs, o => o.OutputToken == "MiddleName"   && o.DestinationTable == "customer" && o.DestinationField == "MiddleName");
        Assert.Contains(mapping.Outputs, o => o.OutputToken == "LastName"     && o.DestinationTable == "customer" && o.DestinationField == "LastName");
        Assert.Contains(mapping.Outputs, o => o.OutputToken == "Suffix"       && o.DestinationTable == "skip");
        Assert.Contains(mapping.Outputs, o => o.OutputToken == "Credentials"  && o.DestinationTable == "skip");
    }

    // ── Preview ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Preview_ReturnsOk()
    {
        var batchId = Guid.NewGuid();
        var preview = new ImportPreviewDto { OkCount = 10 };
        _importMock.Setup(s => s.PreviewAsync(batchId)).ReturnsAsync(preview);

        var result = await CreateSut().Preview(_orgId, batchId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(preview, ok.Value);
    }

    // ── Execute ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_ReturnsAccepted()
    {
        var batchId = Guid.NewGuid();
        _importMock.Setup(s => s.ExecuteAsync(batchId)).Returns(Task.CompletedTask);

        var result = await CreateSut().Execute(_orgId, batchId);

        Assert.IsType<AcceptedResult>(result);
    }

    // ── GetErrors ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetErrors_ReturnsOk()
    {
        var batchId = Guid.NewGuid();
        var errors = new List<ImportErrorDto>();
        _importMock.Setup(s => s.GetErrorsAsync(batchId)).ReturnsAsync(errors);

        var result = await CreateSut().GetErrors(_orgId, batchId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(errors, ok.Value);
    }

    [Fact]
    public async Task GetErrors_PassesBatchId()
    {
        var batchId = Guid.NewGuid();
        _importMock.Setup(s => s.GetErrorsAsync(batchId)).ReturnsAsync([]);

        await CreateSut().GetErrors(_orgId, batchId);

        _importMock.Verify(s => s.GetErrorsAsync(batchId), Times.Once);
    }

    // ── GetStaging ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStaging_ReturnsOk_WithoutStatusFilter()
    {
        var staging = new List<ImportColumnStagingDto>();
        _stagingMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, null)).ReturnsAsync(staging);

        var result = await CreateSut().GetStaging(_orgId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(staging, ok.Value);
    }

    [Fact]
    public async Task GetStaging_ReturnsOk_WithStatusFilter()
    {
        _stagingMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, "pending")).ReturnsAsync([]);

        var result = await CreateSut().GetStaging(_orgId, "pending");

        Assert.IsType<OkObjectResult>(result);
        _stagingMock.Verify(s => s.GetByOrganisationIdAsync(_orgId, "pending"), Times.Once);
    }

    // ── GetStagingById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStagingById_ReturnsOk_WhenFound()
    {
        var stagingId = Guid.NewGuid();
        var staging = BuildStaging(stagingId);
        _stagingMock.Setup(s => s.GetByIdAsync(stagingId)).ReturnsAsync(staging);

        var result = await CreateSut().GetStagingById(_orgId, stagingId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(staging, ok.Value);
    }

    [Fact]
    public async Task GetStagingById_ReturnsNotFound_WhenNull()
    {
        _stagingMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ImportColumnStagingDto?)null);

        var result = await CreateSut().GetStagingById(_orgId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── ResolveStaging ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveStaging_ReturnsOk()
    {
        var stagingId = Guid.NewGuid();
        var request = new ResolveColumnStagingRequest { Status = "resolved", MappingType = "field" };
        var staging = BuildStaging(stagingId);
        _stagingMock.Setup(s => s.ResolveAsync(stagingId, request)).ReturnsAsync(staging);

        var result = await CreateSut().ResolveStaging(_orgId, stagingId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(staging, ok.Value);
    }

    [Fact]
    public async Task ResolveStaging_PassesStagingIdAndRequest()
    {
        var stagingId = Guid.NewGuid();
        var request = new ResolveColumnStagingRequest { Status = "skipped" };
        _stagingMock.Setup(s => s.ResolveAsync(stagingId, request)).ReturnsAsync(BuildStaging(stagingId));

        await CreateSut().ResolveStaging(_orgId, stagingId, request);

        _stagingMock.Verify(s => s.ResolveAsync(stagingId, request), Times.Once);
    }

    // ── DeleteStaging ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteStaging_ReturnsNoContent()
    {
        var stagingId = Guid.NewGuid();
        _stagingMock.Setup(s => s.DeleteAsync(stagingId)).Returns(Task.CompletedTask);

        var result = await CreateSut().DeleteStaging(_orgId, stagingId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteStaging_CallsServiceWithCorrectId()
    {
        var stagingId = Guid.NewGuid();
        _stagingMock.Setup(s => s.DeleteAsync(stagingId)).Returns(Task.CompletedTask);

        await CreateSut().DeleteStaging(_orgId, stagingId);

        _stagingMock.Verify(s => s.DeleteAsync(stagingId), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Mock<IFormFile> BuildFile(string name, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.Length).Returns(length);
        return mock;
    }

    private static ImportBatchDto BuildBatch(Guid? id = null) => new()
    {
        BatchId           = id ?? Guid.NewGuid(),
        FileName          = "test.csv",
        Status            = "completed",
        DuplicateStrategy = "skip",
        UploadedBy        = "System",
        UploadedAt        = DateTime.UtcNow,
    };

    private static ImportColumnStagingDto BuildStaging(Guid? id = null) => new()
    {
        StagingId   = id ?? Guid.NewGuid(),
        CsvHeader   = "email_address",
        Status      = "pending",
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt  = DateTime.UtcNow,
    };

    private static ColumnMatchResultDto BuildDirectMatchResult() => new()
    {
        ColumnIndex      = 0,
        CsvHeader        = "first_name",
        MatchStatus      = "matched",
        DestinationTable = "customer",
        DestinationField = "FirstName",
        TransformType    = "direct",
        IsAutoMatched    = true,
    };

    private static ColumnMatchResultDto BuildSplitMatchResult() => new()
    {
        ColumnIndex      = 1,
        CsvHeader        = "full_name",
        MatchStatus      = "unmatched",
        DestinationTable = "customer",
        DestinationField = null,
        TransformType    = "split_full_name",
        IsAutoMatched    = false,
        Outputs          =
        [
            new() { OutputToken = "FirstName",   DestinationTable = "customer", DestinationField = "FirstName",   SortOrder = 1 },
            new() { OutputToken = "MiddleName",  DestinationTable = "customer", DestinationField = "MiddleName",  SortOrder = 2 },
            new() { OutputToken = "LastName",    DestinationTable = "customer", DestinationField = "LastName",    SortOrder = 3 },
            new() { OutputToken = "Suffix",      DestinationTable = "skip",     DestinationField = null,          SortOrder = 4 },
            new() { OutputToken = "Credentials", DestinationTable = "skip",     DestinationField = null,          SortOrder = 5 },
        ],
    };

    private static ColumnMappingDto BuildDirectMapping() => new()
    {
        ColumnIndex      = 0,
        CsvHeader        = "email",
        DestinationTable = "customer",
        DestinationField = "Email",
        TransformType    = "direct",
        SaveForReuse     = true,
    };

    private static ColumnMappingDto BuildAddressMapping() => new()
    {
        ColumnIndex      = 1,
        CsvHeader        = "address_line1",
        DestinationTable = "customer_address",
        DestinationField = "AddressLine1",
        TransformType    = "direct",
        SaveForReuse     = true,
    };

    private static ColumnMappingDto BuildSplitMapping() => new()
    {
        ColumnIndex      = 2,
        CsvHeader        = "full_name",
        DestinationTable = "customer",
        DestinationField = null,
        TransformType    = "split_full_name",
        SaveForReuse     = true,
        Outputs          =
        [
            new() { OutputToken = "FirstName",   DestinationTable = "customer", DestinationField = "FirstName",   SortOrder = 1 },
            new() { OutputToken = "MiddleName",  DestinationTable = "customer", DestinationField = "MiddleName",  SortOrder = 2 },
            new() { OutputToken = "LastName",    DestinationTable = "customer", DestinationField = "LastName",    SortOrder = 3 },
            new() { OutputToken = "Suffix",      DestinationTable = "skip",     DestinationField = null,          SortOrder = 4 },
            new() { OutputToken = "Credentials", DestinationTable = "skip",     DestinationField = null,          SortOrder = 5 },
        ],
    };
}
