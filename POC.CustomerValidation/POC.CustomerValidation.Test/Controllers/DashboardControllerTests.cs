using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POC.CustomerValidation.API.Controllers;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.Test.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _svcMock = new();

    private DashboardController CreateSut(int? warningDays = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(warningDays.HasValue
                ? new Dictionary<string, string?> { ["DashboardSettings:WarningDaysThreshold"] = warningDays.Value.ToString() }
                : new Dictionary<string, string?>())
            .Build();

        return new DashboardController(_svcMock.Object, config, NullLogger<DashboardController>.Instance);
    }

    // ── GetStats ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsOk_WithDefaultWarningDays()
    {
        var stats = BuildStats();
        _svcMock.Setup(s => s.GetStatsAsync(30)).ReturnsAsync(stats);

        var result = await CreateSut().GetStats();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(stats, ok.Value);
    }

    [Fact]
    public async Task GetStats_ReturnsOk_WithCustomWarningDays()
    {
        var stats = BuildStats();
        _svcMock.Setup(s => s.GetStatsAsync(14)).ReturnsAsync(stats);

        var result = await CreateSut(warningDays: 14).GetStats();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(stats, ok.Value);
        _svcMock.Verify(s => s.GetStatsAsync(14), Times.Once);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task GetStats_UsesConfiguredWarningDays(int days)
    {
        _svcMock.Setup(s => s.GetStatsAsync(days)).ReturnsAsync(BuildStats());

        await CreateSut(warningDays: days).GetStats();

        _svcMock.Verify(s => s.GetStatsAsync(days), Times.Once);
    }

    [Fact]
    public async Task GetStats_DefaultsTo30_WhenConfigMissing()
    {
        _svcMock.Setup(s => s.GetStatsAsync(30)).ReturnsAsync(BuildStats());

        await CreateSut().GetStats();  // no warningDays in config

        _svcMock.Verify(s => s.GetStatsAsync(30), Times.Once);
    }

    // ── GetExpiringProjects ───────────────────────────────────────────────────

    [Fact]
    public async Task GetExpiringProjects_ReturnsOk_WithDefaultWarningDays()
    {
        var expiring = new List<ExpiringProjectDto> { BuildExpiring() };
        _svcMock.Setup(s => s.GetExpiringProjectsAsync(30)).ReturnsAsync(expiring);

        var result = await CreateSut().GetExpiringProjects();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expiring, ok.Value);
    }

    [Fact]
    public async Task GetExpiringProjects_ReturnsOk_WithCustomWarningDays()
    {
        _svcMock.Setup(s => s.GetExpiringProjectsAsync(7)).ReturnsAsync([]);

        var result = await CreateSut(warningDays: 7).GetExpiringProjects();

        Assert.IsType<OkObjectResult>(result);
        _svcMock.Verify(s => s.GetExpiringProjectsAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetExpiringProjects_ReturnsOk_WhenEmpty()
    {
        _svcMock.Setup(s => s.GetExpiringProjectsAsync(30)).ReturnsAsync([]);

        var result = await CreateSut().GetExpiringProjects();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((IEnumerable<ExpiringProjectDto>)ok.Value!);
    }

    [Fact]
    public async Task GetExpiringProjects_DefaultsTo30_WhenConfigMissing()
    {
        _svcMock.Setup(s => s.GetExpiringProjectsAsync(30)).ReturnsAsync([]);

        await CreateSut().GetExpiringProjects();

        _svcMock.Verify(s => s.GetExpiringProjectsAsync(30), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DashboardStatsDto BuildStats() => new()
    {
        TotalActiveOrganizations = 3,
        TotalActiveProjects      = 5,
        TotalCustomers           = 100,
        TotalVerifiedCustomers   = 80,
    };

    private static ExpiringProjectDto BuildExpiring() => new()
    {
        ProjectId        = 8001,
        ProjectName      = "Spring",
        OrganisationId   = Guid.NewGuid(),
        OrganisationName = "Acme",
        MarketingEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
        DaysUntilExpiry  = 5,
    };
}
