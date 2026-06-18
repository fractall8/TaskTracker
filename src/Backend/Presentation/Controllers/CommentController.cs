using Application.Features.Comments.Commands;
using Application.Features.Comments.Queries;
using Contracts.DTOs;
using Contracts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("boards/{boardId:guid}/tasks/{taskId:guid}/comments")]
[Authorize]
public class CommentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetComments(
        [FromRoute] Guid boardId, 
        [FromRoute] Guid taskId, 
        CancellationToken ct)
    {
        var result = await sender.Send(new GetCommentsByTaskIdQuery(boardId, taskId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> AddComment(
        [FromRoute] Guid boardId, 
        [FromRoute] Guid taskId, 
        [FromBody] CreateCommentRequest request, 
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateCommentCommand(boardId, taskId, request.Text), ct);
        return Ok(result);
    }

    [HttpPut("{commentId}")]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        Guid boardId, 
        Guid taskId, 
        Guid commentId, 
        [FromBody] UpdateCommentRequest request, 
        CancellationToken ct)
    {
        var command = new UpdateCommentCommand(boardId, taskId, commentId, request.Text);
        var result = await sender.Send(command, ct);
        return Ok(result);
    }
    
    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] Guid boardId, 
        [FromRoute] Guid taskId, 
        [FromRoute] Guid commentId, 
        CancellationToken ct)
    {
        await sender.Send(new DeleteCommentCommand(boardId, taskId, commentId), ct);
        return NoContent();
    }
}