using Application.Features.Subscriptions.Commands;
using Contracts.Requests.Subscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Authorize]
[Route("workspaces/{workspaceId:guid}/subscriptions")]
public class WorkspaceSubscriptionsController(ISender sender) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateCheckoutSessionRequest request,
        CancellationToken ct)
    {
        var command = new CreateCheckoutSessionCommand(workspaceId, request.PriceId);

        var checkoutUrl = await sender.Send(command, ct);

        return Ok(new { url = checkoutUrl });
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortalSession(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var command = new CreateCustomerPortalSessionCommand(workspaceId);
        var portalUrl = await sender.Send(command, ct);

        return Ok(new { url = portalUrl });
    }
}
