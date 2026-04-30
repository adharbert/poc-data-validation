using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class ContractsControllerTests
{
    private readonly Mock<IContractService> _svcMock = new();
    private readonly Guid _orgId = Guid.NewGuid();

    private ContractsController CreateSut() =>
        new(_svcMock.Object, NullLogger<ContractsController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAll_ReturnsOk(bool includeInactive)
    {
        var contracts = new List<ContractDto> { BuildContract() };
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, includeInactive)).ReturnsAsync(contracts);

        var result = await CreateSut().GetAll(_orgId, includeInactive);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(contracts, ok.Value);
    }

    [Fact]
    public async Task GetAll_PassesOrgIdAndFlagToService()
    {
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(_orgId, false)).ReturnsAsync([]);

        await CreateSut().GetAll(_orgId, false);

        _svcMock.Verify(s => s.GetByOrganisationIdAsync(_orgId, false), Times.Once);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var contractId = Guid.NewGuid();
        var contract = BuildContract(contractId);
        _svcMock.Setup(s => s.GetByIdAsync(contractId)).ReturnsAsync(contract);

        var result = await CreateSut().GetById(_orgId, contractId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(contract, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNull()
    {
        _svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ContractDto?)null);

        var result = await CreateSut().GetById(_orgId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var contract = BuildContract();
        var request = new CreateContractRequest { ContractName = "Main", StartDate = DateOnly.FromDateTime(DateTime.Today) };
        _svcMock.Setup(s => s.CreateAsync(_orgId, request)).ReturnsAsync(contract);

        var result = await CreateSut().Create(_orgId, request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ContractsController.GetById), created.ActionName);
        Assert.Equal(contract.ContractId, created.RouteValues!["contractId"]);
        Assert.Equal(contract, created.Value);
    }

    [Fact]
    public async Task Create_PassesOrgIdAndRequest()
    {
        var request = new CreateContractRequest { ContractName = "X", StartDate = DateOnly.FromDateTime(DateTime.Today) };
        _svcMock.Setup(s => s.CreateAsync(_orgId, request)).ReturnsAsync(BuildContract());

        await CreateSut().Create(_orgId, request);

        _svcMock.Verify(s => s.CreateAsync(_orgId, request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var contractId = Guid.NewGuid();
        var contract = BuildContract(contractId);
        var request = new UpdateContractRequest { ContractName = "Updated", StartDate = DateOnly.FromDateTime(DateTime.Today) };
        _svcMock.Setup(s => s.UpdateAsync(contractId, request)).ReturnsAsync(contract);

        var result = await CreateSut().Update(_orgId, contractId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(contract, ok.Value);
    }

    [Fact]
    public async Task Update_PassesContractIdAndRequest()
    {
        var contractId = Guid.NewGuid();
        var request = new UpdateContractRequest { ContractName = "Y", StartDate = DateOnly.FromDateTime(DateTime.Today) };
        _svcMock.Setup(s => s.UpdateAsync(contractId, request)).ReturnsAsync(BuildContract(contractId));

        await CreateSut().Update(_orgId, contractId, request);

        _svcMock.Verify(s => s.UpdateAsync(contractId, request), Times.Once);
    }

    // ── SetStatus ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStatus_ReturnsNoContent(bool isActive)
    {
        var contractId = Guid.NewGuid();
        var request = new SetStatusRequest { IsActive = isActive, ModifiedBy = "Admin" };
        _svcMock.Setup(s => s.ChangeStatusAsync(contractId, isActive, "Admin")).Returns(Task.CompletedTask);

        var result = await CreateSut().SetStatus(_orgId, contractId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_PassesModifiedByToService()
    {
        var contractId = Guid.NewGuid();
        var request = new SetStatusRequest { IsActive = true, ModifiedBy = "TestUser" };
        _svcMock.Setup(s => s.ChangeStatusAsync(contractId, true, "TestUser")).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(_orgId, contractId, request);

        _svcMock.Verify(s => s.ChangeStatusAsync(contractId, true, "TestUser"), Times.Once);
    }

    [Fact]
    public async Task SetStatus_UsesDefaultModifiedByWhenNotSet()
    {
        var contractId = Guid.NewGuid();
        var request = new SetStatusRequest { IsActive = false };   // ModifiedBy defaults to "System"
        _svcMock.Setup(s => s.ChangeStatusAsync(contractId, false, "System")).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(_orgId, contractId, request);

        _svcMock.Verify(s => s.ChangeStatusAsync(contractId, false, "System"), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContractDto BuildContract(Guid? id = null) => new()
    {
        ContractId   = id ?? Guid.NewGuid(),
        ContractName = "Main Contract",
        StartDate    = DateOnly.FromDateTime(DateTime.Today),
        IsActive     = true,
        CreatedDt    = DateTime.UtcNow,
        CreatedBy    = "System",
    };
}
