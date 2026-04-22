using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Controllers;

[Route("api/customers/{customerId:guid}/values")]
[ApiController]
[Produces("application/json")]
public class CustomerFieldValuesController(IFieldValueService service, ILogger<CustomerFieldValuesController> log) : ControllerBase
{
    private readonly IFieldValueService _service = service;
    private readonly ILogger<CustomerFieldValuesController> _log = log;



    /// <summary>
    /// Get all customer data values.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <returns>IEnumberable of type FieldValueHistoryDto</returns>
    [HttpGet]
    [EndpointSummary("CustomerFieldValues GET all per customer")]
    [ProducesResponseType(typeof(IEnumerable<FieldValueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid customerId)
    {
        _log.LogInformation($"Get all per customer: {customerId}");
        var results = await _service.GetByCustomerIdAsync(customerId);
        return Ok(results);
    }


    /// <summary>
    /// Get history of customer data values.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="page">int</param>
    /// <param name="pageSize">int</param>
    /// <returns>IEnumberable of type FieldValueHistoryDto</returns>
    [HttpGet("history")]
    [EndpointSummary("CustomerFieldValues GET History per customer")]
    [ProducesResponseType(typeof(IEnumerable<FieldValueHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(Guid customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        _log.LogInformation($"Get history per customer: {customerId}");
        var results = await _service.GetHistoryByCustomerAsync(customerId, page, pageSize);
        return Ok(results);
    }



    /// <summary>
    /// Get history of customer data for specified field.
    /// </summary>
    /// <param name="customerId">Guid</param>
    /// <param name="fieldId">Guid</param>
    /// <returns>IEnumberable of type FieldValueHistoryDto</returns>
    [HttpGet("{fieldId:guid}/history")]
    [EndpointSummary("CustomerFieldValues GET History per field")]
    [ProducesResponseType(typeof(IEnumerable<FieldValueHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFieldHistory(Guid customerId, Guid fieldId)
    {
        _log.LogInformation($"Get history of field per customer: {customerId} - field: {fieldId}");
        var results = await _service.GetHistoryByFieldAsync(customerId, fieldId);
        return Ok(results);
    }




}
