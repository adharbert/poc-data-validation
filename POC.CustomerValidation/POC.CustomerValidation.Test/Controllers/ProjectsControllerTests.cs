using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IMarketingProjectService> _svcMock = new();
    private readonly Guid _orgId = Guid.NewGuid();

    private ProjectsController CreateSut() =>
        new(_svcMock.Object, NullLogger<ProjectsController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAll_ReturnsOk(bool includeInactive)
    {
        var projects = new List<MarketingProjectDto> { BuildProject() };
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, includeInactive)).ReturnsAsync(projects);

        var result = await CreateSut().GetAll(_orgId, includeInactive);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(projects, ok.Value);
    }

    [Fact]
    public async Task GetAll_PassesOrgIdAndFlagToService()
    {
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, true)).ReturnsAsync([]);

        await CreateSut().GetAll(_orgId, true);

        _svcMock.Verify(s => s.GetByOrganisationIdAsync(_orgId, true), Times.Once);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var project = BuildProject(8001);
        _svcMock.Setup(s => s.GetByIdAsync(8001)).ReturnsAsync(project);

        var result = await CreateSut().GetById(_orgId, 8001);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(project, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNull()
    {
        _svcMock.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((MarketingProjectDto?)null);

        var result = await CreateSut().GetById(_orgId, 9999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var project = BuildProject(8002);
        var request = new CreateMarketingProjectRequest
        {
            ProjectName = "Spring 2025",
            MarketingStartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _svcMock.Setup(s => s.CreateAsync(_orgId, request)).ReturnsAsync(project);

        var result = await CreateSut().Create(_orgId, request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProjectsController.GetById), created.ActionName);
        Assert.Equal(project.ProjectId, created.RouteValues!["projectId"]);
        Assert.Equal(project, created.Value);
    }

    [Fact]
    public async Task Create_PassesOrgIdAndRequest()
    {
        var request = new CreateMarketingProjectRequest
        {
            ProjectName = "Fall",
            MarketingStartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _svcMock.Setup(s => s.CreateAsync(_orgId, request)).ReturnsAsync(BuildProject());

        await CreateSut().Create(_orgId, request);

        _svcMock.Verify(s => s.CreateAsync(_orgId, request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var project = BuildProject(8001);
        var request = new UpdateMarketingProjectRequest
        {
            ProjectName = "Updated",
            MarketingStartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _svcMock.Setup(s => s.UpdateAsync(8001, request)).ReturnsAsync(project);

        var result = await CreateSut().Update(_orgId, 8001, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(project, ok.Value);
    }

    [Fact]
    public async Task Update_PassesProjectIdAndRequest()
    {
        var request = new UpdateMarketingProjectRequest
        {
            ProjectName = "X",
            MarketingStartDate = DateOnly.FromDateTime(DateTime.Today)
        };
        _svcMock.Setup(s => s.UpdateAsync(8003, request)).ReturnsAsync(BuildProject(8003));

        await CreateSut().Update(_orgId, 8003, request);

        _svcMock.Verify(s => s.UpdateAsync(8003, request), Times.Once);
    }

    // ── SetStatus ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStatus_ReturnsNoContent(bool isActive)
    {
        var request = new SetStatusRequest { IsActive = isActive, ModifiedBy = "Admin" };
        _svcMock.Setup(s => s.ChangeStatusAsync(8001, isActive, "Admin")).Returns(Task.CompletedTask);

        var result = await CreateSut().SetStatus(_orgId, 8001, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_PassesModifiedByToService()
    {
        var request = new SetStatusRequest { IsActive = true, ModifiedBy = "SomeUser" };
        _svcMock.Setup(s => s.ChangeStatusAsync(8002, true, "SomeUser")).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(_orgId, 8002, request);

        _svcMock.Verify(s => s.ChangeStatusAsync(8002, true, "SomeUser"), Times.Once);
    }

    [Fact]
    public async Task SetStatus_UsesDefaultModifiedByWhenNotSet()
    {
        var request = new SetStatusRequest { IsActive = false };   // ModifiedBy defaults to "System"
        _svcMock.Setup(s => s.ChangeStatusAsync(8001, false, "System")).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(_orgId, 8001, request);

        _svcMock.Verify(s => s.ChangeStatusAsync(8001, false, "System"), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MarketingProjectDto BuildProject(int id = 8000) => new()
    {
        ProjectId          = id,
        OrganizationId     = _orgId,
        OrganizationName   = "Acme",
        ProjectName        = "Spring Campaign",
        MarketingStartDate = DateOnly.FromDateTime(DateTime.Today),
        IsActive           = true,
        CreatedDt          = DateTime.UtcNow,
        CreatedBy          = "System",
    };
}
