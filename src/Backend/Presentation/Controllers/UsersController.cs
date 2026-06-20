using Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("[controller]")]
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

        await using var stream = file.OpenReadStream();

        var command = new UploadAvatarCommand(stream, file.FileName, file.ContentType);
        var fileUrl = await mediator.Send(command, cancellationToken);

        return Ok(new { Url = fileUrl });
    }
}
