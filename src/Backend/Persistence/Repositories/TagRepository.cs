using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class TagRepository(TaskTrackerDbContext dbContext) : Repository<Tag, Guid>(dbContext), ITagRepository
{
    public async Task<List<Tag>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await DbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.WorkspaceId == workspaceId)
            .OrderBy(tag => tag.Name)
            .ToListAsync(ct);

    public async Task<Tag?> GetByIdInWorkspaceAsync(Guid tagId, Guid workspaceId, CancellationToken ct = default) =>
        await DbContext.Tags
            .FirstOrDefaultAsync(tag => tag.Id == tagId && tag.WorkspaceId == workspaceId, ct);

    // Matches the case-insensitive unique index; the index is still the authority under a race.
    public async Task<bool> NameExistsAsync(
        Guid workspaceId,
        string name,
        Guid? excludingTagId,
        CancellationToken ct = default) =>
        await DbContext.Tags
            .AsNoTracking()
            .AnyAsync(tag => tag.WorkspaceId == workspaceId
                             && tag.Name.ToLower() == name.ToLower()
                             && (excludingTagId == null || tag.Id != excludingTagId), ct);

    public async Task<int> DetachFromAllTasksAsync(Guid tagId, CancellationToken ct = default)
    {
        var links = await DbContext.TaskTags.Where(link => link.TagId == tagId).ToListAsync(ct);

        // RemoveRange, not an IsDeleted flag: SaveChanges turns deletes into soft deletes and stamps DeletedAt.
        DbContext.TaskTags.RemoveRange(links);

        return links.Count;
    }

    public async Task<TaskTag?> GetLinkAsync(Guid taskId, Guid tagId, CancellationToken ct = default) =>
        await DbContext.TaskTags.FirstOrDefaultAsync(link => link.TaskId == taskId && link.TagId == tagId, ct);

    public async Task AddLinkAsync(TaskTag link, CancellationToken ct = default) =>
        await DbContext.TaskTags.AddAsync(link, ct);

    public void RemoveLink(TaskTag link) => DbContext.TaskTags.Remove(link);
}
