using Application.Features.Files.Commands;
using Application.Features.Tasks.Commands;
using Contracts.DTOs;
using Contracts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("boards/{boardId:guid}/tasks")]
[Authorize]
public class TasksController(ISender sender) : ControllerBase
{
    [HttpPost("columns/{columnId:guid}")]
    public async Task<ActionResult<TaskDto>> Create(
        [FromRoute] Guid boardId,
        [FromRoute] Guid columnId,
        [FromBody] CreateTaskRequest request,
        CancellationToken ct)
    {
        var command = new CreateTaskCommand(
            boardId,
            columnId,
            request.Title,
            request.Description,
            request.DueDate,
            request.AssigneeId);

        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> Update(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken ct)
    {
        var command = new UpdateTaskCommand(
            boardId,
            taskId,
            request.Title,
            request.Description,
            request.DueDate,
            request.AssigneeId,
            request.ColumnId);

        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid boardId, [FromRoute] Guid taskId, CancellationToken ct)
    {
        var command = new DeleteTaskCommand(boardId, taskId);
        await sender.Send(command, ct);

        return NoContent();
    }

    [HttpPost("{taskId:guid}/move")]
    public async Task<IActionResult> Move(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        [FromBody] MoveTaskRequest request,
        CancellationToken ct)
    {
        var command = new MoveTaskCommand(boardId, taskId, request.TargetColumnId, request.NewPosition);
        await sender.Send(command, ct);

        return Ok();
    }

    [HttpPost("{taskId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment([FromRoute] Guid boardId, [FromRoute] Guid taskId,
        IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or was not provided.");
        }

        // For now taskId is not provided to command because we need to create new table for it
        // This will be done when we have basic crud for tasks
        await using var stream = file.OpenReadStream();

        var command = new UploadAttachmentCommand(stream, file.FileName, file.ContentType);
        var fileUrl = await sender.Send(command, cancellationToken);

        return Ok(new { Url = fileUrl });
    }
}