using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITagRepository : IRepository<Tag, Guid>
{
    Task<List<Tag>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);

    Task<Tag?> GetByIdInWorkspaceAsync(Guid tagId, Guid workspaceId, CancellationToken ct = default);

    Task<bool> NameExistsAsync(Guid workspaceId, string name, Guid? excludingTagId, CancellationToken ct = default);

    Task<int> DetachFromAllTasksAsync(Guid tagId, CancellationToken ct = default);

    Task<TaskTag?> GetLinkAsync(Guid taskId, Guid tagId, CancellationToken ct = default);

    Task AddLinkAsync(TaskTag link, CancellationToken ct = default);

    void RemoveLink(TaskTag link);
}
