using Application.Features.Boards.Commands;
using Application.Features.Boards.Queries;
using Contracts.DTOs;
using Contracts.Requests.Boards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class BoardsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardWithColumnsDto>> GetBoardById(Guid id, [FromQuery] string? searchTerm, CancellationToken ct)
    {
        var query = new GetBoardByIdQuery(id, searchTerm);
        var result = await sender.Send(query, ct);
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

    [HttpDelete("{boardId:guid}/leave")]
    public async Task<IActionResult> LeaveBoard(Guid boardId, CancellationToken ct)
    {
        await sender.Send(new LeaveBoardCommand(boardId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/archive/export")]
    public async Task<IActionResult> ArchiveAndExport(Guid id, [FromBody] BoardExportOptionsDto exportOptions, CancellationToken ct)
    {
        var result = await sender.Send(new ArchiveAndExportBoardCommand(id, exportOptions), ct);

        return Accepted(result);
    }
}
