using Contracts.DTOs;
using Contracts.Enums;
using Contracts.Requests.Workspaces;
using Services.Abstractions.Workspaces;

namespace Services.Workspaces.Stores;

public class WorkspaceStore(IWorkspaceApiService workspaceApiService) : IWorkspaceStore
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
            Workspaces = await workspaceApiService.GetUserWorkspacesAsync(ct);
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
            CurrentWorkspace = await workspaceApiService.GetWorkspaceByIdAsync(workspaceId, ct);
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

    public async Task<WorkspaceDto?> CreateWorkspaceAsync(CreateWorkspaceRequest request,
        CancellationToken ct = default)
    {
        var created = await workspaceApiService.CreateWorkspaceAsync(request, ct);
        Workspaces.Add(created);
        NotifyStateChanged();
        return created;
    }

    public async Task UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceRequest request,
        CancellationToken ct = default)
    {
        await workspaceApiService.UpdateWorkspaceAsync(workspaceId, request, ct);

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
        await workspaceApiService.DeleteWorkspaceAsync(workspaceId, ct);

        Workspaces.RemoveAll(w => w.Id == workspaceId);

        if (CurrentWorkspace?.Id == workspaceId)
        {
            CurrentWorkspace = null;
        }

        NotifyStateChanged();
    }

    public async Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct = default)
    {
        await workspaceApiService.RemoveMemberAsync(workspaceId, userId, ct);
        await LoadWorkspaceDetailsAsync(workspaceId, ct);
    }

    public async Task ChangeMemberRoleAsync(Guid workspaceId, Guid userId, WorkspaceRoleDto newRole,
        CancellationToken ct = default)
    {
        var request = new ChangeMemberRoleRequest { Role = newRole };
        await workspaceApiService.ChangeMemberRoleAsync(workspaceId, userId, request, ct);
        await LoadWorkspaceDetailsAsync(workspaceId, ct);
    }

    public async Task<List<UserDto>> GetWorkspaceUsersAsync(Guid workspaceId, string? searchTerm = null,
        CancellationToken ct = default)
    {
        return await workspaceApiService.GetWorkspaceUsersAsync(workspaceId, searchTerm, ct);
    }

    public async Task<PagedList<BoardPreviewDto>> GetWorkspaceBoardsAsync(Guid workspaceId, int pageNumber = 1,
        int pageSize = 24, string? searchTerm = null, CancellationToken ct = default)
    {
        return await workspaceApiService.GetWorkspaceBoardsAsync(workspaceId, pageNumber, pageSize, searchTerm, ct);
    }

    public async Task<List<WorkspaceInviteDto>> GetWorkspaceInvitesAsync(Guid workspaceId,
        CancellationToken ct = default)
    {
        return await workspaceApiService.GetWorkspaceInvitesAsync(workspaceId, ct);
    }

    public async Task<InviteResultDto?> InviteUserAsync(Guid workspaceId, InviteUserRequest request,
        CancellationToken ct = default)
    {
        return await workspaceApiService.InviteUserAsync(workspaceId, request, ct);
    }

    public async Task UpdateInviteExpirationAsync(Guid workspaceId, Guid inviteId,
        UpdateInviteExpirationRequest request, CancellationToken ct = default)
    {
        await workspaceApiService.UpdateInviteExpirationAsync(workspaceId, inviteId, request, ct);
    }

    public async Task RevokeInviteAsync(Guid workspaceId, Guid inviteId, CancellationToken ct = default)
    {
        await workspaceApiService.RevokeInviteAsync(workspaceId, inviteId, ct);
    }

    public async Task AcceptInviteAsync(string token, CancellationToken ct = default)
    {
        await workspaceApiService.AcceptInviteAsync(new AcceptInviteRequest(token), ct);
    }
}
