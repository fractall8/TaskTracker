using Contracts.DTOs;
using Contracts.Enums;
using Contracts.Requests;
using Services.Abstractions.Workspaces;
using Services.Api;
using Services.Extensions;

namespace Services.Workspaces.Stores;

public class WorkspaceStore(IWorkspaceApi workspaceApi, IWorkspaceMembersApi membersApi, IWorkspaceInvitesApi invitesApi)
    : IWorkspaceStore
{
    public List<WorkspaceDto> Workspaces { get; private set; } = [];

    public WorkspaceDetailsDto? CurrentWorkspace { get; private set; }

    public bool IsLoading { get; private set; }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public async Task LoadUserWorkspacesAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var response = await workspaceApi.GetUserWorkspacesAsync(ct);
            Workspaces = await response.HandleResponseAsync();
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

    public async Task LoadWorkspaceDetailsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var response = await workspaceApi.GetWorkspaceByIdAsync(workspaceId, ct);
            CurrentWorkspace = await response.HandleResponseAsync();
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

    public async Task<WorkspaceDto?> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken ct = default)
    {
        var response = await workspaceApi.CreateWorkspaceAsync(request, ct);
        var created = await response.HandleResponseAsync();
        Workspaces.Add(created);
        NotifyStateChanged();
        return created;
    }

    public async Task UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken ct = default)
    {
        var response = await workspaceApi.UpdateWorkspaceAsync(workspaceId, request, ct);
        await response.HandleResponseAsync();

        var index = Workspaces.FindIndex(w => w.Id == workspaceId);
        if (index >= 0)
        {
            Workspaces[index] = Workspaces[index] with { Name = request.Name, Description = request.Description };
        }

        if (CurrentWorkspace?.Id == workspaceId)
        {
            CurrentWorkspace = CurrentWorkspace with { Name = request.Name, Description = request.Description };
        }

        NotifyStateChanged();
    }

    public async Task DeleteWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var response = await workspaceApi.DeleteWorkspaceAsync(workspaceId, ct);
        await response.HandleResponseAsync();

        Workspaces.RemoveAll(w => w.Id == workspaceId);

        if (CurrentWorkspace?.Id == workspaceId)
        {
            CurrentWorkspace = null;
        }

        NotifyStateChanged();
    }

    public async Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        var response = await membersApi.RemoveMemberAsync(workspaceId, userId, ct);
        await response.HandleResponseAsync();
        await LoadWorkspaceDetailsAsync(workspaceId, ct);
    }

    public async Task ChangeMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceRoleDto newRole, CancellationToken ct = default)
    {
        var request = new ChangeMemberRoleRequest { Role = newRole };
        var response = await membersApi.ChangeMemberRoleAsync(workspaceId, userId, request, ct);
        await response.HandleResponseAsync();
        await LoadWorkspaceDetailsAsync(workspaceId, ct);
    }

    public async Task<InviteResultDto?> InviteUserAsync(Guid workspaceId, InviteUserRequest request, CancellationToken ct = default)
    {
        var response = await invitesApi.InviteUserAsync(workspaceId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task AcceptInviteAsync(string token, CancellationToken ct = default)
    {
        var response = await invitesApi.AcceptInviteAsync(new AcceptInviteRequest(token), ct);
        await response.HandleResponseAsync();
    }

    public async Task<List<UserDto>> GetWorkspaceUsersAsync(Guid workspaceId, string? searchTerm = null, CancellationToken ct = default)
    {
        var response = await membersApi.GetWorkspaceUsersAsync(workspaceId, searchTerm, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<List<UserSearchDto>> SearchUsersNotInWorkspaceAsync(Guid workspaceId, string? searchTerm = null, CancellationToken ct = default)
    {
        var response = await membersApi.SearchUsersNotInWorkspaceAsync(workspaceId, searchTerm, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<PagedList<BoardPreviewDto>> GetWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1, int pageSize = 24, string? searchTerm = null, CancellationToken ct = default)
    {
        var response = await workspaceApi.GetWorkspaceBoardsAsync(workspaceId, pageNumber, pageSize, searchTerm, ct);
        return await response.HandleResponseAsync();
    }
}
