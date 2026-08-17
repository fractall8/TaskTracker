using Contracts.DTOs;
using Contracts.Requests.Tags;

namespace Services.Abstractions.Tags;

public interface ITagApiService
{
    Task<List<TagDto>> GetTagsAsync(Guid workspaceId, CancellationToken ct = default);

    Task<TagDto> CreateTagAsync(Guid workspaceId, CreateTagRequest request, CancellationToken ct = default);

    Task<TagDto> UpdateTagAsync(Guid workspaceId, Guid tagId, UpdateTagRequest request,
        CancellationToken ct = default);

    Task DeleteTagAsync(Guid workspaceId, Guid tagId, CancellationToken ct = default);
}
