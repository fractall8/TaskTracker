using Application.Features.Workspaces.Commands;
using Application.Features.Workspaces.Queries;
using Contracts.DTOs;
using Contracts.Requests.Workspaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("workspaces")]
public class WorkspaceInvitesController(ISender sender) : ControllerBase
{
    [HttpPost("{workspaceId:guid}/invites")]
    public async Task<ActionResult<InviteResultDto>> InviteUser(Guid workspaceId, [FromBody] InviteUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new InviteUserToWorkspaceCommand(workspaceId), ct);
        return Ok(result);
    }

    [HttpPost("invites/accept")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        await sender.Send(new AcceptWorkspaceInviteCommand(request.Token), ct);
        return Ok();
    }

    [HttpGet("{workspaceId:guid}/invites")]
    public async Task<ActionResult<List<WorkspaceInviteDto>>> GetInvites(Guid workspaceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceInvitesQuery(workspaceId), ct);
        return Ok(result);
    }

    [HttpPut("{workspaceId:guid}/invites/{inviteId:guid}/expiration")]
    public async Task<IActionResult> UpdateExpiration(Guid workspaceId, Guid inviteId, [FromBody] UpdateInviteExpirationRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateInviteExpirationCommand(workspaceId, inviteId, request.NewExpirationDate), ct);
        return NoContent();
    }

    [HttpDelete("{workspaceId:guid}/invites/{inviteId:guid}")]
    public async Task<IActionResult> RevokeInvite(Guid workspaceId, Guid inviteId, CancellationToken ct)
    {
        await sender.Send(new RevokeInviteCommand(workspaceId, inviteId), ct);
        return NoContent();
    }
}
