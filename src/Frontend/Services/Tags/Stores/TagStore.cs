using Contracts.DTOs;
using Contracts.Requests.Tags;
using Services.Abstractions.Tags;

namespace Services.Tags.Stores;

internal sealed class TagStore(ITagApiService apiService) : ITagStore
{
    private List<TagDto> _tags = [];
    private readonly Dictionary<Guid, IReadOnlyList<TaggedTaskDto>> _tasksByTag = [];

    public Guid? WorkspaceId { get; private set; }
    public IReadOnlyList<TagDto> Tags => _tags;
    public Guid? TasksLoadingForTagId { get; private set; }

    public bool IsLoading { get; private set; }
    public bool IsProcessing { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public async Task LoadAsync(Guid workspaceId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            _tags = await apiService.GetTagsAsync(workspaceId, ct);
            WorkspaceId = workspaceId;

            // Tags may have been attached or detached from a task page since the last visit.
            _tasksByTag.Clear();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public IReadOnlyList<TaggedTaskDto>? GetLoadedTasks(Guid tagId) =>
        _tasksByTag.TryGetValue(tagId, out var tasks) ? tasks : null;

    public async Task LoadTasksAsync(Guid workspaceId, Guid tagId, bool force = false,
        CancellationToken ct = default)
    {
        if (!force && _tasksByTag.ContainsKey(tagId))
        {
            return;
        }

        TasksLoadingForTagId = tagId;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            _tasksByTag[tagId] = await apiService.GetTagTasksAsync(workspaceId, tagId, ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            TasksLoadingForTagId = null;
            NotifyStateChanged();
        }
    }

    public async Task<TagDto> CreateAsync(Guid workspaceId, string name, string? color,
        CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var created = await apiService.CreateTagAsync(workspaceId, new CreateTagRequest(name, color), ct);

            _tags = [.. _tags.Append(created).OrderBy(tag => tag.Name)];

            return created;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsProcessing = false;
            NotifyStateChanged();
        }
    }

    public async Task UpdateAsync(Guid workspaceId, Guid tagId, string name, string color,
        CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var updated = await apiService.UpdateTagAsync(workspaceId, tagId, new UpdateTagRequest(name, color), ct);

            _tags = [.. _tags.Select(tag => tag.Id == tagId ? updated : tag).OrderBy(tag => tag.Name)];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsProcessing = false;
            NotifyStateChanged();
        }
    }

    public async Task DeleteAsync(Guid workspaceId, Guid tagId, CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            await apiService.DeleteTagAsync(workspaceId, tagId, ct);

            _tags = [.. _tags.Where(tag => tag.Id != tagId)];
            _tasksByTag.Remove(tagId);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsProcessing = false;
            NotifyStateChanged();
        }
    }

    public void Reset()
    {
        _tags = [];
        _tasksByTag.Clear();
        WorkspaceId = null;
        TasksLoadingForTagId = null;
        IsLoading = false;
        IsProcessing = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
