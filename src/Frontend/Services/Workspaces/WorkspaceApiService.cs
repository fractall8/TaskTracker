using Contracts.DTOs;
using Contracts.Requests.Workspaces;
using Services.Abstractions.Workspaces;
using Services.Api;
using Services.Extensions;

namespace Services.Workspaces;

public class WorkspaceApiService(
    IWorkspaceApi workspaceApi,
    IWorkspaceMembersApi membersApi,
    IWorkspaceInvitesApi invitesApi) : IWorkspaceApiService
{
    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(CancellationToken ct = default) =>
        await (await workspaceApi.GetUserWorkspacesAsync(ct)).HandleResponseAsync();

    public async Task<WorkspaceDetailsDto> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await (await workspaceApi.GetWorkspaceByIdAsync(workspaceId, ct)).HandleResponseAsync();

    public async Task<WorkspaceDto>
        CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken ct = default) =>
        await (await workspaceApi.CreateWorkspaceAsync(request, ct)).HandleResponseAsync();

    public async Task UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request,
        CancellationToken ct = default) =>
        await (await workspaceApi.UpdateWorkspaceAsync(workspaceId, request, ct)).HandleResponseAsync();

    public async Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default) =>
        await (await workspaceApi.DeleteWorkspaceAsync(workspaceId, ct)).HandleResponseAsync();

    public async Task<PagedList<BoardPreviewDto>> GetMyWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default) =>
        await (await workspaceApi.GetMyWorkspaceBoardsAsync(workspaceId, pageNumber, pageSize, searchTerm, ct))
            .HandleResponseAsync();

    public async Task<PagedList<BoardPreviewDto>> GetAllWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default) =>
        await (await workspaceApi.GetAllWorkspaceBoardsAsync(workspaceId, pageNumber, pageSize, searchTerm, ct))
            .HandleResponseAsync();

    public async Task<List<WorkspaceMemberDto>> GetWorkspaceUsersAsync(Guid workspaceId, string? searchTerm = null,
        CancellationToken ct = default) =>
        await (await membersApi.GetWorkspaceUsersAsync(workspaceId, searchTerm, ct)).HandleResponseAsync();

    public async Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default) =>
        await (await membersApi.RemoveMemberAsync(workspaceId, userId, ct)).HandleResponseAsync();

    public async Task ChangeMemberRoleAsync(Guid workspaceId, Guid userId, ChangeMemberRoleRequest request,
        CancellationToken ct = default) =>
        await (await membersApi.ChangeMemberRoleAsync(workspaceId, userId, request, ct)).HandleResponseAsync();

    public async Task<List<WorkspaceInviteDto>> GetWorkspaceInvitesAsync(Guid workspaceId,
        CancellationToken ct = default) =>
        await (await invitesApi.GetWorkspaceInvitesAsync(workspaceId, ct)).HandleResponseAsync();

    public async Task<InviteResultDto?> InviteUserAsync(Guid workspaceId, InviteUserRequest request,
        CancellationToken ct = default) =>
        await (await invitesApi.InviteUserAsync(workspaceId, request, ct)).HandleResponseAsync();

    public async Task UpdateInviteExpirationAsync(Guid workspaceId, Guid inviteId,
        UpdateInviteExpirationRequest request, CancellationToken ct = default) =>
        await (await invitesApi.UpdateInviteExpirationAsync(workspaceId, inviteId, request, ct)).HandleResponseAsync();

    public async Task RevokeInviteAsync(Guid workspaceId, Guid inviteId, CancellationToken ct = default) =>
        await (await invitesApi.RevokeInviteAsync(workspaceId, inviteId, ct)).HandleResponseAsync();

    public async Task AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default) =>
        await (await invitesApi.AcceptInviteAsync(request, ct)).HandleResponseAsync();
}
