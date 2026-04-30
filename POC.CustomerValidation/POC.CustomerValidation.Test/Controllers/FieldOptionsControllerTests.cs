using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class FieldOptionsControllerTests
{
    private readonly Mock<IFieldOptionService> _svcMock = new();
    private readonly Guid _fieldId = Guid.NewGuid();

    private FieldOptionsController CreateSut() =>
        new(_svcMock.Object, NullLogger<FieldOptionsController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithOptions()
    {
        var options = new List<FieldOptionDto> { BuildOption() };
        _svcMock.Setup(s => s.GetByFieldDefinitionIdAsync(_fieldId)).ReturnsAsync(options);

        var result = await CreateSut().GetAll(_fieldId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(options, ok.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenEmpty()
    {
        _svcMock.Setup(s => s.GetByFieldDefinitionIdAsync(_fieldId)).ReturnsAsync([]);

        var result = await CreateSut().GetAll(_fieldId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((IEnumerable<FieldOptionDto>)ok.Value!);
    }

    [Fact]
    public async Task GetAll_PassesFieldIdToService()
    {
        _svcMock.Setup(s => s.GetByFieldDefinitionIdAsync(_fieldId)).ReturnsAsync([]);

        await CreateSut().GetAll(_fieldId);

        _svcMock.Verify(s => s.GetByFieldDefinitionIdAsync(_fieldId), Times.Once);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201WithOption()
    {
        var request = new CreateFieldOptionRequest("opt_a", "Option A");
        var option = BuildOption();
        _svcMock.Setup(s => s.CreateAsync(_fieldId, request)).ReturnsAsync(option);

        var result = await CreateSut().Create(_fieldId, request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(option, created.Value);
    }

    [Fact]
    public async Task Create_PassesFieldIdAndRequestToService()
    {
        var request = new CreateFieldOptionRequest("opt_b", "Option B");
        _svcMock.Setup(s => s.CreateAsync(_fieldId, request)).ReturnsAsync(BuildOption());

        await CreateSut().Create(_fieldId, request);

        _svcMock.Verify(s => s.CreateAsync(_fieldId, request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var optionId = Guid.NewGuid();
        var request = new UpdateFieldOptionRequest("opt_a", "Option A Updated", 1, true);
        var option = BuildOption(optionId);
        _svcMock.Setup(s => s.UpdateAsync(optionId, request)).ReturnsAsync(option);

        var result = await CreateSut().Update(_fieldId, optionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(option, ok.Value);
    }

    [Fact]
    public async Task Update_PassesOptionIdAndRequest()
    {
        var optionId = Guid.NewGuid();
        var request = new UpdateFieldOptionRequest("opt_a", "Updated", 2, false);
        _svcMock.Setup(s => s.UpdateAsync(optionId, request)).ReturnsAsync(BuildOption(optionId));

        await CreateSut().Update(_fieldId, optionId, request);

        _svcMock.Verify(s => s.UpdateAsync(optionId, request), Times.Once);
    }

    // ── BulkUpsert ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpsert_ReturnsNoContent()
    {
        var request = new BulkUpsertFieldOptionsRequest([new CreateFieldOptionRequest("a", "A")]);
        _svcMock.Setup(s => s.BulkUpsertAsync(_fieldId, request)).Returns(Task.CompletedTask);

        var result = await CreateSut().BulkUpsert(_fieldId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task BulkUpsert_PassesFieldIdAndRequest()
    {
        var request = new BulkUpsertFieldOptionsRequest([]);
        _svcMock.Setup(s => s.BulkUpsertAsync(_fieldId, request)).Returns(Task.CompletedTask);

        await CreateSut().BulkUpsert(_fieldId, request);

        _svcMock.Verify(s => s.BulkUpsertAsync(_fieldId, request), Times.Once);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var optionId = Guid.NewGuid();
        _svcMock.Setup(s => s.DeleteAsync(optionId)).Returns(Task.CompletedTask);

        var result = await CreateSut().Delete(_fieldId, optionId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_CallsServiceWithOptionId()
    {
        var optionId = Guid.NewGuid();
        _svcMock.Setup(s => s.DeleteAsync(optionId)).Returns(Task.CompletedTask);

        await CreateSut().Delete(_fieldId, optionId);

        _svcMock.Verify(s => s.DeleteAsync(optionId), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FieldOptionDto BuildOption(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), _fieldId, "opt_a", "Option A", 0, true);
}
