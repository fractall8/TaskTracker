using Contracts.DTOs;
using Contracts.Requests.Tags;
using Services.Abstractions.Tags;

namespace Services.Tags.Stores;

internal sealed class TagStore(ITagApiService apiService) : ITagStore
{
    private List<TagDto> _tags = [];

    public Guid? WorkspaceId { get; private set; }
    public IReadOnlyList<TagDto> Tags => _tags;

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
        WorkspaceId = null;
        IsLoading = false;
        IsProcessing = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
