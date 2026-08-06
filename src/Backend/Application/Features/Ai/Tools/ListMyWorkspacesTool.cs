using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.Ai.Tools;

// No arguments: this is the entry point that gives the model a workspaceId for every other tool.
public record ListMyWorkspacesTool : IRequest<IReadOnlyList<AiWorkspaceSummary>>;

public class ListMyWorkspacesToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<ListMyWorkspacesTool, IReadOnlyList<AiWorkspaceSummary>>
{
    public async Task<IReadOnlyList<AiWorkspaceSummary>> Handle(ListMyWorkspacesTool request, CancellationToken ct)
    {
        var (userId, _) = await workspaceAccessService.GetCurrentUserInfoAsync(ct);

        return await aiDataRepository.GetMyWorkspacesAsync(userId, ct);
    }
}
