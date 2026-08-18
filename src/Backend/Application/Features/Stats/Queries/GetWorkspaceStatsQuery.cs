using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Interfaces.Services;
using Contracts.DTOs;
using Contracts.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Stats.Queries;

public record GetWorkspaceStatsQuery(Guid WorkspaceId, StatsPeriodDto Period, int UtcOffsetMinutes)
    : IRequest<WorkspaceStatsDto>;

public class GetWorkspaceStatsQueryHandler(
    IWorkspaceAccessService workspaceAccessService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetWorkspaceStatsQuery, WorkspaceStatsDto>
{
    public async Task<WorkspaceStatsDto> Handle(GetWorkspaceStatsQuery request, CancellationToken ct)
    {
        // Owner only. Every figure on this page aggregates the whole workspace with no per-board
        // membership check, so the gate is the only thing keeping it from being a cross-board read.
        await workspaceAccessService.EnsureCanViewStatsAsync(request.WorkspaceId, ct);

        var window = StatsWindow.Resolve(request.Period, request.UtcOffsetMinutes, dateTimeProvider.UtcNow);

        return new WorkspaceStatsDto(window.Period, window.LocalStart, window.LocalEnd);
    }
}

public class GetWorkspaceStatsQueryValidator : AbstractValidator<GetWorkspaceStatsQuery>
{
    public GetWorkspaceStatsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();

        RuleFor(x => x.Period).IsInEnum();

        // The real range of civil offsets. Outside it DateTimeOffset itself would throw.
        RuleFor(x => x.UtcOffsetMinutes)
            .InclusiveBetween(StatsWindow.MinUtcOffsetMinutes, StatsWindow.MaxUtcOffsetMinutes)
            .WithMessage("UTC offset must be between -720 and 840 minutes.");
    }
}
