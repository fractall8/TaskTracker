using Application.Features.Boards.Commands;
using Application.Features.Boards.Queries;
using Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class BoardsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedList<BoardPreviewDto>>> GetBoards(CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null)
    {
        var result = await sender.Send(new GetBoardsQuery(pageNumber, pageSize, searchTerm), ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> CreateBoard(
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BoardPreviewDto>> UpdateBoard(
        Guid id,
        [FromBody] UpdateBoardRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateBoardCommand(id, request.Name, request.Description), ct);

        return Ok(result);
    }
}