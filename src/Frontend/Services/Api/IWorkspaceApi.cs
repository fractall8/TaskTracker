using Contracts.DTOs;
using Contracts.Requests.Workspaces;
using Refit;

namespace Services.Api;

public interface IWorkspaceApi
{
    [Get("/api/workspaces")]
    Task<IApiResponse<List<WorkspaceDto>>> GetUserWorkspacesAsync(CancellationToken ct = default);

    [Get("/api/workspaces/{workspaceId}")]
    Task<IApiResponse<WorkspaceDetailsDto>> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default);

    [Post("/api/workspaces")]
    Task<IApiResponse<WorkspaceDto>> CreateWorkspaceAsync([Body] CreateWorkspaceRequest request, CancellationToken ct = default);

    [Put("/api/workspaces/{workspaceId}")]
    Task<IApiResponse> UpdateWorkspaceAsync(Guid workspaceId, [Body] UpdateWorkspaceRequest request, CancellationToken ct = default);

    [Delete("/api/workspaces/{workspaceId}")]
    Task<IApiResponse> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);

    [Get("/api/workspaces/{workspaceId}/boards")]
    Task<IApiResponse<PagedList<BoardPreviewDto>>> GetWorkspaceBoardsAsync(
        Guid workspaceId,
        [Query] int pageNumber = 1,
        [Query] int pageSize = 24,
        [Query] string? searchTerm = null,
        CancellationToken ct = default);
}
