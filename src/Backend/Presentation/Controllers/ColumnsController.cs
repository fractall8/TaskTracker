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
        var command = new UpdateColumnCommand(columnId, request.Name);
        
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
}