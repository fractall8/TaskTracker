using Application.Common.Models;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Exceptions;

namespace Infrastructure.Subscriptions.Services;

public class WorkspaceLimitService(
    ISubscriptionRepository subscriptionRepository,
    IPlanCatalog planCatalog,
    IBoardRepository boardRepository,
    IColumnRepository columnRepository,
    ITaskRepository taskRepository,
    IWorkspaceMemberRepository workspaceMemberRepository) : IWorkspaceLimitService
{
    public async Task EnsureCanAddWorkspaceMemberAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var limits = await GetLimitsAsync(workspaceId, ct);

        if (limits.MaxMembersPerWorkspace is not { } max)
        {
            return;
        }

        var currentCount = await workspaceMemberRepository.CountAsync(m => m.WorkspaceId == workspaceId, ct);

        if (currentCount >= max)
        {
            throw new WorkspaceLimitExceededException("workspace members", max);
        }
    }

    public async Task EnsureCanAddBoardAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var limits = await GetLimitsAsync(workspaceId, ct);

        if (limits.MaxBoardsPerWorkspace is not { } max)
        {
            return;
        }

        var currentCount = await boardRepository.CountAsync(b => b.WorkspaceId == workspaceId && !b.IsArchived, ct);

        if (currentCount >= max)
        {
            throw new WorkspaceLimitExceededException("boards per workspace", max);
        }
    }

    public async Task EnsureCanAddColumnAsync(Guid boardId, Guid workspaceId, CancellationToken ct = default)
    {
        var limits = await GetLimitsAsync(workspaceId, ct);

        if (limits.MaxColumnsPerBoard is not { } max)
        {
            return;
        }

        var currentCount = await columnRepository.CountAsync(c => c.BoardId == boardId, ct);

        if (currentCount >= max)
        {
            throw new WorkspaceLimitExceededException("columns per board", max);
        }
    }

    public async Task EnsureCanAddTaskAsync(Guid boardId, CancellationToken ct = default)
    {
        var workspaceId = await GetWorkspaceIdForBoardAsync(boardId, ct);
        var limits = await GetLimitsAsync(workspaceId, ct);

        if (limits.MaxTasksPerBoard is not { } max)
        {
            return;
        }

        var currentCount = await taskRepository.CountByBoardIdAsync(boardId, ct);

        if (currentCount >= max)
        {
            throw new WorkspaceLimitExceededException("tasks per board", max);
        }
    }

    public async Task EnsureAttachmentSizeIsAllowedAsync(Guid boardId, long sizeInBytes, CancellationToken ct = default)
    {
        var workspaceId = await GetWorkspaceIdForBoardAsync(boardId, ct);
        var limits = await GetLimitsAsync(workspaceId, ct);

        if (limits.MaxAttachmentSizeMb is not { } maxMb)
        {
            return;
        }

        var maxBytes = maxMb * 1024L * 1024L;

        if (sizeInBytes > maxBytes)
        {
            throw new WorkspaceLimitExceededException("attachment size (MB)", maxMb);
        }
    }

    private async Task<Guid> GetWorkspaceIdForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var board = await boardRepository.GetByIdAsync(boardId, ct);

        if (board is null)
        {
            throw new NotFoundException("Board not found.");
        }

        return board.WorkspaceId;
    }

    private async Task<WorkspaceLimits> GetLimitsAsync(Guid workspaceId, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetSubscriptionByWorkspaceIdAsync(workspaceId, ct);
        var planId = subscription?.PlanId ?? planCatalog.DefaultPlanId;

        return planCatalog.GetLimits(planId);
    }
}
