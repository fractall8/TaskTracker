using Contracts.DTOs;
using Contracts.Requests.Tags;
using Services.Abstractions.Tags;
using Services.Api;
using Services.Extensions;

namespace Services.Tags;

public class TagApiService(ITagsApi tagsApi) : ITagApiService
{
    public async Task<List<TagDto>> GetTagsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await tagsApi.GetAllAsync(workspaceId, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<TagDto> CreateTagAsync(Guid workspaceId, CreateTagRequest request,
        CancellationToken ct = default)
    {
        var response = await tagsApi.CreateAsync(workspaceId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<TagDto> UpdateTagAsync(Guid workspaceId, Guid tagId, UpdateTagRequest request,
        CancellationToken ct = default)
    {
        var response = await tagsApi.UpdateAsync(workspaceId, tagId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task DeleteTagAsync(Guid workspaceId, Guid tagId, CancellationToken ct = default)
    {
        var response = await tagsApi.DeleteAsync(workspaceId, tagId, ct);
        await response.HandleResponseAsync();
    }
}
