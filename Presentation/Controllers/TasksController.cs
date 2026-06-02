using Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(IMediator mediator) : ControllerBase
{
    [HttpPost("{taskId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(Guid taskId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or was not provided.");
        }
        
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0; 
    
        // For now taskId is not provided to command because we need to create new table for it
        // This will be done when we have basic crud for tasks
        var command = new UploadAttachmentCommand(memoryStream, file.FileName, file.ContentType);
        var fileUrl = await mediator.Send(command, cancellationToken);

        return Ok(new { Url = fileUrl });
    }
}