using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Ai.Tools;

public record GetWorkspaceOverviewTool(Guid WorkspaceId) : IRequest<AiWorkspaceUsage>;

public class GetWorkspaceOverviewToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService)
    : IRequestHandler<GetWorkspaceOverviewTool, AiWorkspaceUsage>
{
    public async Task<AiWorkspaceUsage> Handle(GetWorkspaceOverviewTool request, CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        return await aiDataRepository.GetWorkspaceUsageAsync(request.WorkspaceId, userId, ct)
               ?? throw new NotFoundException("Workspace not found.");
    }
}

public class GetWorkspaceOverviewToolValidator : AbstractValidator<GetWorkspaceOverviewTool>
{
    public GetWorkspaceOverviewToolValidator()
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();
    }
}
