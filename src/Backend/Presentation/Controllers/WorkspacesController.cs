using Application.Features.Workspaces.Commands;
using Application.Features.Workspaces.Queries;
using Contracts.DTOs;
using Contracts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WorkspacesController(ISender sender) : ControllerBase
{
    [HttpGet("{workspaceId:guid}/users")]
    public async Task<ActionResult<List<UserDto>>> GetWorkspaceUsers(
        Guid workspaceId,
        [FromQuery] string? searchTerm,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceUsersQuery(workspaceId, searchTerm), ct);

        return Ok(result);
    }

    [HttpPost("{workspaceId:guid}/invites")]
    public async Task<ActionResult<InviteResultDto>> InviteUser(
        Guid workspaceId,
        [FromBody] InviteUserRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new InviteUserToWorkspaceCommand(workspaceId, request.Email), ct);

        return Ok(result);
    }

    [HttpPost("invites/accept")]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken ct)
    {
        await sender.Send(new AcceptWorkspaceInviteCommand(request.Token), ct);

        return Ok();
    }
}
