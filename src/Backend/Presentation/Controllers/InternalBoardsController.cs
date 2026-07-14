using Application.Features.Boards.Queries;
using Application.Interfaces.Notifiers;
using Contracts.DTOs;
using Contracts.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Route("internal/boards")]
[ApiController]
public class InternalBoardsController(
    ISender sender,
    IBoardExportStatusNotifier exportStatusNotifier) : ControllerBase
{
    [HttpPost("{id:guid}/export-data")]
    public async Task<IActionResult> GetExportData(
        Guid id,
        [FromBody] BoardExportOptionsDto exportOptions,
        CancellationToken ct)
    {
        var data = await sender.Send(new GetBoardExportDataQuery(id, exportOptions), ct);

        if (data == null)
        {
            return NotFound($"Board with ID {id} was not found.");
        }

        return Ok(data);
    }

    [HttpPost("{id:guid}/export-status-notify")]
    public async Task<IActionResult> NotifyExportStatusChanged(
        Guid id,
        [FromBody] BoardExportStatusChangedNotification notification,
        CancellationToken ct)
    {
        if (id != notification.BoardId)
        {
            return BadRequest("Board ID mismatch.");
        }

        await exportStatusNotifier.NotifyExportStatusChangedAsync(notification, ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/re-export-status-notify")]
    public async Task<IActionResult> NotifyReExportStatusChanged(
        Guid id,
        [FromBody] BoardExportStatusChangedNotification notification,
        CancellationToken ct)
    {
        if (id != notification.BoardId)
        {
            return BadRequest("Board ID mismatch.");
        }

        await exportStatusNotifier.NotifyReExportStatusChangedAsync(notification, ct);

        return NoContent();
    }
}
