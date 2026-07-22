using System.Text;
using Application.Features.Subscriptions.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Route("webhooks")]
[ApiController]
[AllowAnonymous]
public class WebhookController(ISender sender) : ControllerBase
{
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await sender.Send(new HandleStripeWebhookCommand(payload, signature), ct);

        return Ok();
    }
}
