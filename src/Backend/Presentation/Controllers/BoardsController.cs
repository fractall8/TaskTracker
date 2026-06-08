using Application.Features.Boards.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class BoardsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateBoard(
        [FromBody] CreateBoardCommand command, 
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        
        return Ok(result); 
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBoard(
        Guid id, 
        CancellationToken ct)
    {
        await sender.Send(new DeleteBoardCommand(id), ct);
        
        return NoContent(); 
    }
}