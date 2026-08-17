using Contracts.DTOs;
using Contracts.Requests.Tags;
using Refit;

namespace Services.Api;

public interface ITagsApi
{
    [Get("/api/workspaces/{workspaceId}/tags")]
    Task<IApiResponse<List<TagDto>>> GetAllAsync(Guid workspaceId, CancellationToken ct = default);

    [Post("/api/workspaces/{workspaceId}/tags")]
    Task<IApiResponse<TagDto>> CreateAsync(Guid workspaceId, [Body] CreateTagRequest request,
        CancellationToken ct = default);

    [Put("/api/workspaces/{workspaceId}/tags/{tagId}")]
    Task<IApiResponse<TagDto>> UpdateAsync(Guid workspaceId, Guid tagId, [Body] UpdateTagRequest request,
        CancellationToken ct = default);

    [Delete("/api/workspaces/{workspaceId}/tags/{tagId}")]
    Task<IApiResponse> DeleteAsync(Guid workspaceId, Guid tagId, CancellationToken ct = default);
}
