using Application.Features.FaqChat.Commands;
using Contracts.DTOs;
using Contracts.Requests.FaqChat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Presentation.Extensions;

namespace Presentation.Controllers;

[ApiController]
[Route("faq-chat")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.FaqChatPolicy)]
public class FaqChatController(ISender sender) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<FaqAnswerDto>> Ask(
        [FromBody] AskFaqQuestionRequest request,
        CancellationToken ct)
    {
        var command = new AskFaqQuestionCommand(request.Question, request.History);

        var result = await sender.Send(command, ct);

        return Ok(result);
    }
}
