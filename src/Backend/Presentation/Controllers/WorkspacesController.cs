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
[Route("workspaces")]
public class WorkspacesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WorkspaceDto>>> GetUserWorkspaces(CancellationToken ct)
    {
        var result = await sender.Send(new GetUserWorkspacesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> CreateWorkspace([FromBody] CreateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateWorkspaceCommand(request.Name, request.Description), ct);
        return Ok();
    }

    [HttpPut("{workspaceId:guid}")]
    public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] UpdateWorkspaceRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateWorkspaceCommand(workspaceId, request.Name, request.Description), ct);
        return NoContent();
    }

    [HttpDelete("{workspaceId:guid}")]
    public async Task<IActionResult> DeleteWorkspace(Guid workspaceId, CancellationToken ct)
    {
        await sender.Send(new DeleteWorkspaceCommand(workspaceId), ct);
        return NoContent();
    }

    [HttpGet("{workspaceId:guid}/boards")]
    public async Task<ActionResult<List<BoardPreviewDto>>> GetWorkspaceBoards(Guid workspaceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceBoardsQuery(workspaceId), ct);
        return Ok(result);
    }
}
