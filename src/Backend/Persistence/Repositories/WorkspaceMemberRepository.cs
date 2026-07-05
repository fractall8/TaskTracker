using Application.Interfaces.Repositories;
using Contracts.DTOs;
using Contracts.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class WorkspaceMemberRepository(TaskTrackerDbContext dbContext)
    : Repository<WorkspaceMember, Guid>(dbContext), IWorkspaceMemberRepository
{
    public async Task<WorkspaceMember?> GetByWorkspaceAndUserIdAsync(Guid workspaceId, Guid userId,
        CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

    public async Task<List<WorkspaceMemberDto>> SearchWorkspaceUsersAsync(Guid workspaceId, string? searchTerm,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(m => m.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.User!.Email, $"%{searchTerm}%") ||
                EF.Functions.ILike(m.User!.DisplayName ?? string.Empty, $"%{searchTerm}%"));
        }

        return await query
            .Select(wm => new WorkspaceMemberDto(
                wm.Id,
                wm.UserId,
                wm.User!.Email,
                wm.User.DisplayName,
                wm.User.AvatarUrl,
                (WorkspaceRoleDto)wm.Role,
                wm.JoinedAt))
            .ToListAsync(ct);
    }
}
