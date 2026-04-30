using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;



[Route("api/[controller]")]
[ApiController]
public class OrganizationsController(IOrganizationServices services, ILogger<OrganizationsController> log) : ControllerBase
{
    private readonly IOrganizationServices _services = services;
    private readonly ILogger<OrganizationsController> _log = log;



    /// <summary>
    /// Get call to get all organizations, by default is only by active, can also include inactive.
    /// </summary>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    [HttpGet]
    [EndpointSummary("Organizations Get All")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] string? search = null)
    {
        _log.LogInformation("Request for OrganizationsController: GetAll");
        var results = await _services.GetAllAsync(includeInactive, search);
        return Ok(results);
    }



    /// <summary>
    /// Get call by organization Id
    /// </summary>
    /// <param name="organizationId"></param>
    /// <returns></returns>
    [HttpGet("{organizationId:guid}")]
    [EndpointSummary("Organization Get by Id")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid organizationId)
    {
        _log.LogInformation($"Request for OrganizationController: GetById. ID = {organizationId}");
        var result = await _services.GetByIdAsync(organizationId);
        return Ok(result);
    }



    /// <summary>
    /// Post create new organization.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("Organization Create")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
    {
        _log.LogInformation($"Created organization request for '{request.OrganizationName}'.");
        var org = await _services.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { organizationId = org.OrganizationId}, org);
    }



    /// <summary>
    /// Put call to update organization.
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{organizationId:guid}")]
    [EndpointSummary("Organization Update")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid organizationId, [FromBody] UpdateOrganizationRequest request)
    {
        _log.LogInformation($"Updated organization request for '{request.OrganizationName}'.");
        var org = await _services.UpdateAsync(organizationId, request);
        return Ok(org);
    }




    /// <summary>
    /// Put call to update status.
    /// </summary>
    /// <param name="organizationId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    [HttpPut("{organizationId:guid}/status/{status:bool}")]
    [EndpointSummary("Organization set status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid organizationId, bool status)
    {
        _log.LogInformation($"Setting status for organization Id {organizationId} setting status to '{status}'");
        await _services.ChangeStatus(organizationId, status);
        return NoContent();
    }


}
