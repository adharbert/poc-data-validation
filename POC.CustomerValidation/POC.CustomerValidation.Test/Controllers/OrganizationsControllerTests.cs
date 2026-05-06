using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;
using POC.CustomerValidation.API.Services.Provisioning;

namespace POC.CustomerValidation.Test.Controllers;

public class OrganizationsControllerTests
{
    private readonly Mock<IOrganizationServices>             _svcMock         = new();
    private readonly Mock<IOrganizationProvisioningService>  _provisioningMock = new();

    private OrganizationsController CreateSut() =>
        new(_svcMock.Object, _provisioningMock.Object, NullLogger<OrganizationsController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithDefaultParams()
    {
        var orgs = new List<OrganizationDto> { BuildOrg() };
        _svcMock.Setup(s => s.GetAllAsync(false, null)).ReturnsAsync(orgs);

        var result = await CreateSut().GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(orgs, ok.Value);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "acme")]
    [InlineData(true, "test")]
    public async Task GetAll_ReturnsOk_WithVariousParams(bool includeInactive, string? search)
    {
        _svcMock.Setup(s => s.GetAllAsync(includeInactive, search)).ReturnsAsync(new List<OrganizationDto>());

        var result = await CreateSut().GetAll(includeInactive, search);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_PassesSearchToService()
    {
        const string search = "Acme";
        _svcMock.Setup(s => s.GetAllAsync(false, search)).ReturnsAsync([]);

        await CreateSut().GetAll(search: search);

        _svcMock.Verify(s => s.GetAllAsync(false, search), Times.Once);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var id = Guid.NewGuid();
        var org = BuildOrg(id);
        _svcMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(org);

        var result = await CreateSut().GetById(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(org, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenServiceReturnsNull()
    {
        // Controller does not guard null; returns Ok(null) by design
        _svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrganizationDto?)null);

        var result = await CreateSut().GetById(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var org = BuildOrg();
        var request = new CreateOrganizationRequest("Acme", null, null, "ACME", null, null, null);
        _svcMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(org);

        var result = await CreateSut().Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OrganizationsController.GetById), created.ActionName);
        Assert.Equal(org.OrganizationId, created.RouteValues!["organizationId"]);
        Assert.Equal(org, created.Value);
    }

    [Fact]
    public async Task Create_CallsServiceWithRequest()
    {
        var request = new CreateOrganizationRequest("Acme", null, null, "ACME", null, null, null);
        _svcMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(BuildOrg());

        await CreateSut().Create(request);

        _svcMock.Verify(s => s.CreateAsync(request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var org = BuildOrg(id);
        var request = new UpdateOrganizationRequest(id, "Acme Updated", null, null, "ACME", null, null, null, null);
        _svcMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(org);

        var result = await CreateSut().Update(id, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(org, ok.Value);
    }

    [Fact]
    public async Task Update_PassesOrganizationIdAndRequest()
    {
        var id = Guid.NewGuid();
        var request = new UpdateOrganizationRequest(id, "Acme", null, null, "ACME", null, null, null, null);
        _svcMock.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(BuildOrg(id));

        await CreateSut().Update(id, request);

        _svcMock.Verify(s => s.UpdateAsync(id, request), Times.Once);
    }

    // ── SetStatus ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStatus_ReturnsNoContent(bool active)
    {
        var id = Guid.NewGuid();
        _svcMock.Setup(s => s.ChangeStatus(id, active)).Returns(Task.CompletedTask);

        var result = await CreateSut().SetStatus(id, active);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_CallsServiceWithCorrectArgs()
    {
        var id = Guid.NewGuid();
        _svcMock.Setup(s => s.ChangeStatus(id, true)).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(id, true);

        _svcMock.Verify(s => s.ChangeStatus(id, true), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrganizationDto BuildOrg(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Acme Corp", "ORG001",
        null, null, "ACME", null, null, null,
        true, false, null,
        DateTime.UtcNow, "System", DateTime.UtcNow, null);
}
