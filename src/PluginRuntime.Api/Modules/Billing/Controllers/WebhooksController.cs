using Microsoft.AspNetCore.Mvc;
using PluginRuntime.Api.Modules.Billing.Services;

namespace PluginRuntime.Api.Modules.Billing.Controllers;

/// <summary>
/// Handles incoming Stripe webhook events.
/// Always returns 200 OK — Stripe expects this even on processing errors.
/// </summary>
[ApiController]
[Route("api/billing/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _webhookService.ProcessAsync(payload, signature, ct);

        // Stripe expects 200 even on processing errors
        return Ok();
    }
}
