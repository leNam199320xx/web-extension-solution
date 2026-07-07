using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PluginRuntime.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] string? status = "Pending")
    {
        // TODO: Implement with IApprovalRepository
        return Ok(Array.Empty<object>());
    }

    [HttpPost("{versionId:guid}/approve")]
    public IActionResult Approve([FromRoute] Guid versionId, [FromBody] ApprovalRequest? body)
    {
        // TODO: Implement approval logic
        return Ok(new { versionId, decision = "Approved", comment = body?.Comment });
    }

    [HttpPost("{versionId:guid}/reject")]
    public IActionResult Reject([FromRoute] Guid versionId, [FromBody] ApprovalRequest? body)
    {
        // TODO: Implement rejection logic
        return Ok(new { versionId, decision = "Rejected", comment = body?.Comment });
    }

    [HttpGet("{versionId:guid}/permissions")]
    public IActionResult GetPermissions([FromRoute] Guid versionId)
    {
        // TODO: Implement permission review
        return Ok(new { versionId, permissions = Array.Empty<object>() });
    }
}

public record ApprovalRequest
{
    public string? Comment { get; init; }
    public string? Decision { get; init; }
}
