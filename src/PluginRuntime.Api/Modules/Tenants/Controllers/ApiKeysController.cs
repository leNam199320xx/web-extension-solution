using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Tenants.DTOs;
using PluginRuntime.Api.Modules.Tenants.Services;

namespace PluginRuntime.Api.Modules.Tenants.Controllers;

/// <summary>
/// API controller for API key generation, revocation, and listing.
/// </summary>
[ApiController]
[Route("api/tenants/{tenantId:guid}/keys")]
public sealed class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>Generates a new API key for the specified tenant.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyGenerationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(
        Guid tenantId,
        [FromBody] GenerateApiKeyRequest? request,
        CancellationToken ct)
    {
        var result = await _apiKeyService.GenerateAsync(tenantId, request?.Name, request?.ExpiresAt, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Revokes an active API key.</summary>
    [HttpDelete("{keyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid tenantId,
        Guid keyId,
        CancellationToken ct)
    {
        await _apiKeyService.RevokeAsync(tenantId, keyId, ct);
        return NoContent();
    }

    /// <summary>Lists all API keys for the specified tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        Guid tenantId,
        CancellationToken ct)
    {
        var keys = await _apiKeyService.ListAsync(tenantId, ct);
        return Ok(keys);
    }
}

/// <summary>
/// Request body for generating a new API key.
/// </summary>
public sealed record GenerateApiKeyRequest
{
    /// <summary>Optional human-readable name for the API key.</summary>
    public string? Name { get; init; }

    /// <summary>Optional expiration date for the API key.</summary>
    public DateTime? ExpiresAt { get; init; }
}
