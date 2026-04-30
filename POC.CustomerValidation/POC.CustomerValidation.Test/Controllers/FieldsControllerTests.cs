using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class FieldsControllerTests
{
    private readonly Mock<IFieldDefinitionService> _svcMock = new();
    private static readonly Guid OrgId = Guid.NewGuid();

    private FieldsController CreateSut() =>
        new(_svcMock.Object, NullLogger<FieldsController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAll_ReturnsOk(bool includeInactive)
    {
        var fields = new List<FieldDefinitionDto> { BuildField() };
        _svcMock.Setup(s => s.GetByOrganizationIdAsync(OrgId, includeInactive)).ReturnsAsync(fields);

        var result = await CreateSut().GetAll(OrgId, includeInactive);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(fields, ok.Value);
    }

    [Fact]
    public async Task GetAll_PassesOrgIdAndFlagToService()
    {
        _svcMock.Setup(s => s.GetByOrganizationIdAsync(OrgId, true)).ReturnsAsync([]);

        await CreateSut().GetAll(OrgId, true);

        _svcMock.Verify(s => s.GetByOrganizationIdAsync(OrgId, true), Times.Once);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var fieldId = Guid.NewGuid();
        var field = BuildField(fieldId);
        _svcMock.Setup(s => s.GetByIdAsync(fieldId)).ReturnsAsync(field);

        var result = await CreateSut().GetById(fieldId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(field, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNull()
    {
        _svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FieldDefinitionDto?)null);

        var result = await CreateSut().GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var field = BuildField();
        var request = new CreateFieldDefinitionRequest(OrgId, null, "age", "Age", "number", null, null);
        _svcMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(field);

        var result = await CreateSut().Create(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(field, ok.Value);
    }

    [Fact]
    public async Task Create_PassesRequestToService()
    {
        var request = new CreateFieldDefinitionRequest(OrgId, null, "name", "Name", "text", null, null);
        _svcMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(BuildField());

        await CreateSut().Create(request);

        _svcMock.Verify(s => s.CreateAsync(request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var fieldId = Guid.NewGuid();
        var updated = BuildField(fieldId);
        var request = new UpdateFieldDefinitionRequest(Guid.Empty, null, "Age Label", "number",
            null, null, false, true, 0, null, null, null, null, null, null);
        _svcMock.Setup(s => s.UpdateAsync(It.Is<UpdateFieldDefinitionRequest>(r => r.FieldDefinitionId == fieldId)))
            .ReturnsAsync(updated);

        var result = await CreateSut().Update(fieldId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_MergesFieldIdIntoRequest()
    {
        var fieldId = Guid.NewGuid();
        UpdateFieldDefinitionRequest? captured = null;
        var request = new UpdateFieldDefinitionRequest(Guid.Empty, null, "Label", "text",
            null, null, false, true, 0, null, null, null, null, null, null);
        _svcMock.Setup(s => s.UpdateAsync(It.IsAny<UpdateFieldDefinitionRequest>()))
            .Callback<UpdateFieldDefinitionRequest>(r => captured = r)
            .ReturnsAsync(BuildField(fieldId));

        await CreateSut().Update(fieldId, request);

        Assert.NotNull(captured);
        Assert.Equal(fieldId, captured!.FieldDefinitionId);
    }

    // ── Reorder ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_ReturnsNoContent()
    {
        var updates = new[] { new ReorderFieldRequest(Guid.NewGuid(), 1) };
        _svcMock.Setup(s => s.ReorderAsync(OrgId, It.IsAny<IEnumerable<(Guid, int)>>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Reorder(OrgId, updates);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reorder_PassesCorrectTuplesToService()
    {
        var fieldId = Guid.NewGuid();
        var updates = new[] { new ReorderFieldRequest(fieldId, 3) };
        IEnumerable<(Guid, int)>? captured = null;
        _svcMock.Setup(s => s.ReorderAsync(OrgId, It.IsAny<IEnumerable<(Guid, int)>>()))
            .Callback<Guid, IEnumerable<(Guid, int)>>((_, u) => captured = u)
            .Returns(Task.CompletedTask);

        await CreateSut().Reorder(OrgId, updates);

        var item = Assert.Single(captured!);
        Assert.Equal((fieldId, 3), item);
    }

    [Fact]
    public async Task Reorder_PassesMultipleItems()
    {
        var f1 = Guid.NewGuid();
        var f2 = Guid.NewGuid();
        var updates = new[] { new ReorderFieldRequest(f1, 0), new ReorderFieldRequest(f2, 1) };
        IEnumerable<(Guid, int)>? captured = null;
        _svcMock.Setup(s => s.ReorderAsync(OrgId, It.IsAny<IEnumerable<(Guid, int)>>()))
            .Callback<Guid, IEnumerable<(Guid, int)>>((_, u) => captured = u)
            .Returns(Task.CompletedTask);

        await CreateSut().Reorder(OrgId, updates);

        Assert.Equal(2, captured!.Count());
    }

    // ── SetStatus ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStatus_ReturnsNoContent(bool status)
    {
        var fieldId = Guid.NewGuid();
        _svcMock.Setup(s => s.SetStatusAsync(fieldId, status)).Returns(Task.CompletedTask);

        var result = await CreateSut().SetStatus(fieldId, status);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_CallsServiceWithCorrectArgs()
    {
        var fieldId = Guid.NewGuid();
        _svcMock.Setup(s => s.SetStatusAsync(fieldId, false)).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(fieldId, false);

        _svcMock.Verify(s => s.SetStatusAsync(fieldId, false), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FieldDefinitionDto BuildField(Guid? id = null) => new(
        id ?? Guid.NewGuid(), OrgId, null, null,
        "age", "Age", "number", null, null,
        false, true, 0, null, null, null, null, null, null, []);
}
