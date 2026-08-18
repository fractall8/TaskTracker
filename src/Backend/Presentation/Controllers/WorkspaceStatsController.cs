using Application.Features.Stats.Queries;
using Contracts.DTOs;
using Contracts.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("workspaces/{workspaceId:guid}/stats")]
public class WorkspaceStatsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WorkspaceStatsDto>> Get(
        [FromRoute] Guid workspaceId,
        CancellationToken ct,
        [FromQuery] StatsPeriodDto period = StatsPeriodDto.Last30Days)
    {
        var query = new GetWorkspaceStatsQuery(workspaceId, period);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }
}
