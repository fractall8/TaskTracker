using Application.Features.Workspaces.Commands;
using Contracts.DTOs;
using Contracts.Requests;
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
        var result = await sender.Send(new InviteUserToWorkspaceCommand(workspaceId, request.Email), ct);
        return Ok(result);
    }

    [HttpPost("invites/accept")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        await sender.Send(new AcceptWorkspaceInviteCommand(request.Token), ct);
        return Ok();
    }
}
