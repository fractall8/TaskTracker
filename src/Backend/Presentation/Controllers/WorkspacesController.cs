using Application.Features.Boards.Commands;
using Application.Features.Workspaces.Commands;
using Application.Features.Workspaces.Queries;
using Contracts.DTOs;
using Contracts.Requests.Boards;
using Contracts.Requests.Workspaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("workspaces")]
public class WorkspacesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WorkspaceDto>>> GetUserWorkspaces(CancellationToken ct)
    {
        var result = await sender.Send(new GetUserWorkspacesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{workspaceId:guid}")]
    public async Task<ActionResult<WorkspaceDetailsDto>> GetWorkspaceById(Guid workspaceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceByIdQuery(workspaceId), ct);
        return Ok(result);
    }

    [HttpGet("{workspaceId:guid}/boards/my")]
    public async Task<ActionResult<PagedList<BoardPreviewDto>>> GetMyWorkspaceBoards(
        Guid workspaceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyWorkspaceBoardsQuery(workspaceId, pageNumber, pageSize, searchTerm), ct);
        return Ok(result);
    }

    [HttpGet("{workspaceId:guid}/boards/all")]
    public async Task<ActionResult<PagedList<BoardPreviewDto>>> GetAllWorkspaceBoards(
        Guid workspaceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllWorkspaceBoardsQuery(workspaceId, pageNumber, pageSize, searchTerm), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateWorkspaceCommand(request.Name, request.Description), ct);
        return Ok(result);
    }

    [HttpPut("{workspaceId:guid}")]
    public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] UpdateWorkspaceRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateWorkspaceCommand(workspaceId, request.Name, request.Description), ct);
        return NoContent();
    }

    [HttpPost("{workspaceId:guid}/boards")]
    public async Task<ActionResult<BoardDto>> CreateBoard(
        Guid workspaceId,
        [FromBody] CreateBoardRequest request,
        CancellationToken ct)
    {
        var command = new CreateBoardCommand(workspaceId, request.Name, request.Description);
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{workspaceId:guid}")]
    public async Task<IActionResult> DeleteWorkspace(Guid workspaceId, CancellationToken ct)
    {
        await sender.Send(new DeleteWorkspaceCommand(workspaceId), ct);
        return NoContent();
    }
}
