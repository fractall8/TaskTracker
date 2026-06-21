using Application.Features.Workspaces.Commands;
using Application.Features.Workspaces.Queries;
using Contracts.DTOs;
using Contracts.Requests;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("workspaces/{workspaceId:guid}")]
public class WorkspaceMembersController(ISender sender) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetWorkspaceUsers(Guid workspaceId, [FromQuery] string? searchTerm, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceUsersQuery(workspaceId, searchTerm), ct);
        return Ok(result);
    }

    [HttpGet("users/search-all")]
    public async Task<ActionResult<List<UserSearchDto>>> SearchUsersNotInWorkspace(Guid workspaceId, [FromQuery] string? searchTerm, CancellationToken ct)
    {
        var result = await sender.Send(new SearchUsersNotInWorkspaceQuery(workspaceId, searchTerm), ct);
        return Ok(result);
    }

    [HttpDelete("members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        await sender.Send(new RemoveWorkspaceMemberCommand(workspaceId, userId), ct);
        return NoContent();
    }

    [HttpPut("members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(Guid workspaceId, Guid userId, [FromBody] ChangeMemberRoleRequest request, CancellationToken ct)
    {
        await sender.Send(new ChangeWorkspaceMemberRoleCommand(workspaceId, userId, (WorkspaceRole)request.Role), ct);
        return NoContent();
    }
}
