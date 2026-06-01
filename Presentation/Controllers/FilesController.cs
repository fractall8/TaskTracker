using Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController(IMediator mediator) : ControllerBase
{
    private const int MaxFileSize = 5 * 1024 * 1024; // 5 mb
    
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or was not provided.");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File is too large. Maximum allowed file size is {MaxFileSize / (1024*1024)} MB");
        }

        using var memoryStream = new MemoryStream();
    
        await file.CopyToAsync(memoryStream, cancellationToken);
    
        memoryStream.Position = 0; 
    
        var command = new UploadFileCommand(memoryStream, file.FileName, file.ContentType);
        var fileUrl = await mediator.Send(command, cancellationToken);

        return Ok(new { Url = fileUrl });
    }
}