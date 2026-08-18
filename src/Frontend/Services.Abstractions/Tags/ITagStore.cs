using Contracts.DTOs;

namespace Services.Abstractions.Tags;

public interface ITagStore
{
    Guid? WorkspaceId { get; }
    IReadOnlyList<TagDto> Tags { get; }
    bool IsLoading { get; }
    bool IsProcessing { get; }
    string? ErrorMessage { get; }

    event Action? StateChanged;

    Guid? TasksLoadingForTagId { get; }

    Task LoadAsync(Guid workspaceId, CancellationToken ct = default);

    IReadOnlyList<TaggedTaskDto>? GetLoadedTasks(Guid tagId);

    Task LoadTasksAsync(Guid workspaceId, Guid tagId, bool force = false, CancellationToken ct = default);

    Task<TagDto> CreateAsync(Guid workspaceId, string name, string? color, CancellationToken ct = default);

    Task UpdateAsync(Guid workspaceId, Guid tagId, string name, string color, CancellationToken ct = default);

    Task DeleteAsync(Guid workspaceId, Guid tagId, CancellationToken ct = default);

    void Reset();
}
