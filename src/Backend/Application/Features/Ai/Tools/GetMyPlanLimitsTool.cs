using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Features.Ai.Tools;

public record GetMyPlanLimitsTool(Guid WorkspaceId) : IRequest<AiPlanLimits>;

public class GetMyPlanLimitsToolHandler(
    IAiDataRepository aiDataRepository,
    IWorkspaceAccessService workspaceAccessService,
    IPlanCatalog planCatalog)
    : IRequestHandler<GetMyPlanLimitsTool, AiPlanLimits>
{
    public async Task<AiPlanLimits> Handle(GetMyPlanLimitsTool request, CancellationToken ct)
    {
        var (userId, _, _) = await workspaceAccessService.EnsureIsMemberAsync(request.WorkspaceId, ct);

        var usage = await aiDataRepository.GetWorkspaceUsageAsync(request.WorkspaceId, userId, ct)
                    ?? throw new NotFoundException("Workspace not found.");

        var planId = await aiDataRepository.GetWorkspacePlanIdAsync(request.WorkspaceId, userId, ct)
                     ?? planCatalog.DefaultPlanId;

        var plan = planCatalog.GetPlan(planId);
        var limits = planCatalog.GetLimits(planId);

        return new AiPlanLimits(
            planId,
            plan.PlanDisplayName,
            limits.MaxMembersPerWorkspace,
            limits.MaxBoardsPerWorkspace,
            limits.MaxColumnsPerBoard,
            limits.MaxTasksPerBoard,
            limits.MaxAttachmentSizeMb,
            limits.CanExportBoard,
            usage.BoardCount,
            usage.MemberCount);
    }
}

public class GetMyPlanLimitsToolValidator : AbstractValidator<GetMyPlanLimitsTool>
{
    public GetMyPlanLimitsToolValidator()
    {
        RuleFor(tool => tool.WorkspaceId).NotEmpty();
    }
}
