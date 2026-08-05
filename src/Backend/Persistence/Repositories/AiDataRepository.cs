using Application.Ai.Projections;
using Application.Interfaces.Repositories;
using Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

// Every query projects straight into an AI record — entities are never materialised, so an audit column
// cannot reach the model even by accident.
public class AiDataRepository(TaskTrackerDbContext dbContext) : IAiDataRepository
{
    private const int _maxTake = 100;

    public async Task<IReadOnlyList<AiWorkspaceSummary>> GetMyWorkspacesAsync(
        Guid currentUserId,
        CancellationToken ct = default) =>
        await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == currentUserId)
            .OrderBy(member => member.Workspace!.Name)
            .Select(member => new AiWorkspaceSummary(
                member.WorkspaceId,
                member.Workspace!.Name,
                (WorkspaceRoleDto)member.Role,
                member.Workspace.Boards.Count(board =>
                    !board.IsArchived
                    && board.Members.Any(boardMember => boardMember.WorkspaceMember!.UserId == currentUserId))))
            .ToListAsync(ct);

    public async Task<AiWorkspaceUsage?> GetWorkspaceUsageAsync(
        Guid workspaceId,
        Guid currentUserId,
        CancellationToken ct = default) =>
        await dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.UserId == currentUserId && member.WorkspaceId == workspaceId)
            .Select(member => new AiWorkspaceUsage(
                member.WorkspaceId,
                member.Workspace!.Name,
                (WorkspaceRoleDto)member.Role,
                member.Workspace.Boards.Count(board => !board.IsArchived),
                member.Workspace.Boards.Count(board => board.IsArchived),
                member.Workspace.Members.Count))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AiBoardSummary>> GetBoardsAsync(
        Guid workspaceId,
        Guid currentUserId,
        bool includeArchived,
        CancellationToken ct = default) =>
        await VisibleBoards(currentUserId)
            .Where(board => board.WorkspaceId == workspaceId && (includeArchived || !board.IsArchived))
            .OrderBy(board => board.Name)
            .Select(board => new AiBoardSummary(
                board.Id,
                board.Name,
                board.IsArchived,
                board.ArchivedAt,
                board.Members
                    .Where(member => member.WorkspaceMember!.UserId == currentUserId)
                    .Select(member => (BoardRoleDto?)member.Role)
                    .FirstOrDefault(),
                board.Columns.Count,
                board.Columns.SelectMany(column => column.Tasks).Count()))
            .ToListAsync(ct);

    public async Task<AiBoardDetail?> GetBoardDetailAsync(
        Guid boardId,
        Guid currentUserId,
        DateTimeOffset asOf,
        CancellationToken ct = default) =>
        await VisibleBoards(currentUserId)
            .Where(board => board.Id == boardId)
            .Select(board => new AiBoardDetail(
                board.Id,
                board.Name,
                board.IsArchived,
                board.Members
                    .Where(member => member.WorkspaceMember!.UserId == currentUserId)
                    .Select(member => (BoardRoleDto?)member.Role)
                    .FirstOrDefault(),
                board.Columns.SelectMany(column => column.Tasks).Count(),
                board.Columns.SelectMany(column => column.Tasks)
                    .Count(task => task.DueDate != null && task.DueDate < asOf),
                board.Columns.SelectMany(column => column.Tasks).Count(task => task.AssigneeId == null),
                board.Columns
                    .OrderBy(column => column.Position)
                    .Select(column => new AiColumnTaskCount(
                        column.Id,
                        column.Name,
                        column.Position,
                        column.Tasks.Count))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AiTaskSummary>> GetBoardTasksAsync(
        Guid boardId,
        Guid currentUserId,
        AiTaskFilter filter,
        CancellationToken ct = default)
    {
        var query = VisibleTasks(currentUserId).Where(task => task.Column!.BoardId == boardId);

        if (filter.ColumnId is { } columnId)
        {
            query = query.Where(task => task.ColumnId == columnId);
        }

        if (filter.OnlyAssignedToMe)
        {
            query = query.Where(task => task.AssigneeId == currentUserId);
        }

        if (filter.DueBefore is { } dueBefore)
        {
            query = query.Where(task => task.DueDate != null && task.DueDate < dueBefore);
        }

        return await Summarise(query, currentUserId, filter.Take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AiTaskSummary>> GetWorkspaceOverdueTasksAsync(
        Guid workspaceId,
        Guid currentUserId,
        DateTimeOffset asOf,
        int take,
        CancellationToken ct = default)
    {
        var query = VisibleTasks(currentUserId)
            .Where(task => task.Column!.Board!.WorkspaceId == workspaceId
                           && task.DueDate != null
                           && task.DueDate < asOf);

        return await Summarise(query, currentUserId, take).ToListAsync(ct);
    }

    public async Task<AiTaskCounts> CountWorkspaceTasksAsync(
        Guid workspaceId,
        Guid currentUserId,
        Guid? boardId,
        DateTimeOffset asOf,
        CancellationToken ct = default)
    {
        var weekEnd = asOf.AddDays(7);

        var query = VisibleTasks(currentUserId)
            .Where(task => task.Column!.Board!.WorkspaceId == workspaceId);

        if (boardId is { } id)
        {
            query = query.Where(task => task.Column!.BoardId == id);
        }

        // Grouped so all four counts come back in one round trip.
        var counts = await query
            .GroupBy(_ => 1)
            .Select(group => new AiTaskCounts(
                group.Count(),
                group.Count(task => task.DueDate != null && task.DueDate < asOf),
                group.Count(task => task.DueDate != null && task.DueDate >= asOf && task.DueDate < weekEnd),
                group.Count(task => task.AssigneeId == currentUserId)))
            .FirstOrDefaultAsync(ct);

        return counts ?? new AiTaskCounts(0, 0, 0, 0);
    }

    // Board membership is the visibility rule, matching IBoardAccessService: a workspace role alone does
    // not grant board access.
    private IQueryable<Domain.Entities.Board> VisibleBoards(Guid currentUserId) =>
        dbContext.Boards
            .AsNoTracking()
            .Where(board => board.Members.Any(member => member.WorkspaceMember!.UserId == currentUserId));

    private IQueryable<Domain.Entities.TaskItem> VisibleTasks(Guid currentUserId) =>
        dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Column!.Board!.Members
                .Any(member => member.WorkspaceMember!.UserId == currentUserId));

    private static IQueryable<AiTaskSummary> Summarise(
        IQueryable<Domain.Entities.TaskItem> query,
        Guid currentUserId,
        int take) =>
        query
            .OrderBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.Position)
            .Take(Math.Clamp(take, 1, _maxTake))
            .Select(task => new AiTaskSummary(
                task.Id,
                task.Title,
                task.Column!.Name,
                task.Position,
                task.DueDate,
                task.CreatedAt,
                task.UpdatedAt,
                task.AssigneeId != null,
                task.AssigneeId == currentUserId,
                task.ReporterId == currentUserId,
                task.Attachments.Count,
                task.Comments.Count));
}
