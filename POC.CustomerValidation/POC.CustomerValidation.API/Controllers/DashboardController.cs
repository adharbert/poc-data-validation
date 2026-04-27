using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

/// <summary>
/// Provides summary statistics for the admin dashboard.
/// Warning threshold (days before project end date) is configured via
/// DashboardSettings:WarningDaysThreshold in appsettings.json.
/// </summary>
[Route("api/dashboard")]
[ApiController]
public class DashboardController(IDashboardService service, IConfiguration config, ILogger<DashboardController> log) : ControllerBase
{
    private readonly IDashboardService _service = service;
    private readonly IConfiguration    _config  = config;
    private readonly ILogger<DashboardController> _log = log;

    private int WarningDays => _config.GetValue<int>("DashboardSettings:WarningDaysThreshold", 30);

    /// <summary>
    /// Returns aggregate stats across all active organisations including customer counts,
    /// verification progress, active projects, and projects approaching end date.
    /// </summary>
    [HttpGet("stats")]
    [EndpointSummary("Dashboard — summary statistics")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        _log.LogInformation("Dashboard stats requested (warningDays={WarningDays})", WarningDays);
        var result = await _service.GetStatsAsync(WarningDays);
        return Ok(result);
    }

    /// <summary>
    /// Returns active projects whose MarketingEndDate falls within the configured warning window.
    /// Default window is 30 days, controlled by DashboardSettings:WarningDaysThreshold.
    /// </summary>
    [HttpGet("expiring-projects")]
    [EndpointSummary("Dashboard — projects approaching end date")]
    [ProducesResponseType(typeof(IEnumerable<ExpiringProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringProjects()
    {
        _log.LogInformation("Expiring projects requested (warningDays={WarningDays})", WarningDays);
        var result = await _service.GetExpiringProjectsAsync(WarningDays);
        return Ok(result);
    }
}
