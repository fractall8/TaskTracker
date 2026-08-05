using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.Ai.Tools;

public record ListBoardsTool(Guid WorkspaceId, bool IncludeArchived = false)
    : IRequest<IReadOnlyList<AiBoardSummary>>;

public class ListBoardsToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<ListBoardsTool, IReadOnlyList<AiBoardSummary>>
{
    public async Task<IReadOnlyList<AiBoardSummary>> Handle(ListBoardsTool request, CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        return await aiDataRepository.GetBoardsAsync(request.WorkspaceId, userId, request.IncludeArchived, ct);
    }
}

public class ListBoardsToolValidator : AbstractValidator<ListBoardsTool>
{
    public ListBoardsToolValidator()
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();
    }
}
