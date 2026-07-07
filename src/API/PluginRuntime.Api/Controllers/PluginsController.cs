using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Models;
using PluginRuntime.Core.Interfaces;

namespace PluginRuntime.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PluginsController : ControllerBase
{
    private readonly IPluginVersionRepository _versionRepository;
    private const long MaxUploadSize = 50L * 1024 * 1024; // 50 MB

    public PluginsController(IPluginVersionRepository versionRepository)
    {
        _versionRepository = versionRepository;
    }

    [HttpGet]
    public IActionResult List()
    {
        // TODO: Implement with IPluginRepository when available
        return Ok(Array.Empty<object>());
    }

    [HttpGet("{pluginId:guid}")]
    public IActionResult Get([FromRoute] Guid pluginId)
    {
        // TODO: Implement with IPluginRepository when available
        return Ok(new { pluginId });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ErrorResponse(new ErrorDetail(
                "INVALID_FILE", "Validation", "No file uploaded.", HttpContext.TraceIdentifier, DateTime.UtcNow.ToString("O"))));

        if (file.Length > MaxUploadSize)
            return BadRequest(new ErrorResponse(new ErrorDetail(
                "FILE_TOO_LARGE", "Validation", "File exceeds maximum size of 50 MB.", HttpContext.TraceIdentifier, DateTime.UtcNow.ToString("O"))));

        // Validate ZIP format (check magic bytes PK\x03\x04)
        using var stream = file.OpenReadStream();
        var header = new byte[4];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, 4), cancellationToken);
        if (bytesRead < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            return BadRequest(new ErrorResponse(new ErrorDetail(
                "INVALID_ZIP", "Validation", "File is not a valid ZIP archive.", HttpContext.TraceIdentifier, DateTime.UtcNow.ToString("O"))));

        // Accept and return 202 with pluginVersionId
        var pluginVersionId = Guid.NewGuid();
        return Accepted(new { pluginVersionId, status = "Scanning" });
    }

    [HttpPost("{pluginId:guid}/reload")]
    public IActionResult Reload([FromRoute] Guid pluginId)
    {
        // TODO: Delegate to HotReloadManager
        return Ok(new { pluginId, status = "Reloading" });
    }

    [HttpPost("{pluginId:guid}/revoke")]
    public IActionResult Revoke([FromRoute] Guid pluginId)
    {
        // TODO: Delegate to revocation service
        return Ok(new { pluginId, status = "Revoked" });
    }
}
