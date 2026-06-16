using Application.Features.Boards.Queries;
using Application.Features.Columns.Commands;
using Contracts.DTOs;
using Contracts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("boards/{boardId:guid}/columns")]
[Authorize]
public class ColumnsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ColumnDto>> CreateColumn(
        [FromRoute] Guid boardId,
        [FromBody] CreateColumnRequest request,
        CancellationToken ct)
    {
        var command = new CreateColumnCommand(boardId, request.Name);
        
        var result = await sender.Send(command, ct);
        
        return Ok(result); 
    }

    [HttpPut("{columnId:guid}")]
    public async Task<ActionResult<ColumnDto>> UpdateColumn(
        [FromRoute] Guid boardId,
        [FromRoute] Guid columnId,
        [FromBody] UpdateColumnRequest request,
        CancellationToken ct)
    {
        var command = new UpdateColumnCommand(boardId, columnId, request.Name);
        
        var result = await sender.Send(command, ct);
        
        return Ok(result);
    }
    
    [HttpDelete("{columnId:guid}")]
    public async Task<IActionResult> DeleteColumn(
        [FromRoute] Guid boardId,
        [FromRoute] Guid columnId,
        CancellationToken ct)
    {
        var command = new DeleteColumnCommand(boardId, columnId);
        
        await sender.Send(command, ct);
        
        return NoContent();
    }
    
    [HttpGet]
    public async Task<ActionResult<BoardWithColumnsDto>> GetBoardWithColumns(
        [FromRoute] Guid boardId,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetBoardByIdQuery(boardId), ct);
        return Ok(result);
    }

    [HttpPut("{columnId:guid}/move")]
    public async Task<IActionResult> MoveColumn(
        [FromRoute] Guid boardId,
        [FromRoute] Guid columnId,
        [FromBody] int newPosition,
        CancellationToken ct)
    {
        var command = new MoveColumnCommand(boardId, columnId, newPosition);
        
        await sender.Send(command, ct);

        return NoContent();
    }
}