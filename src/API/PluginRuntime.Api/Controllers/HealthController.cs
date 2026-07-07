using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("/health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new
        {
            status = result.IsHealthy ? "Healthy" : "Unhealthy",
            checks = result.Checks
        };

        return result.IsHealthy ? Ok(response) : StatusCode(503, response);
    }

    [HttpGet("/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckReadinessAsync(cancellationToken);

        var response = new
        {
            status = result.IsHealthy ? "Ready" : "NotReady",
            checks = result.Checks
        };

        return result.IsHealthy ? Ok(response) : StatusCode(503, response);
    }
}
