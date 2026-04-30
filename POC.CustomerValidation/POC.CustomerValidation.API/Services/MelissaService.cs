using POC.CustomerValidation.API.Interfaces;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Services;

/// <summary>
/// Stub implementation — replace with real Melissa REST API calls once
/// credentials and subscription are available.
/// Always returns IsValid=false so MelissaValidated stays false until wired up.
/// </summary>
public class MelissaService(ILogger<MelissaService> log) : IMelissaService
{
    private readonly ILogger<MelissaService> _log = log;

    public Task<MelissaValidationResult> ValidateAsync(
        string addressLine1, string? addressLine2,
        string city, string state, string postalCode, string country = "US")
    {
        _log.LogInformation(
            "Melissa validation stub called for {AddressLine1}, {City} {State} {PostalCode} — returning unvalidated",
            addressLine1, city, state, postalCode);

        return Task.FromResult(new MelissaValidationResult
        {
            IsValid      = false,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City         = city,
            State        = state,
            PostalCode   = postalCode,
        });
    }
}
