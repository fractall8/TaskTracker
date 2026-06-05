namespace Presentation.Controllers;

using Application.Features.Auth.Commands;
using Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/auth")]
[ApiController]
public class AuthController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost("login")]
    public async Task<IActionResult> Login(CancellationToken ct)
    {
        var data = await sender.Send(new LoginCommand(), ct);
        
        return Ok(data);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var data = await sender.Send(new GetCurrentUserQuery(), ct);
        
        if (data == null)
        {
            return Unauthorized(); 
        }

        return Ok(data);
    }
}