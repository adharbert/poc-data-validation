using Microsoft.AspNetCore.Mvc;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class FieldSectionsControllerTests
{
    private readonly Mock<IFieldSectionService>    _sectionMock = new();
    private readonly Mock<IFieldDefinitionService> _fieldMock   = new();
    private readonly Guid _orgId = Guid.NewGuid();

    // FieldSectionsController has no logger dependency
    private FieldSectionsController CreateSut() =>
        new(_sectionMock.Object, _fieldMock.Object);

    // ── GetSections ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSections_ReturnsOk()
    {
        var sections = new List<FieldSectionDto> { BuildSection() };
        _sectionMock.Setup(s => s.GetByOrganizationIdAsync(_orgId)).ReturnsAsync(sections);

        var result = await CreateSut().GetSections(_orgId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(sections, ok.Value);
    }

    [Fact]
    public async Task GetSections_PassesOrgId()
    {
        _sectionMock.Setup(s => s.GetByOrganizationIdAsync(_orgId)).ReturnsAsync([]);

        await CreateSut().GetSections(_orgId);

        _sectionMock.Verify(s => s.GetByOrganizationIdAsync(_orgId), Times.Once);
    }

    // ── CreateSection ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSection_ReturnsCreatedAtAction()
    {
        var section = BuildSection();
        var request = new CreateFieldSectionRequest(_orgId, "Demographics");
        _sectionMock.Setup(s => s.CreateAsync(_orgId, request)).ReturnsAsync(section);

        var result = await CreateSut().CreateSection(_orgId, request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FieldSectionsController.GetSection), created.ActionName);
        Assert.Equal(section.SectionId, created.RouteValues!["sectionId"]);
        Assert.Equal(section, created.Value);
    }

    // ── GetSection ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSection_ReturnsOk_WhenFound()
    {
        var sectionId = Guid.NewGuid();
        var section = BuildSection(sectionId);
        _sectionMock.Setup(s => s.GetByIdAsync(sectionId)).ReturnsAsync(section);

        var result = await CreateSut().GetSection(_orgId, sectionId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(section, ok.Value);
    }

    [Fact]
    public async Task GetSection_ReturnsNotFound_WhenNull()
    {
        _sectionMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FieldSectionDto?)null);

        var result = await CreateSut().GetSection(_orgId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── UpdateSection ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSection_ReturnsOk()
    {
        var sectionId = Guid.NewGuid();
        var request = new UpdateFieldSectionRequest("Updated", 1, true);
        var section = BuildSection(sectionId);
        _sectionMock.Setup(s => s.UpdateAsync(sectionId, request)).ReturnsAsync(section);

        var result = await CreateSut().UpdateSection(_orgId, sectionId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(section, ok.Value);
    }

    [Fact]
    public async Task UpdateSection_PassesSectionIdAndRequest()
    {
        var sectionId = Guid.NewGuid();
        var request = new UpdateFieldSectionRequest("Name", 0, false);
        _sectionMock.Setup(s => s.UpdateAsync(sectionId, request)).ReturnsAsync(BuildSection(sectionId));

        await CreateSut().UpdateSection(_orgId, sectionId, request);

        _sectionMock.Verify(s => s.UpdateAsync(sectionId, request), Times.Once);
    }

    // ── SetSectionStatus ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetSectionStatus_ReturnsNoContent(bool isActive)
    {
        var sectionId = Guid.NewGuid();
        _sectionMock.Setup(s => s.SetStatusAsync(sectionId, isActive)).Returns(Task.CompletedTask);

        var result = await CreateSut().SetSectionStatus(_orgId, sectionId, new SectionStatusRequest(isActive));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetSectionStatus_CallsServiceWithCorrectArgs()
    {
        var sectionId = Guid.NewGuid();
        _sectionMock.Setup(s => s.SetStatusAsync(sectionId, true)).Returns(Task.CompletedTask);

        await CreateSut().SetSectionStatus(_orgId, sectionId, new SectionStatusRequest(true));

        _sectionMock.Verify(s => s.SetStatusAsync(sectionId, true), Times.Once);
    }

    // ── ReorderSections ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderSections_ReturnsNoContent()
    {
        var items = new[] { new SectionOrderItem { SectionId = Guid.NewGuid(), DisplayOrder = 1 } };
        var request = new ReorderSectionsRequest { Sections = items };
        _sectionMock.Setup(s => s.ReorderAsync(It.IsAny<IEnumerable<SectionOrderItem>>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ReorderSections(_orgId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ReorderSections_PassesSectionsCollectionToService()
    {
        var items = new[] { new SectionOrderItem { SectionId = Guid.NewGuid(), DisplayOrder = 2 } };
        var request = new ReorderSectionsRequest { Sections = items };
        IEnumerable<SectionOrderItem>? captured = null;
        _sectionMock.Setup(s => s.ReorderAsync(It.IsAny<IEnumerable<SectionOrderItem>>()))
            .Callback<IEnumerable<SectionOrderItem>>(s => captured = s)
            .Returns(Task.CompletedTask);

        await CreateSut().ReorderSections(_orgId, request);

        Assert.Same(items, captured);
    }

    // ── AssignFields ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignFields_ReturnsNoContent()
    {
        var sectionId = Guid.NewGuid();
        var request = new AssignFieldsToSectionRequest
        {
            Fields = [new SectionFieldAssignment { FieldDefinitionId = Guid.NewGuid(), DisplayOrder = 0 }]
        };
        _sectionMock.Setup(s => s.AssignFieldsAsync(sectionId, request)).Returns(Task.CompletedTask);

        var result = await CreateSut().AssignFields(_orgId, sectionId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AssignFields_PassesSectionIdAndRequest()
    {
        var sectionId = Guid.NewGuid();
        var request = new AssignFieldsToSectionRequest();
        _sectionMock.Setup(s => s.AssignFieldsAsync(sectionId, request)).Returns(Task.CompletedTask);

        await CreateSut().AssignFields(_orgId, sectionId, request);

        _sectionMock.Verify(s => s.AssignFieldsAsync(sectionId, request), Times.Once);
    }

    // ── GetFormPreview ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFormPreview_ReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var preview = new CustomerFormPreviewDto { CustomerId = customerId, CustomerName = "Jane Doe" };
        _fieldMock.Setup(s => s.GetFormPreviewAsync(_orgId, customerId)).ReturnsAsync(preview);

        var result = await CreateSut().GetFormPreview(_orgId, customerId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(preview, ok.Value);
    }

    [Fact]
    public async Task GetFormPreview_PassesOrgIdAndCustomerId()
    {
        var customerId = Guid.NewGuid();
        _fieldMock.Setup(s => s.GetFormPreviewAsync(_orgId, customerId))
            .ReturnsAsync(new CustomerFormPreviewDto());

        await CreateSut().GetFormPreview(_orgId, customerId);

        _fieldMock.Verify(s => s.GetFormPreviewAsync(_orgId, customerId), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FieldSectionDto BuildSection(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), _orgId, "Demographics", 0, true);
}
