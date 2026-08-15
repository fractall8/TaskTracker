using Application.Features.Tags.Commands;
using Application.Features.Tags.Queries;
using Contracts.DTOs;
using Contracts.Requests.Tags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("workspaces/{workspaceId:guid}/tags")]
public class TagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetAll([FromRoute] Guid workspaceId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkspaceTagsQuery(workspaceId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> Create(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateTagRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateTagCommand(workspaceId, request.Name, request.Color), ct);
        return Ok(result);
    }

    [HttpPut("{tagId:guid}")]
    public async Task<ActionResult<TagDto>> Update(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid tagId,
        [FromBody] UpdateTagRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateTagCommand(workspaceId, tagId, request.Name, request.Color), ct);
        return Ok(result);
    }

    [HttpDelete("{tagId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid tagId,
        CancellationToken ct)
    {
        await sender.Send(new DeleteTagCommand(workspaceId, tagId), ct);
        return NoContent();
    }
}
