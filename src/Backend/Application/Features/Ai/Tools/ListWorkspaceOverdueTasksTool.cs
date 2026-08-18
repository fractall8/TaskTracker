using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Ai.Tools;

public record ListWorkspaceOverdueTasksTool(Guid WorkspaceId, int? Take = null)
    : IRequest<IReadOnlyList<AiTaskSummary>>;

public class ListWorkspaceOverdueTasksToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService,
    IOptions<AiToolOptions> toolOptions,
    IBusinessCalendar calendar)
    : IRequestHandler<ListWorkspaceOverdueTasksTool, IReadOnlyList<AiTaskSummary>>
{
    public async Task<IReadOnlyList<AiTaskSummary>> Handle(
        ListWorkspaceOverdueTasksTool request,
        CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);
        var maxRows = toolOptions.Value.MaxRowsPerTool;

        return await aiDataRepository.GetWorkspaceOverdueTasksAsync(
            request.WorkspaceId,
            userId,
            calendar.StartOfTodayUtc(),
            Math.Clamp(request.Take ?? maxRows, 1, maxRows),
            ct);
    }
}

public class ListWorkspaceOverdueTasksToolValidator : AbstractValidator<ListWorkspaceOverdueTasksTool>
{
    public ListWorkspaceOverdueTasksToolValidator(IOptions<AiToolOptions> toolOptions)
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();

        RuleFor(tool => tool.Take)
            .InclusiveBetween(1, toolOptions.Value.MaxRowsPerTool)
            .When(tool => tool.Take.HasValue);
    }
}
