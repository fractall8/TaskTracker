using Application.Features.Boards.Commands;
using Application.Features.Boards.Queries;
using Contracts.DTOs;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("boards/{boardId:guid}/members")]
public class BoardMembersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMembers(
        [FromRoute] Guid boardId,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetBoardMembersQuery(boardId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(
        [FromRoute] Guid boardId,
        [FromBody] AddBoardMemberRequest request,
        CancellationToken ct)
    {
        var command = new AddBoardMemberCommand(boardId, request.WorkspaceMemberId, (BoardRole)request.Role);
        await sender.Send(command, ct);

        return Ok();
    }

    [HttpPut("{workspaceMemberId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(
        [FromRoute] Guid boardId,
        [FromRoute] Guid workspaceMemberId,
        [FromBody] UpdateBoardMemberRoleRequest request,
        CancellationToken ct)
    {
        var command = new UpdateBoardMemberRoleCommand(boardId, workspaceMemberId, (BoardRole)request.Role);
        await sender.Send(command, ct);

        return Ok();
    }

    [HttpDelete("{workspaceMemberId:guid}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid boardId,
        [FromRoute] Guid workspaceMemberId,
        CancellationToken ct)
    {
        var command = new RemoveBoardMemberCommand(boardId, workspaceMemberId);
        await sender.Send(command, ct);

        return NoContent();
    }
}
