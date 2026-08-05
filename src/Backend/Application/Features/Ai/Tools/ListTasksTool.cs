using Application.Ai.Projections;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Ai.Tools;

public record ListTasksTool(
    Guid BoardId,
    Guid? ColumnId = null,
    bool OnlyAssignedToMe = false,
    bool OnlyOverdue = false,
    DateTimeOffset? DueBefore = null,
    int? Take = null) : IRequest<IReadOnlyList<AiTaskSummary>>;

public class ListTasksToolHandler(
    IAiDataRepository aiDataRepository,
    IBoardAccessService boardAccessService,
    IOptions<AiToolOptions> toolOptions,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ListTasksTool, IReadOnlyList<AiTaskSummary>>
{
    public async Task<IReadOnlyList<AiTaskSummary>> Handle(ListTasksTool request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);
        var maxRows = toolOptions.Value.MaxRowsPerTool;

        // OnlyOverdue collapses into DueBefore=now, which is what keeps the repository clock-free.
        var dueBefore = request.OnlyOverdue
            ? Earliest(request.DueBefore, dateTimeProvider.UtcNow)
            : request.DueBefore;

        var filter = new AiTaskFilter(
            request.ColumnId,
            request.OnlyAssignedToMe,
            dueBefore,
            Math.Clamp(request.Take ?? maxRows, 1, maxRows));

        return await aiDataRepository.GetBoardTasksAsync(request.BoardId, access.UserId, filter, ct);
    }

    private static DateTimeOffset Earliest(DateTimeOffset? requested, DateTimeOffset now) =>
        requested is { } value && value < now ? value : now;
}

public class ListTasksToolValidator : AbstractValidator<ListTasksTool>
{
    public ListTasksToolValidator(IOptions<AiToolOptions> toolOptions)
    {
        RuleFor(tool => tool.BoardId).NotEmpty();

        RuleFor(tool => tool.Take)
            .InclusiveBetween(1, toolOptions.Value.MaxRowsPerTool)
            .When(tool => tool.Take.HasValue);
    }
}
