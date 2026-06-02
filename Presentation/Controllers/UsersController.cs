using Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost("avatars")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty or was not provided.");
        }
        
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0; 
    
        var command = new UploadAvatarCommand(memoryStream, file.FileName, file.ContentType);
        var fileUrl = await mediator.Send(command, cancellationToken);

        return Ok(new { Url = fileUrl });
    }
}