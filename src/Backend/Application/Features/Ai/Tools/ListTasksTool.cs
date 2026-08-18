using Application.Ai.Projections;
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
    int? DueWithinDays = null,
    int? Take = null) : IRequest<IReadOnlyList<AiTaskSummary>>;

public class ListTasksToolHandler(
    IAiDataRepository aiDataRepository,
    IBoardAccessService boardAccessService,
    IOptions<AiToolOptions> toolOptions,
    IBusinessCalendar calendar)
    : IRequestHandler<ListTasksTool, IReadOnlyList<AiTaskSummary>>
{
    private const int _maxWindowDays = 90;

    public async Task<IReadOnlyList<AiTaskSummary>> Handle(ListTasksTool request, CancellationToken ct)
    {
        var access = await boardAccessService.EnsureCanViewBoardAsync(request.BoardId, ct);
        var maxRows = toolOptions.Value.MaxRowsPerTool;

        // Start of today, not now: a due date is a day, so a task due today is upcoming rather than late.
        var today = calendar.StartOfTodayUtc();

        // Both windows resolve to absolute bounds here, which is what keeps the repository clock-free.
        // OnlyOverdue wins if both are supplied: "overdue" and "due soon" are disjoint by definition.
        DateTimeOffset? dueAfter = null;
        DateTimeOffset? dueBefore = null;

        if (request.OnlyOverdue)
        {
            dueBefore = today;
        }
        else if (request.DueWithinDays is { } days)
        {
            dueAfter = today;
            dueBefore = today.AddDays(Math.Clamp(days, 1, _maxWindowDays));
        }

        var filter = new AiTaskFilter(
            request.ColumnId,
            request.OnlyAssignedToMe,
            dueAfter,
            dueBefore,
            Math.Clamp(request.Take ?? maxRows, 1, maxRows));

        return await aiDataRepository.GetBoardTasksAsync(request.BoardId, access.UserId, filter, ct);
    }
}

public class ListTasksToolValidator : AbstractValidator<ListTasksTool>
{
    public ListTasksToolValidator(IOptions<AiToolOptions> toolOptions)
    {
        RuleFor(tool => tool.BoardId).NotEmpty();

        RuleFor(tool => tool.DueWithinDays)
            .InclusiveBetween(1, 90)
            .When(tool => tool.DueWithinDays.HasValue);

        RuleFor(tool => tool.Take)
            .InclusiveBetween(1, toolOptions.Value.MaxRowsPerTool)
            .When(tool => tool.Take.HasValue);
    }
}
