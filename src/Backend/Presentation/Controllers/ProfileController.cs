using Application.Features.Profile.Commands;
using Application.Features.Profile.Queries;
using Contracts.DTOs;
using Contracts.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("profile")]
[Authorize]
public class ProfileController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetProfileQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateProfileCommand(request.DisplayName), ct);
        return NoContent();
    }

    [HttpPost("avatar")]
    public async Task<ActionResult<string>> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadAvatarCommand(stream, file.FileName, file.ContentType);
        var newUrl = await sender.Send(command, ct);

        return Ok(new { Url = newUrl });
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        await sender.Send(new DeleteAvatarCommand(), ct);
        return NoContent();
    }
}
