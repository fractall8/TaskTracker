namespace Application.Interfaces.Services;

public interface IWorkspaceLimitService
{
    Task EnsureCanAddWorkspaceMemberAsync(Guid workspaceId, CancellationToken ct = default);

    Task EnsureCanAddBoardAsync(Guid workspaceId, CancellationToken ct = default);

    Task EnsureCanAddColumnAsync(Guid boardId, CancellationToken ct = default);

    Task EnsureCanAddTaskAsync(Guid boardId, CancellationToken ct = default);

    Task EnsureAttachmentSizeIsAllowedAsync(Guid boardId, long sizeInBytes, CancellationToken ct = default);
}
