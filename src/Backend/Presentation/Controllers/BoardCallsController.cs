using Application.Features.BoardCalls.Commands;
using Application.Features.BoardCalls.Queries;
using Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("boards/{boardId:guid}/calls")]
[Authorize]
public class BoardCallsController(ISender sender) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<BoardCallDto?>> GetActive([FromRoute] Guid boardId, CancellationToken ct)
    {
        var query = new GetActiveBoardCallQuery(boardId);
        var result = await sender.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("start")]
    public async Task<ActionResult<StartOrJoinBoardCallResponse>> Start([FromRoute] Guid boardId, CancellationToken ct)
    {
        var command = new StartBoardCallCommand(boardId);
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("join")]
    public async Task<ActionResult<StartOrJoinBoardCallResponse>> Join([FromRoute] Guid boardId, CancellationToken ct)
    {
        var command = new JoinBoardCallCommand(boardId);
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromRoute] Guid boardId, CancellationToken ct)
    {
        var command = new LeaveBoardCallCommand(boardId);
        await sender.Send(command, ct);
        return NoContent();
    }

    [HttpPost("end")]
    public async Task<IActionResult> End([FromRoute] Guid boardId, CancellationToken ct)
    {
        var command = new EndBoardCallCommand(boardId);
        await sender.Send(command, ct);
        return NoContent();
    }
}
