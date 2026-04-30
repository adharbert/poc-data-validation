using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class CustomerFieldValuesControllerTests
{
    private readonly Mock<IFieldValueService> _svcMock = new();
    private readonly Guid _customerId = Guid.NewGuid();

    private CustomerFieldValuesController CreateSut() =>
        new(_svcMock.Object, NullLogger<CustomerFieldValuesController>.Instance);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithValues()
    {
        var values = new List<FieldValueDto> { BuildFieldValue() };
        _svcMock.Setup(s => s.GetByCustomerIdAsync(_customerId)).ReturnsAsync(values);

        var result = await CreateSut().GetAll(_customerId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(values, ok.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WhenEmpty()
    {
        _svcMock.Setup(s => s.GetByCustomerIdAsync(_customerId)).ReturnsAsync([]);

        var result = await CreateSut().GetAll(_customerId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((IEnumerable<FieldValueDto>)ok.Value!);
    }

    [Fact]
    public async Task GetAll_PassesCustomerIdToService()
    {
        _svcMock.Setup(s => s.GetByCustomerIdAsync(_customerId)).ReturnsAsync([]);

        await CreateSut().GetAll(_customerId);

        _svcMock.Verify(s => s.GetByCustomerIdAsync(_customerId), Times.Once);
    }

    // ── GetHistory ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ReturnsOk_WithDefaultPaging()
    {
        var history = new List<FieldValueHistoryDto> { BuildHistory() };
        _svcMock.Setup(s => s.GetHistoryByCustomerAsync(_customerId, 1, 50)).ReturnsAsync(history);

        var result = await CreateSut().GetHistory(_customerId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(history, ok.Value);
    }

    [Theory]
    [InlineData(2, 25)]
    [InlineData(3, 10)]
    public async Task GetHistory_ReturnsOk_WithCustomPaging(int page, int pageSize)
    {
        _svcMock.Setup(s => s.GetHistoryByCustomerAsync(_customerId, page, pageSize)).ReturnsAsync([]);

        var result = await CreateSut().GetHistory(_customerId, page, pageSize);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetHistory_PassesAllPagingArgsToService()
    {
        _svcMock.Setup(s => s.GetHistoryByCustomerAsync(_customerId, 5, 100)).ReturnsAsync([]);

        await CreateSut().GetHistory(_customerId, 5, 100);

        _svcMock.Verify(s => s.GetHistoryByCustomerAsync(_customerId, 5, 100), Times.Once);
    }

    [Fact]
    public async Task GetHistory_UsesDefaultPage1AndSize50()
    {
        _svcMock.Setup(s => s.GetHistoryByCustomerAsync(_customerId, 1, 50)).ReturnsAsync([]);

        await CreateSut().GetHistory(_customerId);

        _svcMock.Verify(s => s.GetHistoryByCustomerAsync(_customerId, 1, 50), Times.Once);
    }

    // ── GetFieldHistory ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetFieldHistory_ReturnsOk_WithHistory()
    {
        var fieldId = Guid.NewGuid();
        var history = new List<FieldValueHistoryDto> { BuildHistory() };
        _svcMock.Setup(s => s.GetHistoryByFieldAsync(_customerId, fieldId)).ReturnsAsync(history);

        var result = await CreateSut().GetFieldHistory(_customerId, fieldId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(history, ok.Value);
    }

    [Fact]
    public async Task GetFieldHistory_ReturnsOk_WhenEmpty()
    {
        var fieldId = Guid.NewGuid();
        _svcMock.Setup(s => s.GetHistoryByFieldAsync(_customerId, fieldId)).ReturnsAsync([]);

        var result = await CreateSut().GetFieldHistory(_customerId, fieldId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((IEnumerable<FieldValueHistoryDto>)ok.Value!);
    }

    [Fact]
    public async Task GetFieldHistory_PassesBothIdsToService()
    {
        var fieldId = Guid.NewGuid();
        _svcMock.Setup(s => s.GetHistoryByFieldAsync(_customerId, fieldId)).ReturnsAsync([]);

        await CreateSut().GetFieldHistory(_customerId, fieldId);

        _svcMock.Verify(s => s.GetHistoryByFieldAsync(_customerId, fieldId), Times.Once);
    }

    [Fact]
    public async Task GetFieldHistory_DoesNotCallGetHistoryByCustomer()
    {
        var fieldId = Guid.NewGuid();
        _svcMock.Setup(s => s.GetHistoryByFieldAsync(_customerId, fieldId)).ReturnsAsync([]);

        await CreateSut().GetFieldHistory(_customerId, fieldId);

        _svcMock.Verify(s => s.GetHistoryByCustomerAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private FieldValueDto BuildFieldValue() => new(
        Guid.NewGuid(), _customerId, Guid.NewGuid(),
        "Email", "email", "jane@example.com",
        null, null, null, null, "jane@example.com",
        null, null, null, null,
        DateTime.UtcNow, DateTime.UtcNow);

    private FieldValueHistoryDto BuildHistory() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _customerId,
        "Email", "old@example.com", "System", DateTime.UtcNow, null);
}
