using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _svcMock = new();
    private static readonly Guid OrgId = Guid.NewGuid();

    private CustomersController CreateSut() =>
        new(_svcMock.Object, NullLogger<CustomersController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithDefaultParams()
    {
        var paged = BuildPaged();
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(OrgId, false, 1, 50)).ReturnsAsync(paged);

        var result = await CreateSut().GetAll(OrgId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(paged, ok.Value);
    }

    [Theory]
    [InlineData(true, 2, 25)]
    [InlineData(false, 3, 10)]
    public async Task GetAll_ReturnsOk_WithVariousParams(bool includeInactive, int page, int pageSize)
    {
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(OrgId, includeInactive, page, pageSize))
            .ReturnsAsync(BuildPaged());

        var result = await CreateSut().GetAll(OrgId, includeInactive, page, pageSize);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_PassesAllParamsToService()
    {
        _svcMock.Setup(s => s.GetByOrganisationIdAsync(OrgId, true, 4, 15)).ReturnsAsync(BuildPaged());

        await CreateSut().GetAll(OrgId, true, 4, 15);

        _svcMock.Verify(s => s.GetByOrganisationIdAsync(OrgId, true, 4, 15), Times.Once);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var customerId = Guid.NewGuid();
        var customer = BuildCustomer(customerId);
        _svcMock.Setup(s => s.GetByIdAsync(customerId)).ReturnsAsync(customer);

        var result = await CreateSut().GetById(OrgId, customerId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(customer, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNull()
    {
        _svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CustomerDto?)null);

        var result = await CreateSut().GetById(OrgId, Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var customer = BuildCustomer();
        var request = new CreateCustomerRequest { FirstName = "Jane", LastName = "Doe" };
        _svcMock.Setup(s => s.CreateAsync(OrgId, request)).ReturnsAsync(customer);

        var result = await CreateSut().Create(OrgId, request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CustomersController.GetById), created.ActionName);
        Assert.Equal(customer.CustomerId, created.RouteValues!["customerId"]);
        Assert.Equal(customer, created.Value);
    }

    [Fact]
    public async Task Create_PassesOrgIdAndRequest()
    {
        var request = new CreateCustomerRequest { FirstName = "Bob", LastName = "Smith" };
        _svcMock.Setup(s => s.CreateAsync(OrgId, request)).ReturnsAsync(BuildCustomer());

        await CreateSut().Create(OrgId, request);

        _svcMock.Verify(s => s.CreateAsync(OrgId, request), Times.Once);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var customer = BuildCustomer(customerId);
        var request = new UpdateCustomerRequest { FirstName = "Jane", LastName = "Smith", IsActive = true };
        _svcMock.Setup(s => s.UpdateAsync(customerId, request)).ReturnsAsync(customer);

        var result = await CreateSut().Update(OrgId, customerId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(customer, ok.Value);
    }

    [Fact]
    public async Task Update_PassesCustomerIdAndRequest()
    {
        var customerId = Guid.NewGuid();
        var request = new UpdateCustomerRequest { FirstName = "Jane", LastName = "Smith", IsActive = false };
        _svcMock.Setup(s => s.UpdateAsync(customerId, request)).ReturnsAsync(BuildCustomer(customerId));

        await CreateSut().Update(OrgId, customerId, request);

        _svcMock.Verify(s => s.UpdateAsync(customerId, request), Times.Once);
    }

    // ── SetStatus ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStatus_ReturnsNoContent(bool isActive)
    {
        var customerId = Guid.NewGuid();
        _svcMock.Setup(s => s.ChangeStatusAsync(customerId, isActive)).Returns(Task.CompletedTask);
        var request = new SetStatusRequest { IsActive = isActive };

        var result = await CreateSut().SetStatus(OrgId, customerId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_PassesIsActiveToService()
    {
        var customerId = Guid.NewGuid();
        _svcMock.Setup(s => s.ChangeStatusAsync(customerId, false)).Returns(Task.CompletedTask);

        await CreateSut().SetStatus(OrgId, customerId, new SetStatusRequest { IsActive = false });

        _svcMock.Verify(s => s.ChangeStatusAsync(customerId, false), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PagedResult<CustomerDto> BuildPaged(IEnumerable<CustomerDto>? items = null) =>
        new(items ?? [], 0, 1, 50);

    private static CustomerDto BuildCustomer(Guid? id = null) => new()
    {
        CustomerId     = id ?? Guid.NewGuid(),
        OrganizationId = OrgId,
        FirstName      = "Jane",
        LastName       = "Doe",
        CustomerCode   = "C001",
        IsActive       = true,
        CreatedDate    = DateTime.UtcNow,
        ModifiedDate   = DateTime.UtcNow,
    };
}
