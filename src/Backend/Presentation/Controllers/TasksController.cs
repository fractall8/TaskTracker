using Application.Features.Attachments.Commands;
using Application.Features.Attachments.Queries;
using Application.Features.Tasks.Commands;
using Application.Features.Tasks.Queries;
using Contracts.DTOs;
using Contracts.Requests.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("boards/{boardId:guid}/tasks")]
[Authorize]
public class TasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAllForBoard(
        [FromRoute] Guid boardId,
        CancellationToken ct)
    {
        var query = new GetTasksByBoardIdQuery(boardId);
        var result = await sender.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        CancellationToken ct)
    {
        var query = new GetTaskByIdQuery(boardId, taskId);
        var result = await sender.Send(query, ct);
        return Ok(result);
    }

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
        var command = new UpdateTaskDetailsCommand(
            boardId,
            taskId,
            request.Title,
            request.Description,
            request.DueDate,
            request.AssigneeId);

        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    [HttpPatch("{taskId:guid}/due-date")]
    public async Task<ActionResult<TaskDto>> UpdateDueDate(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskDueDateRequest request,
        CancellationToken ct)
    {
        var command = new UpdateTaskDueDateCommand(boardId, taskId, request.DueDate);
        var result = await sender.Send(command, ct);

        return Ok(result);
    }

    [HttpPost("{taskId:guid}/complete")]
    public async Task<ActionResult<TaskDto>> Complete(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        CancellationToken ct)
    {
        var result = await sender.Send(new CompleteTaskCommand(boardId, taskId), ct);

        return Ok(result);
    }

    [HttpPost("{taskId:guid}/reopen")]
    public async Task<ActionResult<TaskDto>> Reopen(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        CancellationToken ct)
    {
        var result = await sender.Send(new ReopenTaskCommand(boardId, taskId), ct);

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
    public async Task<ActionResult<AttachmentDto>> UploadAttachment(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or was not provided.");
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadAttachmentCommand(
            BoardId: boardId,
            TaskId: taskId,
            FileStream: stream,
            FileName: file.FileName,
            ContentType: file.ContentType,
            SizeInBytes: file.Length
        );

        var attachmentDto = await sender.Send(command, ct);

        return Ok(attachmentDto);
    }

    [HttpGet("{taskId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<ActionResult<AttachmentDownloadDto>> GetAttachmentDownloadUrl(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        [FromRoute] Guid attachmentId,
        CancellationToken ct)
    {
        var query = new GetAttachmentDownloadQuery(boardId, taskId, attachmentId);
        var result = await sender.Send(query, ct);
        return Ok(result);
    }

    [HttpDelete("{taskId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(
        [FromRoute] Guid boardId,
        [FromRoute] Guid taskId,
        [FromRoute] Guid attachmentId,
        CancellationToken ct)
    {
        var command = new DeleteAttachmentCommand(boardId, taskId, attachmentId);
        await sender.Send(command, ct);
        return NoContent();
    }
}
