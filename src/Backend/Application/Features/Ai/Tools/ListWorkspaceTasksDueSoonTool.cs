using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Ai.Tools;

public record ListWorkspaceTasksDueSoonTool(Guid WorkspaceId, int WithinDays = 7, int? Take = null)
    : IRequest<IReadOnlyList<AiTaskSummary>>;

public class ListWorkspaceTasksDueSoonToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService,
    IOptions<AiToolOptions> toolOptions,
    IBusinessCalendar calendar)
    : IRequestHandler<ListWorkspaceTasksDueSoonTool, IReadOnlyList<AiTaskSummary>>
{
    private const int _maxWindowDays = 90;

    public async Task<IReadOnlyList<AiTaskSummary>> Handle(
        ListWorkspaceTasksDueSoonTool request,
        CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);
        var maxRows = toolOptions.Value.MaxRowsPerTool;

        return await aiDataRepository.GetWorkspaceTasksDueSoonAsync(
            request.WorkspaceId,
            userId,
            calendar.StartOfTodayUtc(),
            TimeSpan.FromDays(Math.Clamp(request.WithinDays, 1, _maxWindowDays)),
            Math.Clamp(request.Take ?? maxRows, 1, maxRows),
            ct);
    }
}

public class ListWorkspaceTasksDueSoonToolValidator : AbstractValidator<ListWorkspaceTasksDueSoonTool>
{
    public ListWorkspaceTasksDueSoonToolValidator(IOptions<AiToolOptions> toolOptions)
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();

        RuleFor(tool => tool.WithinDays).InclusiveBetween(1, 90);

        RuleFor(tool => tool.Take)
            .InclusiveBetween(1, toolOptions.Value.MaxRowsPerTool)
            .When(tool => tool.Take.HasValue);
    }
}
