using Application.Ai.Projections;
using Application.Common.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Ai.Tools;

// Not "open" tasks — TaskItem has no completion state TODO: add completion state
public record CountWorkspaceTasksTool(Guid WorkspaceId, Guid? BoardId = null) : IRequest<AiTaskCounts>;

public class CountWorkspaceTasksToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CountWorkspaceTasksTool, AiTaskCounts>
{
    public async Task<AiTaskCounts> Handle(CountWorkspaceTasksTool request, CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        return await aiDataRepository.CountWorkspaceTasksAsync(
            request.WorkspaceId,
            userId,
            request.BoardId,
            dateTimeProvider.UtcNow,
            ct);
    }
}

public class CountWorkspaceTasksToolValidator : AbstractValidator<CountWorkspaceTasksTool>
{
    public CountWorkspaceTasksToolValidator()
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();
    }
}
