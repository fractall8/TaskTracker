using Contracts.DTOs;
using Contracts.Enums;
using Contracts.Requests.Boards;
using Services.Abstractions.Boards;

namespace Services.Boards.Stores;

internal sealed class BoardMembersStore(IBoardMembersApiService apiService) : IBoardMembersStore
{
    private List<BoardMemberDto> _members = [];
    public IReadOnlyList<BoardMemberDto> Members => _members;

    public bool IsLoading { get; private set; }
    public bool IsProcessing { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public async Task LoadAsync(Guid boardId, CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            _members = await apiService.GetMembersAsync(boardId, ct);
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

    public async Task AddMemberAsync(Guid boardId, Guid workspaceMemberId, BoardRoleDto role,
        CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            await apiService.AddMemberAsync(boardId, new AddBoardMemberRequest(workspaceMemberId, role), ct);
            await LoadAsync(boardId, ct);
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

    public async Task UpdateRoleAsync(Guid boardId, Guid workspaceMemberId, BoardRoleDto newRole,
        CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            await apiService.UpdateRoleAsync(boardId, workspaceMemberId, new UpdateBoardMemberRoleRequest(newRole), ct);

            var index = _members.FindIndex(m => m.WorkspaceMemberId == workspaceMemberId);
            if (index != -1)
            {
                _members[index] = _members[index] with { BoardRole = newRole };
            }
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

    public async Task RemoveMemberAsync(Guid boardId, Guid workspaceMemberId, CancellationToken ct = default)
    {
        IsProcessing = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            await apiService.RemoveMemberAsync(boardId, workspaceMemberId, ct);

            _members.RemoveAll(m => m.WorkspaceMemberId == workspaceMemberId);
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
        _members.Clear();
        IsLoading = false;
        IsProcessing = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
