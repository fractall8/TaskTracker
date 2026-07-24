using Application.Features.Subscriptions.Commands;
using Application.Features.Subscriptions.Queries;
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
    [HttpGet("entitlements")]
    public async Task<IActionResult> GetEntitlements(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var query = new GetWorkspaceEntitlementsQuery(workspaceId);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var query = new GetSubscriptionPlansQuery(workspaceId);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var query = new GetWorkspaceSubscriptionQuery(workspaceId);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateCheckoutSessionRequest request,
        CancellationToken ct)
    {
        var command = new CreateCheckoutSessionCommand(workspaceId, request.PriceId);

        var result = await sender.Send(command, ct);

        return Ok(result);
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortalSession(
        [FromRoute] Guid workspaceId,
        CancellationToken ct)
    {
        var command = new CreateCustomerPortalSessionCommand(workspaceId);
        var result = await sender.Send(command, ct);

        return Ok(result);
    }
}
