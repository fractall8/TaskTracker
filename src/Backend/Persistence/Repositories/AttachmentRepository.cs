using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class AttachmentRepository(TaskTrackerDbContext dbContext)
    : Repository<Attachment, Guid>(dbContext), IAttachmentRepository
{
    public async Task<List<string>> GetUrlsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await DbContext.Attachments
            .Where(a => a.Task!.Column!.Board!.WorkspaceId == workspaceId && !a.IsDeleted)
            .Select(a => a.FileUrl)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetUrlsByBoardIdAsync(Guid boardId, CancellationToken ct = default)
    {
        return await DbContext.Attachments
            .Where(a => a.Task!.Column!.BoardId == boardId && !a.IsDeleted)
            .Select(a => a.FileUrl)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetUrlsByColumnIdAsync(Guid columnId, CancellationToken ct = default)
    {
        return await DbContext.Attachments
            .Where(a => a.Task!.Column!.Id == columnId && !a.IsDeleted)
            .Select(a => a.FileUrl)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetUrlsByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await DbContext.Attachments
            .Where(a => a.TaskId ==  taskId && !a.IsDeleted)
            .Select(a => a.FileUrl)
            .ToListAsync(ct);
    }

    public async Task<List<Attachment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await DbContext.Attachments.Where(a => a.TaskId == taskId && !a.IsDeleted).ToListAsync(ct);
    }
}
